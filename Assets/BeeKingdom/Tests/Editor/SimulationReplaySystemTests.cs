using BeeKingdom.Core.Simulation;
using NUnit.Framework;

namespace BeeKingdom.Tests.Editor
{
    public sealed class SimulationReplaySystemTests
    {
        [Test]
        public void RecordingCapturesFramesInOrder()
        {
            SimulationReplaySystem replay = new SimulationReplaySystem();
            replay.StartRecording("session");

            Assert.That(replay.RecordFrame(1, 0.05d, 0.05d, 10, 100), Is.True);
            Assert.That(replay.RecordFrame(2, 0.10d, 0.05d, 11, 101), Is.True);
            SimulationReplayRecording recording = replay.StopRecording();

            Assert.That(recording.Version, Is.EqualTo(1));
            Assert.That(recording.SessionId, Is.EqualTo("session"));
            Assert.That(recording.Frames.Count, Is.EqualTo(2));
            Assert.That(recording.Frames[1].StateHash, Is.EqualTo(101));
        }

        [Test]
        public void CompareReportsMatchingRecordings()
        {
            SimulationReplayRecording expected = CreateRecording(42);
            SimulationReplayRecording actual = CreateRecording(42);
            SimulationReplaySystem replay = new SimulationReplaySystem();

            SimulationReplayComparison comparison = replay.Compare(expected, actual);

            Assert.That(comparison.Matches, Is.True);
            Assert.That(comparison.ComparedFrames, Is.EqualTo(1));
            Assert.That(comparison.MismatchIndex, Is.EqualTo(-1));
        }

        [Test]
        public void CompareReportsFirstMismatch()
        {
            SimulationReplayRecording expected = CreateRecording(42);
            SimulationReplayRecording actual = CreateRecording(99);
            SimulationReplaySystem replay = new SimulationReplaySystem();

            SimulationReplayComparison comparison = replay.Compare(expected, actual);

            Assert.That(comparison.Matches, Is.False);
            Assert.That(comparison.MismatchIndex, Is.EqualTo(0));
            Assert.That(replay.Diagnostics.Mismatches, Is.EqualTo(1));
        }

        private static SimulationReplayRecording CreateRecording(int stateHash)
        {
            SimulationReplaySystem replay = new SimulationReplaySystem();
            replay.StartRecording("session");
            replay.RecordFrame(1, 0.05d, 0.05d, 7, stateHash);
            return replay.StopRecording();
        }
    }
}
