using System;
using System.Collections.Generic;

namespace BeeKingdom.Core.Simulation
{
    public readonly struct SimulationReplayFrame
    {
        public long TickIndex { get; }
        public double SimulationSeconds { get; }
        public double DeltaSeconds { get; }
        public int InputHash { get; }
        public int StateHash { get; }

        public SimulationReplayFrame(long tickIndex, double simulationSeconds, double deltaSeconds, int inputHash, int stateHash)
        {
            TickIndex = tickIndex;
            SimulationSeconds = simulationSeconds;
            DeltaSeconds = deltaSeconds;
            InputHash = inputHash;
            StateHash = stateHash;
        }
    }

    public sealed class SimulationReplayRecording
    {
        public int Version { get; }
        public string SessionId { get; }
        public IReadOnlyList<SimulationReplayFrame> Frames { get; }

        public SimulationReplayRecording(int version, string sessionId, IReadOnlyList<SimulationReplayFrame> frames)
        {
            Version = version;
            SessionId = string.IsNullOrWhiteSpace(sessionId) ? "default" : sessionId;
            Frames = frames ?? Array.Empty<SimulationReplayFrame>();
        }
    }

    public readonly struct SimulationReplayComparison
    {
        public bool Matches { get; }
        public int ComparedFrames { get; }
        public int MismatchIndex { get; }
        public string Reason { get; }

        public SimulationReplayComparison(bool matches, int comparedFrames, int mismatchIndex, string reason)
        {
            Matches = matches;
            ComparedFrames = comparedFrames;
            MismatchIndex = mismatchIndex;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class SimulationReplayDiagnostics
    {
        public int RecordingsStarted { get; private set; }
        public int FramesRecorded { get; private set; }
        public int RecordingsCompleted { get; private set; }
        public int Comparisons { get; private set; }
        public int Mismatches { get; private set; }

        public void RecordStart() => RecordingsStarted++;
        public void RecordFrame() => FramesRecorded++;
        public void RecordComplete() => RecordingsCompleted++;
        public void RecordComparison(bool matches)
        {
            Comparisons++;
            if (!matches) Mismatches++;
        }
    }

    public sealed class SimulationReplaySystem
    {
        private const int RecordingVersion = 1;

        private readonly List<SimulationReplayFrame> frames = new List<SimulationReplayFrame>();
        private string activeSessionId = "default";
        private bool isRecording;

        public SimulationReplayDiagnostics Diagnostics { get; } = new SimulationReplayDiagnostics();
        public bool IsRecording => isRecording;
        public int FrameCount => frames.Count;

        public void StartRecording(string sessionId)
        {
            frames.Clear();
            activeSessionId = string.IsNullOrWhiteSpace(sessionId) ? "default" : sessionId;
            isRecording = true;
            Diagnostics.RecordStart();
        }

        public bool RecordFrame(long tickIndex, double simulationSeconds, double deltaSeconds, int inputHash, int stateHash)
        {
            if (!isRecording || tickIndex < 0 || deltaSeconds < 0d)
            {
                return false;
            }

            frames.Add(new SimulationReplayFrame(tickIndex, simulationSeconds, deltaSeconds, inputHash, stateHash));
            Diagnostics.RecordFrame();
            return true;
        }

        public SimulationReplayRecording StopRecording()
        {
            isRecording = false;
            Diagnostics.RecordComplete();
            return new SimulationReplayRecording(RecordingVersion, activeSessionId, new List<SimulationReplayFrame>(frames));
        }

        public void LoadRecording(SimulationReplayRecording recording)
        {
            frames.Clear();
            activeSessionId = recording?.SessionId ?? "default";
            if (recording == null) return;

            for (int i = 0; i < recording.Frames.Count; i++)
            {
                frames.Add(recording.Frames[i]);
            }
        }

        public SimulationReplayRecording CreateRecording()
        {
            return new SimulationReplayRecording(RecordingVersion, activeSessionId, new List<SimulationReplayFrame>(frames));
        }

        public SimulationReplayComparison Compare(SimulationReplayRecording expected, SimulationReplayRecording actual)
        {
            if (expected == null || actual == null)
            {
                SimulationReplayComparison missing = new SimulationReplayComparison(false, 0, -1, "Missing recording.");
                Diagnostics.RecordComparison(false);
                return missing;
            }

            int count = Math.Min(expected.Frames.Count, actual.Frames.Count);
            for (int i = 0; i < count; i++)
            {
                SimulationReplayFrame left = expected.Frames[i];
                SimulationReplayFrame right = actual.Frames[i];
                if (!FramesMatch(left, right))
                {
                    SimulationReplayComparison mismatch = new SimulationReplayComparison(false, i + 1, i, "Replay frame mismatch.");
                    Diagnostics.RecordComparison(false);
                    return mismatch;
                }
            }

            if (expected.Frames.Count != actual.Frames.Count)
            {
                SimulationReplayComparison lengthMismatch = new SimulationReplayComparison(false, count, count, "Replay length mismatch.");
                Diagnostics.RecordComparison(false);
                return lengthMismatch;
            }

            SimulationReplayComparison match = new SimulationReplayComparison(true, count, -1, string.Empty);
            Diagnostics.RecordComparison(true);
            return match;
        }

        private static bool FramesMatch(SimulationReplayFrame left, SimulationReplayFrame right)
        {
            return left.TickIndex == right.TickIndex
                && left.SimulationSeconds.Equals(right.SimulationSeconds)
                && left.DeltaSeconds.Equals(right.DeltaSeconds)
                && left.InputHash == right.InputHash
                && left.StateHash == right.StateHash;
        }
    }
}
