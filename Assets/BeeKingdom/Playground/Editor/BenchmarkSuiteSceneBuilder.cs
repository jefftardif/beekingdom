using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using BeeKingdom.Core.Simulation;
using BeeKingdom.Core.Time;
using BeeKingdom.Gameplay;
using BeeKingdom.Hive;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeeKingdom.Playground.Editor
{
    public static class BenchmarkSuiteSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/BenchmarkSuite.unity";

        [MenuItem("Bee Kingdom/Playground/Rebuild Benchmark Suite Scene")]
        public static void RebuildBenchmarkSuiteScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BenchmarkSuite";
            new GameObject("Performance Benchmark Suite").AddComponent<BenchmarkSuiteBootstrap>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            cameraObject.transform.position = new Vector3(0f, 8f, -11f);
            cameraObject.transform.rotation = Quaternion.Euler(40f, 0f, 0f);

            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/LivingHive.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/ConstructionDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/PopulationDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/AIObservationLab.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/LogisticsDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/CommunicationLab.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/WorldSimulation.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/SeasonWeatherDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/CombatDefenseDemo.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MultiplayerSynchronization.unity", true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
        }

        public static void ValidateBenchmarkSuiteScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new InvalidOperationException("Benchmark Suite scene could not be opened.");
            if (UnityEngine.Object.FindFirstObjectByType<BenchmarkSuiteBootstrap>() == null) throw new InvalidOperationException("Benchmark Suite scene does not contain BenchmarkSuiteBootstrap.");

            List<BenchmarkResult> results = new List<BenchmarkResult>
            {
                RunColonyBenchmark("100 bees", 100, 600),
                RunColonyBenchmark("500 bees", 500, 300),
                RunMultiColonyBenchmark()
            };

            string directory = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Benchmarks");
            Directory.CreateDirectory(directory);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string csvPath = Path.Combine(directory, "DEMO-011_" + stamp + ".csv");
            string jsonPath = Path.Combine(directory, "DEMO-011_" + stamp + ".json");
            string mdPath = Path.Combine(directory, "DEMO-011_" + stamp + ".md");
            WriteCsv(csvPath, results);
            WriteJson(jsonPath, results);
            WriteMarkdown(mdPath, results);

            UnityEngine.Debug.Log("Benchmark Suite validation completed: " + results.Count + " benchmarks exported to Docs/Benchmarks.");
        }

        private static BenchmarkResult RunColonyBenchmark(string name, int bees, int ticks)
        {
            PlayableHiveState state = new NewGameInitializer().CreateNewGame(StarterHiveProfile.CreateDefault(), new StarterPopulationProfile(bees, Math.Max(1, bees / 10), Math.Max(1, bees / 5), Math.Max(1, bees / 10), Math.Max(1, bees / 10), 100, 100, null), StarterResourceProfile.CreateDefault());
            ActivateQueen(state);
            Stopwatch watch = Stopwatch.StartNew();
            double maxMs = 0d;
            for (int i = 0; i < ticks; i++)
            {
                Stopwatch tickWatch = Stopwatch.StartNew();
                SimulationExecutionContext context = new SimulationExecutionContext(new SimulationTimestamp(i + 1, (i + 1) * 0.1d), new SimulationCalendar(1, 0, 0, SimulationSeason.Spring), SimulationTickFrequency.TenHz, 0.1d, null);
                state.Controller.Execute(context);
                state.AIManager.Execute(context);
                tickWatch.Stop();
                maxMs = Math.Max(maxMs, tickWatch.Elapsed.TotalMilliseconds);
            }
            watch.Stop();
            return new BenchmarkResult(name, bees, ticks, watch.Elapsed.TotalMilliseconds / ticks, maxMs, state.AIManager.GetStatistics().BrainCount, state.TaskManager.Diagnostics.AssignmentCount, state.Diagnostics.ErrorCount);
        }

        private static BenchmarkResult RunMultiColonyBenchmark()
        {
            BenchmarkResult left = RunColonyBenchmark("multi-colony-a", 150, 200);
            BenchmarkResult right = RunColonyBenchmark("multi-colony-b", 150, 200);
            return new BenchmarkResult("multiple colonies", left.Bees + right.Bees, left.Ticks + right.Ticks, (left.AverageMs + right.AverageMs) * 0.5d, Math.Max(left.MaxMs, right.MaxMs), left.AIBrains + right.AIBrains, left.Assignments + right.Assignments, left.Errors + right.Errors);
        }

        private static void ActivateQueen(PlayableHiveState state)
        {
            state.QueenManager.UpdateState(state.QueenId, QueenState.Larva);
            state.QueenManager.UpdateState(state.QueenId, QueenState.Pupa);
            state.QueenManager.UpdateState(state.QueenId, QueenState.VirginQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.MatedQueen);
            state.QueenManager.UpdateState(state.QueenId, QueenState.ActiveQueen);
        }

        private static void WriteCsv(string path, IReadOnlyList<BenchmarkResult> results)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("name,bees,ticks,average_ms,max_ms,ai_brains,assignments,errors");
                foreach (BenchmarkResult result in results) writer.WriteLine(result.ToCsv());
            }
        }

        private static void WriteJson(string path, IReadOnlyList<BenchmarkResult> results)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("[");
                for (int i = 0; i < results.Count; i++)
                {
                    writer.Write("  " + results[i].ToJson());
                    writer.WriteLine(i == results.Count - 1 ? string.Empty : ",");
                }
                writer.WriteLine("]");
            }
        }

        private static void WriteMarkdown(string path, IReadOnlyList<BenchmarkResult> results)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("# DEMO-011 Benchmark Export");
                writer.WriteLine();
                writer.WriteLine("| Benchmark | Bees | Ticks | Average ms | Max ms | AI brains | Assignments | Errors |");
                writer.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
                foreach (BenchmarkResult result in results) writer.WriteLine(result.ToMarkdownRow());
                writer.WriteLine();
                writer.WriteLine("Network benchmark unavailable: no runtime networking framework or server service is available in this Unity workspace.");
            }
        }

        private readonly struct BenchmarkResult
        {
            public string Name { get; }
            public int Bees { get; }
            public int Ticks { get; }
            public double AverageMs { get; }
            public double MaxMs { get; }
            public int AIBrains { get; }
            public int Assignments { get; }
            public int Errors { get; }

            public BenchmarkResult(string name, int bees, int ticks, double averageMs, double maxMs, int aiBrains, int assignments, int errors)
            {
                Name = name;
                Bees = bees;
                Ticks = ticks;
                AverageMs = averageMs;
                MaxMs = maxMs;
                AIBrains = aiBrains;
                Assignments = assignments;
                Errors = errors;
            }

            public string ToCsv() => Name + "," + Bees + "," + Ticks + "," + AverageMs.ToString("0.000", CultureInfo.InvariantCulture) + "," + MaxMs.ToString("0.000", CultureInfo.InvariantCulture) + "," + AIBrains + "," + Assignments + "," + Errors;
            public string ToJson() => "{\"name\":\"" + Name + "\",\"bees\":" + Bees + ",\"ticks\":" + Ticks + ",\"averageMs\":" + AverageMs.ToString("0.000", CultureInfo.InvariantCulture) + ",\"maxMs\":" + MaxMs.ToString("0.000", CultureInfo.InvariantCulture) + ",\"aiBrains\":" + AIBrains + ",\"assignments\":" + Assignments + ",\"errors\":" + Errors + "}";
            public string ToMarkdownRow() => "| " + Name + " | " + Bees + " | " + Ticks + " | " + AverageMs.ToString("0.000", CultureInfo.InvariantCulture) + " | " + MaxMs.ToString("0.000", CultureInfo.InvariantCulture) + " | " + AIBrains + " | " + Assignments + " | " + Errors + " |";
        }
    }
}
