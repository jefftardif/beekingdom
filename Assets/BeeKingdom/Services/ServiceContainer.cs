using System;
using System.Collections.Generic;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Services
{
    public sealed class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public void Register<TService>(TService service) where TService : class
        {
            services[typeof(TService)] = service;
        }

        public bool TryGet<TService>(out TService service) where TService : class
        {
            if (services.TryGetValue(typeof(TService), out object value))
            {
                service = value as TService;
                return service != null;
            }

            service = null;
            return false;
        }

        public TService Get<TService>() where TService : class
        {
            if (TryGet(out TService service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service {typeof(TService).Name} is not registered.");
        }

        public void Clear()
        {
            services.Clear();
        }
    }
}
