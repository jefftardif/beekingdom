using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;
namespace BeeKingdom.Tests.Editor
{
    public sealed class RoyalReserveRecoveryTests
    {
        private static readonly Guid Player=Guid.Parse("10000000-0000-4000-8000-000000000016");
        private static readonly Guid Hive=Guid.Parse("20000000-0000-4000-8000-000000000016");
        private static RemoteRoyalReserveSnapshot Snapshot()=>new RemoteRoyalReserveSnapshot {
            PlayerId=Player,HiveId=Hive,ContractVersion=RoyalReserveClient.ContractVersion,
            Revision=5,ServerTimeUtc=DateTimeOffset.UtcNow,BankLevel=1,Capacity=5000,ReservedTotal=100,
            Reserved=new Dictionary<string,long>{{"honey",100},{"wax",0},{"pollen",0}},
            LiquidBalances=new Dictionary<string,RemoteHiveResourceBalance>{
                {"honey",new RemoteHiveResourceBalance{Amount=900,Capacity=10000}},
                {"wax",new RemoteHiveResourceBalance{Amount=100,Capacity=10000}},
                {"pollen",new RemoteHiveResourceBalance{Amount=100,Capacity=10000}}
            }
        };
        private static RoyalReserveClient Client(Transport transport,bool authenticated=true)
        {
            var gate=new MobileAccountSessionGate();gate.ConfigureTransport(true);
            gate.Apply(AccountSessionReadinessSnapshot.FromServer(true,true,true,true,true));
            return new RoyalReserveClient(gate,new Session(authenticated?new GameAccountSession(Player,"qa-token-not-real"):null),transport);
        }
        [Test]
        public async Task Read_UsesExistingBankContractAndReturnsOnlyValidatedServerData()
        {
            var value=Snapshot();var t=new Transport{Value=value};
            var result=await Client(t).ReadAsync(Hive);
            Assert.That(result,Is.SameAs(value));Assert.That(t.Calls,Is.EqualTo(1));Assert.That(t.Request.Body,Is.Null);
            Assert.That(RoyalReserveClient.BasePath(Hive),Is.EqualTo("/game/v1/hives/"+Hive.ToString("D")+"/bank/royal-reserve"));
        }
        [TestCase(false)]
        [TestCase(true)]
        public async Task Transfer_PreservesServerRevisionAndIdempotencyKey(bool withdraw)
        {
            var t=new Transport{Value=Snapshot()};var client=Client(t);const string key="qa-same-command-key";
            if(withdraw)await client.WithdrawAsync(Hive,"honey",42,5,key);
            else await client.DepositAsync(Hive,"honey",42,5,key);
            var body=(RoyalReserveMutationRequest)t.Request.Body;
            Assert.That(body.ResourceKey,Is.EqualTo("honey"));Assert.That(body.Amount,Is.EqualTo(42));
            Assert.That(body.ExpectedRevision,Is.EqualTo(5));Assert.That(body.IdempotencyKey,Is.EqualTo(key));
        }
        [TestCase("gold",1)]
        [TestCase("honey",0)]
        [TestCase("wax",-1)]
        public void InvalidTransfers_DoNotReachTransport(string resource,long amount)
        {
            var t=new Transport{Value=Snapshot()};var client=Client(t);
            Assert.Throws<RoyalReserveClientException>(()=>client.DepositAsync(Hive,resource,amount,5,"qa"));
            Assert.That(t.Calls,Is.Zero);
        }
        [Test]
        public void MissingSession_BlocksReadBeforeSendingCredentials()
        {
            var t=new Transport{Value=Snapshot()};var client=Client(t,false);
            var error=Assert.ThrowsAsync<RoyalReserveClientException>(async()=>await client.ReadAsync(Hive));
            Assert.That(error.Error,Is.EqualTo(RoyalReserveClientError.AuthenticationRequired));Assert.That(t.Calls,Is.Zero);
        }
        [TestCase("player")]
        [TestCase("hive")]
        [TestCase("contract")]
        [TestCase("total")]
        [TestCase("negative")]
        [TestCase("missing")]
        public void InvalidSnapshots_AreRejectedRatherThanDisplayed(string kind)
        {
            var v=Snapshot();
            switch(kind){case "player":v.PlayerId=Guid.NewGuid();break;case "hive":v.HiveId=Guid.NewGuid();break;
                case "contract":v.ContractVersion="wrong";break;case "total":v.ReservedTotal=999;break;
                case "negative":v.Reserved["wax"]=-1;break;case "missing":v.Reserved.Remove("pollen");break;}
            var client=Client(new Transport{Value=v});
            var error=Assert.ThrowsAsync<RoyalReserveClientException>(async()=>await client.ReadAsync(Hive));
            Assert.That(error.Error,Is.EqualTo(RoyalReserveClientError.InvalidResponse));
        }
        private sealed class Session:IGameAccountSessionSource
        {
            private readonly GameAccountSession value;
            public Session(GameAccountSession session){value=session;}
            public bool TryGetSession(out GameAccountSession session){session=value;return value!=null;}
        }
        private sealed class Transport:IAuthenticatedGameRestTransport
        {
            public RemoteRoyalReserveSnapshot Value;public int Calls;public AuthenticatedGameRestRequest Request;
            public Task<T> SendAsync<T>(AuthenticatedGameRestRequest request,string bearerAccessToken,CancellationToken cancellationToken)
            {Calls++;Request=request;return Task.FromResult((T)(object)Value);}
        }
    }
}