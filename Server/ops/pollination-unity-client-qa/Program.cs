using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;

await PollinationSmoke.Run();
await PollinationPanelSmoke.Run();

static class PollinationSmoke
{
    private static readonly Guid P=Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid H=Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid M=Guid.Parse("88888888-9999-aaaa-bbbb-cccccccccccc");
    private static readonly Guid Alliance=Guid.Parse("abcdefab-cdef-abcd-efab-cdefabcdefab");
    private static readonly DateTimeOffset Now =
        new(2026,10,21,19,0,0,TimeSpan.Zero);
    static void Check(bool ok,string name)
    {
        if(!ok)throw new InvalidOperationException("P5_QA_FAIL:"+name);
        Console.WriteLine("PASS "+name);
    }
    static async Task Expect<T>(Func<Task> action,string name) where T:Exception
    {
        try { await action(); } catch(T) { Check(true,name); return; }
        throw new InvalidOperationException("Expected "+typeof(T).Name+" for "+name);
    }
    static PollinationClient NewClient(FakeTransport transport, FakeSource source=null,bool enabled=true)
        => new(new MobileAccountSessionGate(),source??new FakeSource(P),transport,enabled);
    static RemoteFloralSite Site() => new()
    {
        ContractVersion=PollinationClient.SiteVersion,
        WorldId="world-qa",RegionId="region-qa",SiteId="clover-qa-01",
        FamilyId="clover",NameFr="Trèfle blanc",NameEn="White clover",
        CatalogVersion="alpha.qa.v1",Revision=7,ServerTimeUtc=Now,
        PollenCapacity=30,PollenRemaining=20,FertilityBp=7100,
        BiodiversityBp=6400,Phase="blooming",PublicAccess=true,
        TotalPollinationPoints=10,DistinctContributors=2
    };
    static RemoteCivilPopulationSnapshot Population() => new()
    {
        PlayerId=P,HiveId=H,ContractVersion=PollinationClient.PopulationVersion,
        HiveRevision=2,CivilRevision=1,ServerTimeUtc=Now,Capacity=200,
        AvailableWorkers=20,AssignedWorkers=0,RecoveringWorkers=0,
        Status="available",CanStart=true
    };
    static RemoteCivilExpeditionSnapshot Expedition() => new()
    {
        ContractVersion=PollinationClient.ExpeditionVersion,PlayerId=P,HiveId=H,
        HiveRevision=5,ServerTimeUtc=Now,Status="outbound",
        AvailableWorkers=10,AssignedWorkers=10,
        Active=new()
        {
            MissionId=M,WorldId="world-qa",RegionId="region-qa",
            SiteId="clover-qa-01",Workers=10,Mode="balanced",
            StartedAtUtc=Now,ArrivesAtUtc=Now.AddMinutes(5),
            WorkEndsAtUtc=Now.AddMinutes(10),
            ReturnsAtUtc=Now.AddMinutes(15),
            PollenPerWorker=1,FertilityGainBp=5,BiodiversityGainBp=2,
            AllianceIdAtStart=Alliance
        }
    };
    static RemoteFloralAllianceProjectSnapshot Project() => new()
    {
        ContractVersion=PollinationClient.ProjectVersion,
        ServerTimeUtc=Now,
        Project=new() {ProjectId=M,AllianceId=Alliance,WorldId="world-qa",
            SiteId="clover-qa-01",CreatedByPlayerId=P,CreatedAtUtc=Now,
            Revision=2,GoalPoints=20,RequiredDistinctMembers=2,
            MinimumPointsPerMember=5,Points=25,QualifiedMembers=1,Status="Active"},
        Progression=new(){AllianceId=Alliance,Revision=1,
            Level=1,Experience=0,CompletedProjects=0},
        IsCompleted=false
    };
    public static async Task Run()
    {
        var codec=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        RemoteFloralSite decoded=JsonSerializer.Deserialize<RemoteFloralSite>(
            JsonSerializer.Serialize(Site(),codec),codec);
        PollinationClient.ValidateSite(decoded);
        Check(decoded.LocalizedName("fr-CA")=="Trèfle blanc" &&
              decoded.LocalizedName("en-US")=="White clover",
              "Unicode FR/EN site roundtrip");
        var disabledTransport=new FakeTransport();
        await Expect<PollinationClientException>(
            ()=>NewClient(disabledTransport,enabled:false).ReadSitesAsync(),
            "local opt-in defaults to fail closed");
        Check(disabledTransport.Requests.Count==0,"disabled client never sends REST");

        var invalid=new FakeTransport();
        await Expect<PollinationClientException>(()=>
            NewClient(invalid).StartExpeditionAsync(H,"../bad",5,0,0,"valid-qa-key"),
            "invalid route rejected before HTTP");
        await Expect<PollinationClientException>(()=>
            NewClient(invalid).StartExpeditionAsync(H,"clover-qa-01",5,0,0,"bad space"),
            "unsafe idempotency key rejected before HTTP");
        Check(invalid.Requests.Count==0,"bad mutation never touches server");

        var siteTransport=new FakeTransport(new List<RemoteFloralSite>{Site()});
        var sites=await NewClient(siteTransport).ReadSitesAsync();
        Check(sites.Count==1 && siteTransport.Requests[0].Method=="GET" &&
            siteTransport.Requests[0].Path==PollinationClient.SitesPath,
            "site GET uses official authenticated world route");
        var badSite=Site(); badSite.PollenRemaining=40;
        await Expect<PollinationClientException>(()=>
            NewClient(new FakeTransport(new List<RemoteFloralSite>{badSite})).ReadSitesAsync(),
            "overdrawn site rejected");
        await Expect<PollinationClientException>(()=>
            NewClient(new FakeTransport(new List<RemoteFloralSite>{Site(),Site()})).ReadSitesAsync(),
            "duplicate shared site rejected");

        var wrong=Population();wrong.PlayerId=Guid.NewGuid();
        await Expect<PollinationClientException>(()=>
            NewClient(new FakeTransport(wrong)).ReadPopulationAsync(H),
            "foreign player snapshot rejected");
        var session=new FakeSource(P);
        var refreshTransport=new FakeTransport(new AuthenticatedGameRestException(
            AuthenticatedGameRestError.Unauthorized,"expired",401),Population());
        await NewClient(refreshTransport,session).ReadPopulationAsync(H);
        Check(session.RefreshCount==1 && refreshTransport.Requests.Count==2 &&
            Object.ReferenceEquals(refreshTransport.Requests[0],refreshTransport.Requests[1]),
            "401 refresh replays exact same REST request once");
        Check(refreshTransport.Tokens.SequenceEqual(new[]{"access-token","rotated-token"}),
            "identity-preserving refresh token used");
        var changed=new FakeSource(P){RotatedId=Guid.NewGuid()};
        var changer=new FakeTransport(new AuthenticatedGameRestException(
            AuthenticatedGameRestError.Unauthorized,"expired",401));
        await Expect<PollinationClientException>(()=>
            NewClient(changer,changed).ReadPopulationAsync(H),
            "refresh cannot switch the player account");
        Check(changer.Requests.Count==1 && changed.InvalidateCount==1,
            "changed identity prevents second request and invalidates session");

        var result=new RemoteCivilExpeditionResult
        {
            Succeeded=true,Code=PollinationClient.ExpeditionStartCode,
            Snapshot=Expedition(),
            Receipt=new(){Action="start",Code=PollinationClient.ExpeditionStartCode,
                IdempotencyKey="flight-idempotent-qa",MissionId=M,
                HiveRevisionBefore=4,HiveRevisionAfter=5,
                SiteRevisionBefore=7,SiteRevisionAfter=7,AcceptedAtUtc=Now}
        };
        var launchTransport=new FakeTransport(result);
        var launched=await NewClient(launchTransport).StartExpeditionAsync(
            H,"clover-qa-01",10,4,7,"flight-idempotent-qa");
        var req=(CivilExpeditionStartWireRequest)launchTransport.Requests[0].Body;
        Check(launchTransport.Requests[0].Method=="POST" &&
            launchTransport.Requests[0].Path==PollinationClient.ExpeditionStartPath(H) &&
            req.Workers==10 && req.Mode=="balanced" &&
            req.ExpectedHiveRevision==4 && req.ExpectedSiteRevision==7,
            "civil-only expedition sends server versions and stable nonce");
        Check(launched.Snapshot.Active.AllianceIdAtStart==Alliance,
            "official departure alliance roundtrips");
        await Expect<PollinationClientException>(()=>
            NewClient(new FakeTransport(CloneWrongMission())).StartExpeditionAsync(
                H,"clover-qa-01",10,4,7,"flight-idempotent-qa"),
            "forged mission receipt blocked");

        var projectTransport=new FakeTransport(new List<RemoteFloralAllianceProjectSnapshot>{Project()});
        var projects=await NewClient(projectTransport).ReadProjectsAsync(Alliance);
        Check(projects.Count==1 &&
              projectTransport.Requests[0].Path==PollinationClient.ProjectsPath(Alliance),
              "alliance project GET uses official authenticated route");
        var invalidProject=Project(); invalidProject.Project.Status="Completed";
        invalidProject.IsCompleted=true; invalidProject.Project.QualifiedMembers=1;
        await Expect<PollinationClientException>(()=>
            NewClient(new FakeTransport(invalidProject)).ReadProjectAsync(Alliance,M),
            "solo completion cannot be displayed as finished");
        var create=new RemoteFloralProjectCreateResult
        {
            Succeeded=true,Code=PollinationClient.ProjectCreateCode,
            Snapshot=Project()
        };
        var createTransport=new FakeTransport(create);
        await NewClient(createTransport).CreateProjectAsync(Alliance,"clover-qa-01",0,"create-project-qa");
        Check(createTransport.Requests[0].Method=="POST" &&
            ((CreateFloralProjectWireRequest)createTransport.Requests[0].Body)
              .ExpectedProgressionRevision==0,
            "project creation uses SQL-backed official alliance route");
        Console.WriteLine("P5_UNITY_CLIENT_QA_PASS");
    }
    static RemoteCivilExpeditionResult CloneWrongMission()
    {
        var x=new RemoteCivilExpeditionResult
        {
            Succeeded=true,Code=PollinationClient.ExpeditionStartCode,
            Snapshot=Expedition(),
            Receipt=new(){Action="start",Code=PollinationClient.ExpeditionStartCode,
                IdempotencyKey="flight-idempotent-qa",MissionId=Guid.NewGuid(),
                HiveRevisionBefore=4,HiveRevisionAfter=5,
                SiteRevisionBefore=7,SiteRevisionAfter=7,AcceptedAtUtc=Now}
        };
        return x;
    }
}
namespace BeeKingdom.Networking
{
    public sealed class MobileAccountSessionGate
    {
        public bool CanSubmitLogin {get;set;}=true;
    }
    public sealed class GameAccountSession
    {
        public GameAccountSession(Guid player,string token){PlayerId=player;AccessToken=token;}
        public Guid PlayerId{get;}
        public string AccessToken{get;}
    }
    public interface IGameAccountSessionSource
    {
        bool TryGetSession(out GameAccountSession session);
    }
    public interface IRefreshableGameAccountSessionSource:IGameAccountSessionSource
    {
        bool TryGetKnownPlayerId(out Guid playerId);
        Task<GameAccountSession> GetFreshSessionAsync(CancellationToken cancellationToken);
        Task<GameAccountSession> RefreshAfterUnauthorizedAsync(string token,CancellationToken cancellationToken);
        Task InvalidateUnauthorizedSessionAsync(string token,CancellationToken cancellationToken);
    }
    public enum MobileAccountSessionError{TransportFailure,SessionExpired}
    public sealed class MobileAccountSessionException:Exception
    {
        public MobileAccountSessionException(MobileAccountSessionError error,string code){Error=error;SafeCode=code;}
        public MobileAccountSessionError Error{get;}
        public string SafeCode{get;}
    }
    public enum AuthenticatedGameRestError{NetworkFailure,Unauthorized,RemoteRejected,InvalidResponse}
    public sealed class AuthenticatedGameRestException:Exception
    {
        public AuthenticatedGameRestException(AuthenticatedGameRestError error,string code,int status=0){Error=error;SafeCode=code;}
        public AuthenticatedGameRestError Error{get;}
        public string SafeCode{get;}
    }
    public sealed class AuthenticatedGameRestRequest
    {
        public AuthenticatedGameRestRequest(string method,string path,object body=null){Method=method;Path=path;Body=body;}
        public string Method{get;}
        public string Path{get;}
        public object Body{get;}
    }
    public interface IAuthenticatedGameRestTransport
    {
        Task<T> SendAsync<T>(AuthenticatedGameRestRequest request,string token,CancellationToken ct);
    }
    internal sealed class FakeSource:IRefreshableGameAccountSessionSource
    {
        private readonly Guid identity;
        public FakeSource(Guid id){identity=id;RotatedId=id;}
        public Guid RotatedId{get;set;}
        public int RefreshCount{get;private set;}
        public int InvalidateCount{get;private set;}
        public bool TryGetSession(out GameAccountSession session){session=new GameAccountSession(identity,"access-token");return true;}
        public bool TryGetKnownPlayerId(out Guid id){id=identity;return true;}
        public Task<GameAccountSession> GetFreshSessionAsync(CancellationToken ct)=>Task.FromResult(new GameAccountSession(identity,"access-token"));
        public Task<GameAccountSession> RefreshAfterUnauthorizedAsync(string token,CancellationToken ct)
        {RefreshCount++;return Task.FromResult(new GameAccountSession(RotatedId,"rotated-token"));}
        public Task InvalidateUnauthorizedSessionAsync(string token,CancellationToken ct){InvalidateCount++;return Task.CompletedTask;}
    }
    internal sealed class FakeTransport:IAuthenticatedGameRestTransport
    {
        private readonly Queue<object> steps=new();
        public FakeTransport(params object[] responses){foreach(var step in responses)steps.Enqueue(step);}
        public List<AuthenticatedGameRestRequest> Requests{get;}=new();
        public List<string> Tokens{get;}=new();
        public Task<T> SendAsync<T>(AuthenticatedGameRestRequest r,string token,CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Requests.Add(r);Tokens.Add(token);
            object value=steps.Count>0?steps.Dequeue():null;
            if(value is Exception ex)throw ex;
            return Task.FromResult((T)value);
        }
    }
}
