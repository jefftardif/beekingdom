using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BeeKingdom.Editor
{
    // One-shot, opt-in LOCAL EditMode test runner. Does not stop Play mode or
    // exit Unity; it waits until the user is no longer playing. No builds,
    // server changes, login, resource spending or scene saves.
    [InitializeOnLoad]
    public static class AcademyEditModeTestOneShot
    {
        private static readonly string Root =
            Directory.GetParent(Application.dataPath)?.FullName ??
            Environment.CurrentDirectory;
        private static readonly string RequestFile =
            Path.Combine(Root, "Temp", "BKAcademyEditMode.request");
        private static readonly string ResultFile =
            Path.Combine(Root, "Temp", "BKAcademyEditMode.results.txt");
        private static SequentialRunner active;

        static AcademyEditModeTestOneShot()
        {
            EditorApplication.update += Tick;
        }

        // Explicit manual fallback if Unity auto-refresh was disabled while
        // the editor was in Play mode. Does not force exit or steal focus.
        [MenuItem("BK Studio/QA/Run Academy EditMode Tests")]
        public static void RequestFromMenu()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RequestFile));
            File.WriteAllText(RequestFile,"Academy manual QA requested");
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (active != null || !File.Exists(RequestFile)) return;
            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                File.Delete(RequestFile);
                Directory.CreateDirectory(Path.GetDirectoryName(ResultFile));
                File.WriteAllText(ResultFile,
                    "Academy EditMode QA started: " +
                    DateTimeOffset.UtcNow.ToString("O") + Environment.NewLine);
                var runnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
                active = new SequentialRunner(runnerApi, ResultFile);
                active.Start();
            }
            catch (Exception exception)
            {
                File.AppendAllText(ResultFile, "START ERROR: " +
                    exception + Environment.NewLine);
                Debug.LogError("[AcademyQA] Cannot start: " + exception);
                active = null;
                EditorApplication.update -= Tick;
            }
        }

        private sealed class SequentialRunner : ICallbacks
        {
            private static readonly string[] Groups =
            {
                "BeeKingdom.Playground.Editor.BKStudioAcademyPremiumWindowTests",
                "BeeKingdom.Playground.Editor.BKStudioAcademyTrainingContractTests",
                "BeeKingdom.Playground.Editor.BKStudioGeneticsPremiumWindowTests",
                "BeeKingdom.Playground.Editor.InfirmaryEntryRecoveryTests",
                "BeeKingdom.Playground.Editor.SandboxDefensePremiumWindowTests"
            };
            private readonly TestRunnerApi api;
            private readonly string log;
            private int current;
            private int passed, failed, skipped, found;
            public SequentialRunner(TestRunnerApi api, string log)
            {
                this.api = api;
                this.log = log;
            }

            private void Note(string value)
            {
                File.AppendAllText(log, value + Environment.NewLine);
                Debug.Log("[AcademyQA] " + value);
            }
            public void Start()
            {
                api.RegisterCallbacks(this);
                EditorApplication.delayCall += Next;
            }
            private void Next()
            {
                if (current >= Groups.Length)
                {
                    Note("END Passed=" + passed + " Failed=" + failed +
                        " Skipped=" + skipped + " Matched=" + found +
                        " at " + DateTimeOffset.UtcNow.ToString("O"));
                    active = null;
                    EditorApplication.update -= Tick;
                    return;
                }
                Note("GROUP " + (current + 1) + "/" + Groups.Length +
                    ": " + Groups[current]);
                var filter = new Filter
                {
                    testMode = TestMode.EditMode,
                    groupNames = new[] { Groups[current] }
                };
                api.Execute(new ExecutionSettings(filter));
            }
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                int n = result.PassCount + result.FailCount + result.SkipCount;
                found += n;
                passed += result.PassCount;
                failed += result.FailCount;
                skipped += result.SkipCount;
                if (n == 0)
                {
                    failed++;
                    Note("FAILURE: no tests matched group " + Groups[current]);
                }
                Note("GROUP-END pass=" + result.PassCount +
                    " fail=" + result.FailCount + " skip=" + result.SkipCount);
                current++;
                EditorApplication.delayCall += Next;
            }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Passed)
                    Note("PASS: " + result.FullName);
                else if (result.TestStatus != TestStatus.Skipped)
                    Note("FAIL: " + result.FullName +
                        " | " + result.Message + " | " + result.StackTrace);
            }
        }
    }
}
