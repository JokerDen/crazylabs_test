using System;
using System.Collections.Generic;

namespace SlingshotRunner
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Services[typeof(T)] = service;
        }

        public static void Unregister<T>(T service) where T : class
        {
            if (service == null)
            {
                return;
            }

            Type type = typeof(T);
            if (Services.TryGetValue(type, out object registeredService) && ReferenceEquals(registeredService, service))
            {
                Services.Remove(type);
            }
        }

        public static T Get<T>() where T : class
        {
            if (TryGet(out T service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service is not registered: {typeof(T).Name}");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object registeredService))
            {
                service = registeredService as T;
                return service != null;
            }

            service = null;
            return false;
        }
    }
}
