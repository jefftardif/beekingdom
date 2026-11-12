using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeeKingdom.Networking;
using NUnit.Framework;

namespace BeeKingdom.Playground.Editor
{
    public sealed class BKStudioAcademyTrainingContractTests
    {
        private static RemoteAcademySnapshot Valid(Guid player,Guid hive)
        {
            var now = new DateTimeOffset(2026,10,19,13,0,0,TimeSpan.Zero);
            return new RemoteAcademySnapshot
            {
                PlayerId = player,
                HiveId = hive,
                ContractVersion = AcademyTrainingClient.ContractVersion,
                CatalogVersion = AcademyTrainingClient.CatalogVersion,
                Revision = 7,
                AcademyRevision = 2,
                ServerTimeUtc = now,
                AcademyBuildingLevel = 2,
                EarnedCredits = 1,
                Balances = new Dictionary<string,RemoteAcademyBalance>
                {
                    ["honey"] = new RemoteAcademyBalance {Amount=2000,Capacity=5000},
                    ["pollen"] = new RemoteAcademyBalance {Amount=800,Capacity=1000},
                    ["wax"] = new RemoteAcademyBalance {Amount=300,Capacity=1000}
                },
                Completed = new List<RemoteAcademyCompletion>
                {
                    new RemoteAcademyCompletion
                    {
                        CourseId="tactics",Credits=1,CompletedAtUtc=now.AddMinutes(-10)
                    }
                },
                Courses = new List<RemoteAcademyCourse>
                {
                    Course("tactics","completed",1),
                    Course("logistics","available",2),
                    Course("exploration","locked",3),
                    Course("mentorship","locked",4)
                }
            };
        }

        private static RemoteAcademyCourse Course(string id,string state,int level)
        {
            return new RemoteAcademyCourse
            {
                CourseId=id, State=state, Credits=1,
                RequiredAcademyLevel=level,
                Duration=TimeSpan.FromMinutes(3),
                Costs=new Dictionary<string,long> {["honey"]=100},
                Prerequisites=new List<string>(),CanStart=state=="available"
            };
        }

        [Test]
        public void AcademyServerSnapshotWithCertificatesPassesValidation()
        {
            var player=Guid.NewGuid();var hive=Guid.NewGuid();
            Assert.DoesNotThrow(()=>
                AcademyTrainingClient.ValidateSnapshot(Valid(player,hive),player,hive));
        }

        [Test]
        public void ForgedPlayerOrHiveSnapshotIsNeverAccepted()
        {
            var player=Guid.NewGuid();var hive=Guid.NewGuid();
            Assert.Throws<InvalidOperationException>(()=>
                AcademyTrainingClient.ValidateSnapshot(Valid(player,hive),
                    Guid.NewGuid(),hive));
            Assert.Throws<InvalidOperationException>(()=>
                AcademyTrainingClient.ValidateSnapshot(Valid(player,hive),
                    player,Guid.NewGuid()));
        }

        [Test]
        public void ForgedCertificateCreditsAndDuplicateCoursesAreRejected()
        {
            var player=Guid.NewGuid();var hive=Guid.NewGuid();
            var bad=Valid(player,hive);bad.EarnedCredits=4;
            Assert.Throws<InvalidOperationException>(()=>
                AcademyTrainingClient.ValidateSnapshot(bad,player,hive));
            bad=Valid(player,hive);
            bad.Courses[3].CourseId="tactics";
            Assert.Throws<InvalidOperationException>(()=>
                AcademyTrainingClient.ValidateSnapshot(bad,player,hive));
            bad=Valid(player,hive);
            bad.ActiveOperation=new RemoteAcademyOperation
            {
                OperationId=Guid.NewGuid(),CourseId="tactics",
                StartedAtUtc=bad.ServerTimeUtc.AddMinutes(-1),
                CompletesAtUtc=bad.ServerTimeUtc.AddMinutes(2),
                Status=AcademyTrainingClient.RunningStatus
            };
            Assert.Throws<InvalidOperationException>(()=>
                AcademyTrainingClient.ValidateSnapshot(bad,player,hive));
        }

        [Test]
        public void FakeUnlockOrIncorrectServerTimeAreRejected()
        {
            var player=Guid.NewGuid();
            var hive=Guid.NewGuid();
            var tooEarly=Valid(player,hive);
            tooEarly.Courses[2].State="available"; // exploration requires level 3
            Assert.Throws<InvalidOperationException>(()=>
                AcademyTrainingClient.ValidateSnapshot(tooEarly,player,hive));
            var brokenClock=Valid(player,hive);
            brokenClock.ServerTimeUtc=default(DateTimeOffset);
            Assert.Throws<InvalidOperationException>(()=>
                AcademyTrainingClient.ValidateSnapshot(brokenClock,player,hive));
        }

        [Test]
        public void FakeCompletionReceiptWithoutServerCertificateIsRejected()
        {
            var player=Guid.NewGuid();
            var hive=Guid.NewGuid();
            Guid operation=Guid.NewGuid();
            const string key="academy-complete-fake-123";
            RemoteAcademySnapshot snapshot=Valid(player,hive);
            var result=new RemoteAcademyCommandResult
            {
                Succeeded=true,
                Code="game.academy_completed",
                Snapshot=snapshot,
                Receipt=new RemoteAcademyCommandReceipt
                {
                    IdempotencyKey=key,
                    CourseId="tactics",
                    OperationId=operation,
                    Code="game.academy_completed",
                    RevisionBefore=6,
                    RevisionAfter=7,
                    AcceptedAtUtc=snapshot.ServerTimeUtc
                }
            };
            var method=typeof(AcademyTrainingClient).GetMethod(
                "ValidateMutation",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null);
            object[] arguments={result,player,hive,key,6L,
                null,operation,"game.academy_completed"};
            Assert.DoesNotThrow(()=>method.Invoke(null,arguments));

            snapshot.Completed.Clear();
            snapshot.EarnedCredits=0;
            snapshot.Courses[0].State="available";
            // Generic snapshot envelope is well-formed. The receipt is still
            // fraudulent, because no Tactics certificate was granted.
            Assert.DoesNotThrow(()=>AcademyTrainingClient.ValidateSnapshot(
                snapshot,player,hive));
            var thrown=Assert.Throws<
                System.Reflection.TargetInvocationException>(
                    ()=>method.Invoke(null,arguments));
            Assert.That(thrown.InnerException,
                Is.TypeOf<InvalidOperationException>());
        }

        private sealed class ReadFailureClient : IAcademyTrainingClient
        {
            private readonly RemoteAcademySnapshot value;
            public ReadFailureClient(RemoteAcademySnapshot model) => value = model;
            public bool FailNextRead { get; set; }
            public int MutationCalls { get; private set; }
            public bool IsOfflineReadOnly => false;
            public Task<RemoteAcademySnapshot> ReadAsync(Guid hiveId,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                if (FailNextRead)
                {
                    FailNextRead = false;
                    throw new InvalidOperationException("game.academy_qa_read_failed");
                }
                return Task.FromResult(value);
            }
            public Task<RemoteAcademyCommandResult> StartAsync(Guid hiveId,
                string courseId, long expectedRevision, string idempotencyKey,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                MutationCalls++;
                throw new InvalidOperationException("Mutation was not allowed.");
            }
            public Task<RemoteAcademyCommandResult> CompleteAsync(Guid hiveId,
                Guid operationId, long expectedRevision, string idempotencyKey,
                CancellationToken cancellationToken = default(CancellationToken))
            {
                MutationCalls++;
                throw new InvalidOperationException("Mutation was not allowed.");
            }
        }

        private static RemoteAcademySnapshot DueTraining(Guid player, Guid hive)
        {
            RemoteAcademySnapshot model = Valid(player,hive);
            model.Completed.Clear();
            model.EarnedCredits = 0;
            model.Courses[0].State = "available";
            model.Courses[0].CanStart = false;
            model.Courses[1].State = "locked";
            model.Courses[1].CanStart = false;
            model.Courses[1].Prerequisites.Add("tactics");
            model.ActiveOperation = new RemoteAcademyOperation
            {
                OperationId = Guid.NewGuid(),
                CourseId = "tactics",
                StartedAtUtc = model.ServerTimeUtc.AddMinutes(-4),
                CompletesAtUtc = model.ServerTimeUtc.AddMinutes(-1),
                Status = AcademyTrainingClient.AwaitingCompletionStatus
            };
            return model;
        }

        [Test]
        public void ExpiredServerTrainingRemainsValidAndAwaitingCertificate()
        {
            var player = Guid.NewGuid();
            var hive = Guid.NewGuid();
            var model = DueTraining(player,hive);
            Assert.DoesNotThrow(() =>
                AcademyTrainingClient.ValidateSnapshot(model,player,hive));
        }

        [Test]
        public async Task TransientReadFailurePreservesFinishedCourseWithoutClaims()
        {
            Guid player = Guid.NewGuid(), hive = Guid.NewGuid();
            var fake = new ReadFailureClient(DueTraining(player,hive));
            var controller = new AcademyTrainingPanelController(fake,hive);
            try
            {
                await controller.RefreshForProofAsync();
                Assert.That(controller.State,Is.EqualTo(AcademyTrainingScreenState.Ready));
                Assert.That(controller.ReadyToComplete,Is.True);
                fake.FailNextRead = true;
                await controller.RefreshForProofAsync();
                Assert.That(controller.State,Is.EqualTo(AcademyTrainingScreenState.Error));
                Assert.That(controller.LastCode,Is.EqualTo("game.academy_qa_read_failed"));
                Assert.That(controller.ReadyToComplete,Is.True);
                await controller.CompleteForProofAsync();
                Assert.That(fake.MutationCalls,Is.Zero,
                    "A failed authenticated refresh must never claim a diploma.");
                await controller.ConfirmDeadlineForProofAsync();
                Assert.That(controller.State,Is.EqualTo(AcademyTrainingScreenState.Ready));
                Assert.That(controller.ReadyToComplete,Is.True);
                Assert.That(fake.MutationCalls,Is.Zero);
            }
            finally { controller.Dispose(); }
        }

        private sealed class DeadlineReadProofClient : IAcademyTrainingClient
        {
            private readonly RemoteAcademySnapshot snapshot;
            public int Reads { get; private set; }
            public int Mutations { get; private set; }
            public bool IsOfflineReadOnly => false;

            public DeadlineReadProofClient(RemoteAcademySnapshot snapshot)
                => this.snapshot = snapshot;

            public Task<RemoteAcademySnapshot> ReadAsync(Guid hive,
                CancellationToken ct = default(CancellationToken))
            {
                Reads++;
                if (Reads == 2)
                {
                    // The server observes its own clock and declares the
                    // operation claimable on the first request after expiry.
                    snapshot.ActiveOperation.Status =
                        AcademyTrainingClient.AwaitingCompletionStatus;
                    snapshot.ServerTimeUtc = snapshot.ActiveOperation.CompletesAtUtc
                        .AddMilliseconds(50);
                }
                return Task.FromResult(snapshot);
            }

            public Task<RemoteAcademyCommandResult> StartAsync(Guid hive,
                string course,long revision,string key,
                CancellationToken ct = default(CancellationToken))
            {
                Mutations++;
                throw new InvalidOperationException("Unexpected start mutation.");
            }

            public Task<RemoteAcademyCommandResult> CompleteAsync(Guid hive,
                Guid op,long revision,string key,
                CancellationToken ct = default(CancellationToken))
            {
                Mutations++;
                throw new InvalidOperationException("Unexpected claim mutation.");
            }
        }

        [Test]
        public async Task DeadlineRequestsAuthoritativeConfirmationWithoutPollingDelay()
        {
            Guid player=Guid.NewGuid(), hive=Guid.NewGuid();
            RemoteAcademySnapshot snapshot = DueTraining(player,hive);
            var expires = snapshot.ServerTimeUtc.AddMilliseconds(130);
            snapshot.ActiveOperation.StartedAtUtc = expires.AddMinutes(-3);
            snapshot.ActiveOperation.CompletesAtUtc = expires;
            snapshot.ActiveOperation.Status = AcademyTrainingClient.RunningStatus;
            Assert.DoesNotThrow(()=>
                AcademyTrainingClient.ValidateSnapshot(snapshot,player,hive));

            var fake = new DeadlineReadProofClient(snapshot);
            var controller = new AcademyTrainingPanelController(fake,hive);
            try
            {
                await controller.RefreshForProofAsync();
                Assert.That(fake.Reads,Is.EqualTo(1));
                Assert.That(controller.TrainingElapsed,Is.False);
                Assert.That(controller.ReadyToComplete,Is.False);
                await controller.ConfirmDeadlineForProofAsync();
                Assert.That(fake.Reads,Is.EqualTo(1),
                    "Cannot claim or poll prematurely.");
                await Task.Delay(210);
                Assert.That(controller.TrainingElapsed,Is.True);
                Assert.That(controller.ReadyToComplete,Is.False);
                await controller.ConfirmDeadlineForProofAsync();
                Assert.That(fake.Reads,Is.EqualTo(2));
                Assert.That(controller.ReadyToComplete,Is.True,
                    "Only the server-confirmed awaiting_completion permits claim.");
                Assert.That(fake.Mutations,Is.Zero);
                await controller.ConfirmDeadlineForProofAsync();
                Assert.That(fake.Reads,Is.EqualTo(2),
                    "No repeated GET once server has confirmed.");
            }
            finally { controller.Dispose(); }
        }

        [Test]
        public void AcademysReadAndMutationRoutesAreSeparateFromResearch()
        {
            Guid hive=Guid.NewGuid();
            string academy=AcademyTrainingClient.Path(hive);
            Assert.That(academy,Does.EndWith("/academy"));
            Assert.That(academy,Does.Not.Contain("/research"));
            Assert.That(academy,Does.Not.Contain("/guard_post"));
        }
    }
}
