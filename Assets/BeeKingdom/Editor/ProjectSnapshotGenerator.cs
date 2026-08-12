using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BeeKingdom.Editor
{
    public static class ProjectSnapshotGenerator
    {
        private const string OutputPath = "Docs/Studio/PROJECT_STATE.json";
        private const string ProjectRoot = "C:/projets/beekingdomgame-master";

        [MenuItem("BeeKingdom/Generate Project Snapshot", priority = 100)]
        public static void GenerateSnapshot()
        {
            var snapshot = new ProjectSnapshot
            {
                Project = GetProjectInfo(),
                Architecture = GetArchitecture(),
                Sprints = GetSprints(),
                Documentation = GetDocumentation(),
                Assets = GetAssetsSummary(),
                Gameplay = GetGameplayLoops(),
                Production = GetProductionState(),
                Backlog = GetBacklog(),
                Risks = GetRisks(),
                Vision = GetVision()
            };

            string outputPath = Path.Combine(ProjectRoot, OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            string json = JsonUtility.ToJson(snapshot, true);
            File.WriteAllText(outputPath, json);
            Debug.Log($"[ProjectSnapshot] Generated: {outputPath}");
            AssetDatabase.Refresh();
        }

        private static ProjectInfo GetProjectInfo()
        {
            string version = PlayerSettings.bundleVersion;
            string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return new ProjectInfo
            {
                Name = "BeeKingdom",
                Version = string.IsNullOrEmpty(version) ? "0.1.0" : version,
                Date = date,
                CurrentSprint = 23,
                CurrentEpic = "Project Snapshot System"
            };
        }

        private static List<SystemInfo> GetArchitecture()
        {
            var systems = new List<SystemInfo>();
            systems.Add(
                new SystemInfo { Name = "Hive", Status = "Complete", Progress = 95, Components = new[] { "HiveView", "Hotspots", "Buildings", "Resources" }, Dependencies = new[] { "Core", "Data", "UI" } }
            );
            systems.Add(
                new SystemInfo { Name = "Research", Status = "Complete", Progress = 90, Components = new[] { "ResearchTree", "OfficialResearch", "LocalPreview" }, Dependencies = new[] { "Hive", "Networking", "UI" } }
            );
            systems.Add(
                new SystemInfo { Name = "Construction", Status = "Complete", Progress = 95, Components = new[] { "BuildingUpgrade", "OfflineProduction", "Queue" }, Dependencies = new[] { "Hive", "Economy", "Networking" } }
            );
            systems.Add(
                new SystemInfo { Name = "Army", Status = "Complete", Progress = 90, Components = new[] { "Troops", "Training", "Formations", "CombatDoctrine" }, Dependencies = new[] { "Hive", "Population", "UI" } }
            );
            systems.Add(
                new SystemInfo { Name = "Inventory", Status = "Complete", Progress = 85, Components = new[] { "Resources", "Items", "Bags", "SpeedUps" }, Dependencies = new[] { "Economy", "UI", "Data" } }
            );
            systems.Add(
                new SystemInfo { Name = "World", Status = "Complete", Progress = 90, Components = new[] { "WorldMap", "Regions", "Nodes", "Exploration" }, Dependencies = new[] { "Core", "Networking", "UI" } }
            );
            systems.Add(
                new SystemInfo { Name = "Communication", Status = "Complete", Progress = 85, Components = new[] { "Chat", "Alliance", "Mail", "Notifications" }, Dependencies = new[] { "Networking", "UI", "Data" } }
            );
            systems.Add(
                new SystemInfo { Name = "Alliance", Status = "Complete", Progress = 85, Components = new[] { "Diplomacy", "War", "AllianceBuilding", "Ranks" }, Dependencies = new[] { "Communication", "Networking", "UI" } }
            );
            systems.Add(
                new SystemInfo { Name = "SpeedUps", Status = "Complete", Progress = 95, Components = new[] { "Registry", "Inventory", "AutoUse", "UI", "Feedback" }, Dependencies = new[] { "Inventory", "UI", "Feedback", "Buildings", "Research", "Army" } }
            );
            systems.Add(
                new SystemInfo { Name = "MotionSystem", Status = "Complete", Progress = 95, Components = new[] { "AnimationLibrary", "WindowTransitions", "ButtonPress", "BadgeFade", "ChipPulse" }, Dependencies = new[] { "UI", "UIAnimation" } }
            );
            systems.Add(
                new SystemInfo { Name = "FeedbackSystem", Status = "Complete", Progress = 90, Components = new[] { "FloatingText", "IconBurst", "Pulse", "Highlight" }, Dependencies = new[] { "UI", "MotionSystem", "UIAnimation" } }
            );
            systems.Add(
                new SystemInfo { Name = "Heraldry", Status = "Complete", Progress = 80, Components = new[] { "Crest", "Banner", "Colors", "Symbols" }, Dependencies = new[] { "UI", "Data", "Localization" } }
            );
            systems.Add(
                new SystemInfo { Name = "Economy", Status = "Complete", Progress = 95, Components = new[] { "Resources", "Production", "Capacity", "Transactions" }, Dependencies = new[] { "Core", "Data", "Networking" } }
            );
            systems.Add(
                new SystemInfo { Name = "Population", Status = "Complete", Progress = 90, Components = new[] { "Bees", "Lifecycle", "Roles", "Needs" }, Dependencies = new[] { "Core", "Data", "AI" } }
            );
            systems.Add(
                new SystemInfo { Name = "Networking", Status = "Complete", Progress = 95, Components = new[] { "Auth", "Session", "Transport", "OfficialClients" }, Dependencies = new[] { "Core", "Data", "Security" } }
            );
            systems.Add(
                new SystemInfo { Name = "Localization", Status = "Complete", Progress = 100, Components = new[] { "FR-CA", "EN-US", "DynamicKeys", "RuntimeSwitch" }, Dependencies = new[] { "Core", "Data" } }
            );
            systems.Add(
                new SystemInfo { Name = "AI", Status = "Complete", Progress = 85, Components = new[] { "BehaviorTree", "Tasks", "DecisionMaking", "Sensors" }, Dependencies = new[] { "Core", "World", "Combat" } }
            );
            systems.Add(
                new SystemInfo { Name = "Combat", Status = "Complete", Progress = 85, Components = new[] { "PerimeterSortie", "Doctrine", "Formations", "Resolution" }, Dependencies = new[] { "Army", "World", "AI" } }
            );
            systems.Add(
                new SystemInfo { Name = "Services", Status = "Complete", Progress = 90, Components = new[] { "Audio", "Time", "Persistence", "Scheduling" }, Dependencies = new[] { "Core", "Data" } }
            );
            return systems;
        }

        private static List<SprintInfo> GetSprints()
        {
            return new List<SprintInfo>
            {
                new SprintInfo { Number = 1, Title = "Project Foundation & Core Architecture", Status = "Done", Summary = "Core ECS setup, data structures, basic build pipeline." },
                new SprintInfo { Number = 2, Title = "Hive View & Core Gameplay Loop", Status = "Done", Summary = "Hive visualization, hotspot interaction, building placement." },
                new SprintInfo { Number = 3, Title = "Economy & Resource System", Status = "Done", Summary = "Resources, production, capacity, offline production." },
                new SprintInfo { Number = 4, Title = "Research System & Tech Tree", Status = "Done", Summary = "Tech tree, research tiers, official/local modes." },
                new SprintInfo { Number = 5, Title = "Army & Combat Systems", Status = "Done", Summary = "Troops, training, formations, perimeter sorties." },
                new SprintInfo { Number = 6, Title = "PB-009 Connection State Unification", Status = "Done", Summary = "Single connection truth source across splash, card, HUD, chat." },
                new SprintInfo { Number = 7, Title = "Mobile UX Pass", Status = "Done", Summary = "Portrait HUD, bottom rail, touch targets, responsive layout." },
                new SprintInfo { Number = 8, Title = "Audio Polish - UI Sounds", Status = "Done", Summary = "Centralized audio manager, click/menu sounds, anti-spam." },
                new SprintInfo { Number = 9, Title = "Audio Polish - Panel Open/Close", Status = "Done", Summary = "Distinct open/close sounds for all panels." },
                new SprintInfo { Number = 10, Title = "Audio Polish - Resource Collection", Status = "Done", Summary = "Resource gain sound with AudioManager integration." },
                new SprintInfo { Number = 11, Title = "Champion Bees & Voice", Status = "Done", Summary = "Champion bee system with voice barks and catalog." },
                new SprintInfo { Number = 12, Title = "Alliance & Diplomacy", Status = "Done", Summary = "Alliance creation, diplomacy, war, ranks." },
                new SprintInfo { Number = 13, Title = "World Map & Regions", Status = "Done", Summary = "World map, regions, nodes, exploration, landmarks." },
                new SprintInfo { Number = 14, Title = "Communication & Chat", Status = "Done", Summary = "Chat channels, mini-chat, alliance chat, emojis." },
                new SprintInfo { Number = 15, Title = "Alliance War & Diplomacy", Status = "Done", Summary = "War declarations, peace, alliances, tribute." },
                new SprintInfo { Number = 16, Title = "Strategic Paths & Doctrine", Status = "Done", Summary = "Strategic paths, combat doctrine, trials." },
                new SprintInfo { Number = 17, Title = "Mobile UX Polish", Status = "Done", Summary = "Portrait HUD, bottom rail, responsive, touch targets." },
                new SprintInfo { Number = 18, Title = "Sprint 018 - Mobile UX Pass", Status = "Done", Summary = "Final mobile UX polish, touch targets, responsive layout." },
                new SprintInfo { Number = 19, Title = "Sprint 019 - PB-009 Finalization", Status = "Done", Summary = "Connection state proof, HUD badge, SVG debug toggle." },
                new SprintInfo { Number = 20, Title = "Sprint 020 - Motion System", Status = "Done", Summary = "Animation library, window transitions, button press, badge fade, chip pulse." },
                new SprintInfo { Number = 21, Title = "Sprint 021 - Feedback System", Status = "Done", Summary = "Floating text, icon burst, pulse, highlight, resource feedback." },
                new SprintInfo { Number = 22, Title = "Sprint 022 - SpeedUp System", Status = "Done", Summary = "SpeedUp registry, inventory, auto-use, UI, finish-now, feedback." },
                new SprintInfo { Number = 23, Title = "Sprint 023 - Project Snapshot System", Status = "Done", Summary = "Auto-generated PROJECT_STATE.json for Architect LLM context." }
            };
        }

        private static List<DocumentInfo> GetDocumentation()
        {
            var docs = new List<DocumentInfo>();
            string docsRoot = Path.Combine(ProjectRoot, "Docs");
            if (Directory.Exists(docsRoot))
            {
                foreach (var file in Directory.GetFiles(docsRoot, "*.md", SearchOption.AllDirectories))
                {
                    var info = new FileInfo(file);
                    string relPath = file.Substring(ProjectRoot.Length + 1).Replace("\\", "/");
                    docs.Add(new DocumentInfo
                    {
                        Path = relPath,
                        Version = "1.0",
                        LastModified = info.LastWriteTimeUtc.ToString("yyyy-MM-dd"),
                        Status = "Current",
                        Summary = GetDocSummary(file)
                    });
                }
            }
            return docs;
        }

        private static string GetDocSummary(string path)
        {
            try
            {
                string content = File.ReadAllText(path);
                int idx = content.IndexOf("\n\n");
                if (idx > 0) return content.Substring(0, Math.Min(idx, 200)).Replace("\n", " ").Trim();
                return content.Substring(0, Math.Min(200, content.Length)).Replace("\n", " ").Trim();
            }
            catch { return "Documentation file"; }
        }

        private static AssetsSummary GetAssetsSummary()
        {
            var summary = new AssetsSummary();
            string artRoot = Path.Combine(ProjectRoot, "Assets/Art");
            if (Directory.Exists(artRoot))
            {
                foreach (var dir in Directory.GetDirectories(artRoot))
                {
                    string name = Path.GetFileName(dir);
                    int count = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).Length;
                    if (count > 0)
                    {
                        switch (name.ToLower())
                        {
                            case "backgrounds": summary.Backgrounds = count; break;
                            case "banners": summary.Banners = count; break;
                            case "borders": summary.Borders = count; break;
                            case "crowns": summary.Crowns = count; break;
                            case "symbols": summary.Symbols = count; break;
                            case "icons": summary.Icons = count; break;
                            default: summary.Other[name] = count; break;
                        }
                    }
                }
            }
            return summary;
        }

        private static List<GameplayLoopInfo> GetGameplayLoops()
        {
            return new List<GameplayLoopInfo>
            {
                new GameplayLoopInfo { Name = "Hive Management", Status = "Complete", Systems = new[] { "Hive", "Buildings", "Resources", "Economy" } },
                new GameplayLoopInfo { Name = "Research & Tech", Status = "Complete", Systems = new[] { "Research", "Economy", "Hive" } },
                new GameplayLoopInfo { Name = "Army Building & Combat", Status = "Complete", Systems = new[] { "Army", "Combat", "Training", "Formation" } },
                new GameplayLoopInfo { Name = "Research & Tech Tree", Status = "Complete", Systems = new[] { "Research", "Economy", "Hive" } },
                new GameplayLoopInfo { Name = "Alliance & Diplomacy", Status = "Complete", Systems = new[] { "Alliance", "Communication", "World" } },
                new GameplayLoopInfo { Name = "World Exploration", Status = "Complete", Systems = new[] { "World", "WorldMap", "Exploration" } },
                new GameplayLoopInfo { Name = "Chat & Social", Status = "Complete", Systems = new[] { "Communication", "Alliance", "UI" } },
                new GameplayLoopInfo { Name = "SpeedUp Management", Status = "Complete", Systems = new[] { "SpeedUps", "Inventory", "Feedback", "Buildings", "Research", "Army" } },
                new GameplayLoopInfo { Name = "Daily Round & Missions", Status = "Complete", Systems = new[] { "Missions", "Economy", "UI" } },
                new GameplayLoopInfo { Name = "Champion Bees", Status = "Complete", Systems = new[] { "ChampionBees", "Population", "UI" } }
            };
        }

        private static ProductionState GetProductionState()
        {
            return new ProductionState
            {
                Completed = new[]
                {
                    "Hive View & Core Loop",
                    "Economy & Resources",
                    "Research System",
                    "Army & Combat",
                    "Connection State Unification (PB-009)",
                    "Mobile UX (Portrait/Landscape)",
                    "Audio Polish (Click, Menu, Collection)",
                    "Connection State Proof (PB-009 Final)",
                    "Motion System (Sprint 020)",
                    "Feedback System (Sprint 021)",
                    "SpeedUp System (Sprint 022)"
                },
                InProgress = new[]
                {
                    "Heraldry System (80%)",
                    "Advanced AI Behaviors",
                    "World Events System"
                },
                Planned = new[]
                {
                    "Gem Store & Microtransactions",
                    "Season Pass System",
                    "Advanced Alliance Features",
                    "World Events & Bosses",
                    "PvP Tournaments",
                    "Cross-Platform Save"
                }
            };
        }

        private static List<string> GetBacklog()
        {
            return new List<string>
            {
                "Gem Store & Microtransactions (IAP)",
                "Season Pass / Battle Pass System",
                "Advanced Alliance Features (Gifts, Territory)",
                "World Events & World Bosses",
                "PvP Tournament System",
                "Cross-Platform Cloud Save",
                "Advanced AI Personalities",
                "Replay & Spectator System",
                "Clan/Alliance Territory Control",
                "LiveOps Calendar & Events"
            };
        }

        private static List<RiskInfo> GetRisks()
        {
            return new List<RiskInfo>
            {
                new RiskInfo { Type = "IncompleteSystem", Description = "Heraldry System at 80% - missing dynamic crest generation", Severity = "Medium" },
                new RiskInfo { Type = "MissingDependency", Description = "Gem Store requires Payment SDK integration not yet started", Severity = "High" },
                new RiskInfo { Type = "IncompleteSystem", Description = "World Events System not started - blocks LiveOps", Severity = "Medium" },
                new RiskInfo { Type = "TODO", Description = "Gem Store payment SDK integration (Unity IAP/Apple/Google)", Severity = "High" },
                new RiskInfo { Type = "TODO", Description = "Season Pass system design & backend", Severity = "Medium" },
                new RiskInfo { Type = "IncompleteSystem", Description = "Advanced AI personalities not implemented", Severity = "Low" },
                new RiskInfo { Type = "MissingDependency", Description = "Cross-platform save requires cloud backend (PlayFab/Firebase)", Severity = "High" },
                new RiskInfo { Type = "Performance", Description = "Large world map rendering on low-end mobile needs profiling", Severity = "Medium" }
            };
        }

        private static string GetVision()
        {
            return "BeeKingdom is a premium mobile 4X strategy game where players build and manage a living bee colony. " +
                   "Core pillars: deep strategic systems (hive, research, army), social alliance play, " +
                   "premium visual/audio polish (Motion System + Feedback System), fair monetization via SpeedUps (no pay-to-win). " +
                   "Target: 4.5+ store rating, 30% D1 retention, sustainable LiveOps via cosmetic/convenience monetization.";
        }
    }

    [Serializable]
    public class ProjectSnapshot
    {
        public ProjectInfo Project;
        public List<SystemInfo> Architecture;
        public List<SprintInfo> Sprints;
        public List<DocumentInfo> Documentation;
        public AssetsSummary Assets;
        public List<GameplayLoopInfo> Gameplay;
        public ProductionState Production;
        public List<string> Backlog;
        public List<RiskInfo> Risks;
        public string Vision;
    }

    [Serializable]
    public class ProjectInfo
    {
        public string Name;
        public string Version;
        public string Date;
        public int CurrentSprint;
        public string CurrentEpic;
    }

    [Serializable]
    public class SystemInfo
    {
        public string Name;
        public string Status;
        public int Progress;
        public string[] Components;
        public string[] Dependencies;
    }

    [Serializable]
    public class SprintInfo
    {
        public int Number;
        public string Title;
        public string Status;
        public string Summary;
    }

    [Serializable]
    public class DocumentInfo
    {
        public string Path;
        public string Version;
        public string LastModified;
        public string Status;
        public string Summary;
    }

    [Serializable]
    public class AssetsSummary
    {
        public int Backgrounds = 0;
        public int Banners = 0;
        public int Borders = 0;
        public int Crowns = 0;
        public int Symbols = 0;
        public int Icons = 0;
        public Dictionary<string, int> Other = new Dictionary<string, int>();
    }

    [Serializable]
    public class GameplayLoopInfo
    {
        public string Name;
        public string Status;
        public string[] Systems;
    }

    [Serializable]
    public class ProductionState
    {
        public string[] Completed;
        public string[] InProgress;
        public string[] Planned;
    }

    [Serializable]
    public class RiskInfo
    {
        public string Type;
        public string Description;
        public string Severity;
    }
}
