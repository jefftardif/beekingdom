using System;
using System.Collections.Generic;
using System.Linq;
using BeeKingdom.Core.Logging;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Services
{
    public sealed class ServiceLifecycleOrchestrator
    {
        private readonly ServiceContainer container;
        private readonly IBeeLogger logger;
        private readonly Dictionary<Type, IGameService> servicesByContract = new Dictionary<Type, IGameService>();
        private readonly List<IGameService> registeredServices = new List<IGameService>();
        private List<IGameService> orderedServices = new List<IGameService>();
        private List<IGameService> runningServices = new List<IGameService>();

        public IReadOnlyList<IGameService> OrderedServices => orderedServices;

        public ServiceLifecycleOrchestrator(ServiceContainer container, IBeeLogger logger)
        {
            this.container = container;
            this.logger = logger;
        }

        public void Register<TContract>(TContract service) where TContract : class
        {
            container.Register(service);

            if (service is IGameService gameService)
            {
                servicesByContract[typeof(TContract)] = gameService;
                if (!registeredServices.Contains(gameService))
                {
                    registeredServices.Add(gameService);
                }
            }
        }

        public void Bootstrap()
        {
            ValidateDependencyGraph();
            orderedServices = BuildStartupOrder();

            foreach (IGameService service in orderedServices)
            {
                if (HasFailedDependency(service))
                {
                    service.Fail(new InvalidOperationException("A dependency failed before this service could start."));
                    logger.Log(BeeLogLevel.Error, $"{service.ServiceName} was not started because a dependency failed.");
                    continue;
                }

                try
                {
                    service.Initialize(container);
                    service.Start();
                }
                catch (Exception exception)
                {
                    service.Fail(exception);
                    logger.Log(BeeLogLevel.Error, $"{service.ServiceName} failed: {exception.Message}");
                }
            }

            runningServices = orderedServices
                .Where(service => service.State == ServiceState.Running)
                .ToList();
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < runningServices.Count; i++)
            {
                runningServices[i].Tick(deltaTime);
            }
        }

        public void FixedTick(float deltaTime)
        {
            for (int i = 0; i < runningServices.Count; i++)
            {
                runningServices[i].FixedTick(deltaTime);
            }
        }

        public void LateTick(float deltaTime)
        {
            for (int i = 0; i < runningServices.Count; i++)
            {
                runningServices[i].LateTick(deltaTime);
            }
        }

        public void Pause()
        {
            foreach (IGameService service in runningServices)
            {
                service.Pause();
            }
        }

        public void Resume()
        {
            foreach (IGameService service in runningServices)
            {
                if (service.State == ServiceState.Paused)
                {
                    service.Resume();
                }
            }
        }

        public void Shutdown()
        {
            for (int i = orderedServices.Count - 1; i >= 0; i--)
            {
                IGameService service = orderedServices[i];
                if (service.State == ServiceState.Initialized || service.State == ServiceState.Running || service.State == ServiceState.Paused || service.State == ServiceState.Failed)
                {
                    try
                    {
                        service.Shutdown();
                    }
                    catch (Exception exception)
                    {
                        service.Fail(exception);
                        logger.Log(BeeLogLevel.Error, $"{service.ServiceName} shutdown failed: {exception.Message}");
                    }
                }
            }

            runningServices.Clear();
            orderedServices.Clear();
        }

        private void ValidateDependencyGraph()
        {
            foreach (IGameService service in registeredServices)
            {
                foreach (Type dependency in service.Dependencies)
                {
                    if (!servicesByContract.ContainsKey(dependency))
                    {
                        throw new InvalidOperationException($"{service.ServiceName} depends on missing service {dependency.Name}.");
                    }
                }
            }

            HashSet<Type> visiting = new HashSet<Type>();
            HashSet<Type> visited = new HashSet<Type>();
            foreach (Type serviceType in servicesByContract.Keys)
            {
                if (HasCycle(serviceType, visiting, visited))
                {
                    throw new InvalidOperationException($"Circular service dependency detected at {serviceType.Name}.");
                }
            }
        }

        private List<IGameService> BuildStartupOrder()
        {
            List<IGameService> ordered = new List<IGameService>();
            HashSet<Type> visited = new HashSet<Type>();

            foreach (KeyValuePair<Type, IGameService> pair in servicesByContract.OrderBy(pair => pair.Value.Priority).ThenBy(pair => pair.Value.ServiceName))
            {
                Visit(pair.Key, visited, ordered);
            }

            return ordered.Distinct().ToList();
        }

        private void Visit(Type contractType, HashSet<Type> visited, List<IGameService> ordered)
        {
            if (visited.Contains(contractType))
            {
                return;
            }

            visited.Add(contractType);
            IGameService service = servicesByContract[contractType];
            foreach (Type dependency in service.Dependencies.OrderBy(type => servicesByContract[type].Priority))
            {
                Visit(dependency, visited, ordered);
            }

            ordered.Add(service);
        }

        private bool HasCycle(Type contractType, HashSet<Type> visiting, HashSet<Type> visited)
        {
            if (visited.Contains(contractType)) return false;
            if (visiting.Contains(contractType)) return true;

            visiting.Add(contractType);
            foreach (Type dependency in servicesByContract[contractType].Dependencies)
            {
                if (HasCycle(dependency, visiting, visited))
                {
                    return true;
                }
            }

            visiting.Remove(contractType);
            visited.Add(contractType);
            return false;
        }

        private bool HasFailedDependency(IGameService service)
        {
            foreach (Type dependency in service.Dependencies)
            {
                if (servicesByContract[dependency].State == ServiceState.Failed)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
