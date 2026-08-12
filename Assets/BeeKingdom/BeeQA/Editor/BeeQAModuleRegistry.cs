using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace BeeKingdom.BeeQA
{
    public enum BeeQAModuleStatus
    {
        Ready,
        Running,
        Pass,
        Fail
    }

    public sealed class BeeQAResult
    {
        public bool Passed { get; }
        public string Status => Passed ? "PASS" : "FAIL";
        public double DurationMilliseconds { get; }
        public DateTime UtcDate { get; }
        public string Message { get; }

        public BeeQAResult(bool passed, double durationMilliseconds, DateTime utcDate, string message)
        {
            Passed = passed;
            DurationMilliseconds = durationMilliseconds;
            UtcDate = utcDate;
            Message = message ?? string.Empty;
        }
    }

    public interface IBeeQAModule
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        string Version { get; }
        string Author { get; }
        BeeQACategory Category { get; }
        BeeQAModuleStatus Status { get; }
        bool CanExecute { get; }
        BeeQAResult LastResult { get; }
        BeeQAResult Execute();
    }

    public abstract class BeeQAModuleBase : IBeeQAModule
    {
        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public abstract string Version { get; }
        public virtual string Author => "BeeKingdom QA";
        public abstract BeeQACategory Category { get; }
        public BeeQAModuleStatus Status { get; private set; } = BeeQAModuleStatus.Ready;
        public virtual bool CanExecute => true;
        public BeeQAResult LastResult { get; private set; }

        public BeeQAResult Execute()
        {
            if (!CanExecute)
            {
                LastResult = new BeeQAResult(false, 0d, DateTime.UtcNow, "Module non exécutable dans ce contexte.");
                Status = BeeQAModuleStatus.Fail;
                return LastResult;
            }

            Status = BeeQAModuleStatus.Running;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                string message;
                bool passed = ExecuteCore(out message);
                stopwatch.Stop();
                LastResult = new BeeQAResult(passed, stopwatch.Elapsed.TotalMilliseconds, DateTime.UtcNow, message);
                Status = passed ? BeeQAModuleStatus.Pass : BeeQAModuleStatus.Fail;
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                LastResult = new BeeQAResult(false, stopwatch.Elapsed.TotalMilliseconds, DateTime.UtcNow, exception.Message);
                Status = BeeQAModuleStatus.Fail;
            }
            return LastResult;
        }

        protected abstract bool ExecuteCore(out string message);
    }

    public static class BeeQAModuleRegistry
    {
        private static readonly List<IBeeQAModule> modules = new List<IBeeQAModule>(64);
        private static readonly List<BeeQAResult> lastRunResults = new List<BeeQAResult>(64);
        private static bool discovered;

        public static IReadOnlyList<IBeeQAModule> Modules
        {
            get
            {
                EnsureDiscovered();
                return modules;
            }
        }

        public static bool Register(IBeeQAModule module)
        {
            if (module == null || string.IsNullOrWhiteSpace(module.Id)) return false;
            for (int i = 0; i < modules.Count; i++)
            {
                if (string.Equals(modules[i].Id, module.Id, StringComparison.Ordinal)) return false;
            }
            modules.Add(module);
            return true;
        }

        public static void EnsureDiscovered()
        {
            if (discovered) return;
            discovered = true;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int assemblyIndex = 0; assemblyIndex < assemblies.Length; assemblyIndex++)
            {
                Type[] types;
                try { types = assemblies[assemblyIndex].GetTypes(); }
                catch (ReflectionTypeLoadException exception) { types = exception.Types; }
                for (int typeIndex = 0; typeIndex < types.Length; typeIndex++)
                {
                    Type type = types[typeIndex];
                    if (type == null || type.IsAbstract || type.IsInterface || type.ContainsGenericParameters ||
                        !typeof(IBeeQAModule).IsAssignableFrom(type) || type.GetConstructor(Type.EmptyTypes) == null)
                        continue;
                    try { Register((IBeeQAModule)Activator.CreateInstance(type)); }
                    catch { }
                }
            }
        }

        public static BeeQAResult Run(IBeeQAModule module)
        {
            if (module == null) return new BeeQAResult(false, 0d, DateTime.UtcNow, "Module introuvable.");
            try { return module.Execute(); }
            catch (Exception exception) { return new BeeQAResult(false, 0d, DateTime.UtcNow, exception.Message); }
        }

        public static IReadOnlyList<BeeQAResult> RunAll()
        {
            EnsureDiscovered();
            lastRunResults.Clear();
            for (int i = 0; i < modules.Count; i++) lastRunResults.Add(Run(modules[i]));
            return lastRunResults;
        }

        public static int CountFor(BeeQACategory category)
        {
            EnsureDiscovered();
            int count = 0;
            for (int i = 0; i < modules.Count; i++)
                if (modules[i].Category == category) count++;
            return count;
        }

        public static void RefreshDiscovery()
        {
            modules.Clear();
            lastRunResults.Clear();
            discovered = false;
            EnsureDiscovered();
        }
    }
}
