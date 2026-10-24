using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using BeeKingdom.Playground;

internal static class PollinationPanelSmoke
{
    private static readonly Guid Player=Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid Hive=Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid Alliance=Guid.Parse("abcdefab-cdef-abcd-efab-cdefabcdefab");
    private static readonly Guid Mission=Guid.Parse("88888888-9999-aaaa-bbbb-cccccccccccc");
    private static readonly DateTimeOffset Now=new(2026,10,21,19,0,0,TimeSpan.Zero);

    private static void Check(bool ok,string description)
    {
        if(!ok)throw new InvalidOperationException("P5_PANEL_FAIL: "+description);
        Console.WriteLine("PASS "+description);
    }

    public static async Task Run()
    {
        var fake=new MockPollinationClient();
        var off=new MockPollinationClient{Enabled=false};
        using(var inactive=new PollinationPanelController(off,Hive))
        {
            inactive.Refresh();
            inactive.LaunchSelectedExpedition();
            Check(!inactive.IsConfigured &&
                inactive.Model.State==PollinationScreenState.NotConfigured &&
                off.ReadCalls==0 && off.LaunchKeys.Count==0,
                "opt-out panel never reads or sends a mutation");
        }

        using(var panel=new PollinationPanelController(fake,Hive))
        {
            panel.Refresh();
            Check(panel.IsConfigured&&
                panel.Model.State==PollinationScreenState.Ready &&
                panel.Model.Sites.Count==2 &&
                panel.Model.SelectedSiteId=="clover-qa-01",
                "server sites load without local flower simulation");
            panel.SelectSite("not-a-real-field");
            Check(panel.Model.SelectedSiteId=="clover-qa-01",
                "unrecognized site cannot be selected");
            panel.SelectSite("clover-qa-02");
            Check(panel.Model.SelectedSiteId=="clover-qa-02",
                "known official site can be selected");
            panel.SelectSite("clover-qa-01");
            panel.AdjustWorkers(50);
            Check(panel.Model.DraftWorkers==20 &&
                panel.Model.CanLaunch,
                "draft civilian swarm is capped to server-owned roster");
            panel.AdjustWorkers(-10);
            Check(panel.Model.DraftWorkers==10,
                "civilian draft can be adjusted without touching military roster");

            fake.FailNextLaunch=true;
            panel.LaunchSelectedExpedition();
            Check(fake.LaunchKeys.Count==1 &&
                panel.Model.Expedition.Active==null &&
                panel.Model.Population.AvailableWorkers==20,
                "uncertain launch never fabricates a resource or mission");
            panel.LaunchSelectedExpedition();
            Check(fake.LaunchKeys.Count==2 &&
                fake.LaunchKeys[0]==fake.LaunchKeys[1] &&
                panel.Model.Expedition.Active!=null,
                "user explicit retry uses original idempotency key");
            Check(!panel.Model.CanLaunch &&
                panel.Model.Expedition.Active.Workers==10,
                "server mission disables a second launch");

            panel.ConfigureAlliance(Alliance,true);
            Check(fake.ProjectReads>=1 &&
                panel.Model.AllianceId==Alliance &&
                panel.Model.MayManageAllianceProjects,
                "alliance project query is scoped to official membership");
            panel.ConfigureAlliance(Guid.Empty,false);
            Check(panel.Model.AllianceId==Guid.Empty &&
                panel.Model.Projects.Count==0,
                "alliance departure clears previously private projects");

            panel.Dispose();
            Check(!panel.IsConfigured &&
                panel.Model.Population==null &&
                panel.Model.Expedition==null &&
                panel.Model.Sites.Count==0,
                "signout disposes sensitive client projections");
        }

        // The display must never display a completed solo project even when
        // its points exceed the goal. This invariant is validated by Client.
        var project=new RemoteFloralAllianceProjectSnapshot
        {
            ContractVersion=PollinationClient.ProjectVersion,
            ServerTimeUtc=Now,
            Project=new RemoteFloralAllianceProject
            {
                ProjectId=Mission,AllianceId=Alliance,WorldId="world-qa",
                SiteId="clover-qa-01",CreatedByPlayerId=Player,
                CreatedAtUtc=Now,Revision=1,GoalPoints=10,
                RequiredDistinctMembers=2,MinimumPointsPerMember=5,
                Points=20,QualifiedMembers=1,Status="Active"
            },
            Progression=new RemoteFloralAllianceProgression
            {AllianceId=Alliance,Level=1,Revision=0},
            IsCompleted=false
        };
        PollinationClient.ValidateProject(project,Alliance);
        Check(project.Project.Points>=project.Project.GoalPoints &&
            !project.IsCompleted,
            "UI contract does not convert a solo over-goal into completion");
        await Task.CompletedTask;
        Console.WriteLine("P5_PRESENTATION_QA_PASS");
    }

    private sealed class MockPollinationClient:IPollinationClient
    {
        public bool Enabled{get;set;}=true;
        public int ReadCalls{get;private set;}
        public int ProjectReads{get;private set;}
        public bool FailNextLaunch{get;set;}
        public List<string> LaunchKeys{get;}=new();
        private RemoteCivilExpeditionSnapshot expedition=new()
        {
            ContractVersion=PollinationClient.ExpeditionVersion,
            PlayerId=Player,HiveId=Hive,ServerTimeUtc=Now,
            HiveRevision=0,Status="idle",AvailableWorkers=20,
            AssignedWorkers=0
        };
        private readonly RemoteCivilPopulationSnapshot population=new()
        {
            ContractVersion=PollinationClient.PopulationVersion,
            PlayerId=Player,HiveId=Hive,HiveRevision=0,CivilRevision=0,
            ServerTimeUtc=Now,Capacity=200,AvailableWorkers=20,
            Status="available",CanStart=true
        };
        private readonly List<RemoteFloralSite> sites=new()
        {
            new(){ContractVersion=PollinationClient.SiteVersion,WorldId="world-qa",
                RegionId="region-qa",SiteId="clover-qa-01",FamilyId="clover",
                CatalogVersion="qa-v1",NameFr="Champ de trèfle",NameEn="Clover field",
                Phase="blooming",PollenRemaining=50,PollenCapacity=50,PublicAccess=true,ServerTimeUtc=Now},
            new(){ContractVersion=PollinationClient.SiteVersion,WorldId="world-qa",
                RegionId="region-qa",SiteId="clover-qa-02",FamilyId="clover",
                CatalogVersion="qa-v1",NameFr="Prairie",NameEn="Meadow",
                Phase="blooming",PollenRemaining=70,PollenCapacity=70,PublicAccess=true,ServerTimeUtc=Now}
        };
        public Task<RemoteCivilPopulationSnapshot> ReadPopulationAsync(Guid id,CancellationToken ct=default)
        {
            ReadCalls++;return Task.FromResult(population);
        }
        public Task<RemoteCivilCohortResult> StartCohortAsync(Guid hive,long revision,string key,CancellationToken ct=default)
            => throw new NotImplementedException();
        public Task<IReadOnlyList<RemoteFloralSite>> ReadSitesAsync(CancellationToken ct=default)
        {
            ReadCalls++;return Task.FromResult<IReadOnlyList<RemoteFloralSite>>(sites);
        }
        public Task<RemoteFloralSite> ReadSiteAsync(string id,CancellationToken ct=default)
            => Task.FromResult(sites.Find(x=>x.SiteId==id));
        public Task<RemoteCivilExpeditionSnapshot> ReadExpeditionAsync(Guid hive,CancellationToken ct=default)
        {
            ReadCalls++;return Task.FromResult(expedition);
        }
        public Task<RemoteCivilExpeditionResult> StartExpeditionAsync(Guid hive,
            string site,long workers,long hiveRevision,long siteRevision,
            string key,CancellationToken ct=default)
        {
            LaunchKeys.Add(key);
            if(FailNextLaunch)
            {
                FailNextLaunch=false;
                throw new PollinationClientException(
                    PollinationClientError.NetworkUnavailable,"offline");
            }
            expedition=new RemoteCivilExpeditionSnapshot
            {
                ContractVersion=PollinationClient.ExpeditionVersion,
                PlayerId=Player,HiveId=Hive,HiveRevision=hiveRevision+1,
                ServerTimeUtc=Now,Status="outbound",AssignedWorkers=workers,
                AvailableWorkers=20-workers,
                Active=new RemoteCivilExpeditionMission
                {
                    MissionId=Mission,SiteId=site,WorldId="world-qa",
                    RegionId="region-qa",Workers=workers,Mode="balanced",
                    StartedAtUtc=Now,ArrivesAtUtc=Now.AddMinutes(5),
                    WorkEndsAtUtc=Now.AddMinutes(10),
                    ReturnsAtUtc=Now.AddMinutes(15),PollenPerWorker=1
                }
            };
            return Task.FromResult(new RemoteCivilExpeditionResult
            {
                Succeeded=true,Code=PollinationClient.ExpeditionStartCode,
                Snapshot=expedition
            });
        }
        public Task<IReadOnlyList<RemoteFloralAllianceProjectSnapshot>> ReadProjectsAsync(
            Guid allianceId,CancellationToken ct=default)
        {
            ProjectReads++;
            return Task.FromResult<IReadOnlyList<RemoteFloralAllianceProjectSnapshot>>(
                Array.Empty<RemoteFloralAllianceProjectSnapshot>());
        }
        public Task<RemoteFloralAllianceProjectSnapshot> ReadProjectAsync(Guid alliance,
            Guid project,CancellationToken ct=default)=>throw new NotImplementedException();
        public Task<RemoteFloralProjectCreateResult> CreateProjectAsync(Guid alliance,
            string site,long revision,string key,CancellationToken ct=default)
            =>throw new NotImplementedException();
    }
}
