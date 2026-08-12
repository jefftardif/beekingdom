using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BeeKingdom.Editor
{
    // Lance les runs EditMode filtre par liste de classes, un par un, et quitte
    // l'editeur quand tous sont termines. Contourne les incoherences de la CLI
    // -runTests (TestJobRunner qui se bloque sans fin sur certains filtres) et
    // l'execution multiple via groupNames (seul le premier groupe etait lance).
    public static class WorldMapTestRunner
    {
        public static void Execute()
        {
            string[] args = Environment.GetCommandLineArgs();
            List<string> filters = new List<string>();
            bool capture = false;
            foreach (string arg in args)
            {
                if (capture)
                {
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        capture = false;
                    }
                    else
                    {
                        filters.Add(arg);
                        continue;
                    }
                }

                if (arg == "-worldmapTests")
                {
                    capture = true;
                }
            }

            if (filters.Count == 0)
            {
                Debug.LogError("[WorldMapTests] Aucun filtre fourni (-worldmapTests <class1> <class2> ...)");
                EditorApplication.Exit(1);
                return;
            }

            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var runner = new SequentialRunner(api, filters.ToArray());
            runner.Start();
        }

        private sealed class SequentialRunner : ICallbacks
        {
            private readonly TestRunnerApi api;
            private readonly string[] filters;
            private int index;
            private int totalPasses;
            private int totalFailures;

            public SequentialRunner(TestRunnerApi api, string[] filters)
            {
                this.api = api;
                this.filters = filters;
            }

            public void Start()
            {
                api.RegisterCallbacks(this);
                RunNext();
            }

            private void RunNext()
            {
                if (index >= filters.Length)
                {
                    Debug.Log("[WorldMapTests] Run global termine : " + totalPasses + " passes, " + totalFailures + " echecs.");
                    EditorApplication.Exit(totalFailures > 0 ? 2 : 0);
                    return;
                }

                string filter = filters[index];
                Debug.Log("[WorldMapTests] Run " + (index + 1) + "/" + filters.Length + " : " + filter);
                var filterObj = new Filter { testMode = TestMode.EditMode, groupNames = new[] { filter } };
                api.Execute(new ExecutionSettings(filterObj));
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                totalPasses += result.PassCount;
                totalFailures += result.FailCount;
                Debug.Log("[WorldMapTests] Run termine : " + result.PassCount + " passes, " + result.FailCount + " echecs, " + result.SkipCount + " ignores.");
                index++;
                EditorApplication.delayCall += RunNext;
            }

            public void TestStarted(ITestAdaptor test)
            {
                Debug.Log("[WorldMapTests] Test demarre : " + test.FullName);
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus != TestStatus.Passed)
                {
                    Debug.Log("[WorldMapTests] ECHEC : " + result.FullName + " :: " + result.Message + " :: " + result.StackTrace);
                }
            }
        }
    }
}
