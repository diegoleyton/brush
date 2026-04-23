using System;
using System.Collections.Generic;

namespace Flowbit.Utilities.Core.Services
{
    /// <summary>
    /// Manages services, where you can register, unregister and get services.
    /// </summary>
    public sealed class ServiceContainer
    {
        private Dictionary<Type, object> singleServices_ = new Dictionary<Type, object>();
        private Dictionary<Type, Dictionary<int, object>> multiServices_ = new Dictionary<Type, Dictionary<int, object>>();

        /// <summary>
        /// Register a service of a certain type
        /// </summary>
        public void Register<T>(T service)
        {
            var type = typeof(T);
            singleServices_[type] = service;
        }

        /// <summary>
        /// Register a service of a certain type, using an id
        /// </summary>
        public void Register<T>(int id, T service)
        {
            var type = typeof(T);
            if (!multiServices_.ContainsKey(type))
                multiServices_[type] = new Dictionary<int, object>();

            multiServices_[type][id] = service;
        }

        /// <summary>
        /// Get a service of a certain type
        /// </summary>
        public T Get<T>()
        {
            var type = typeof(T);
            if (singleServices_.TryGetValue(type, out var instance))
                return (T)instance;

            throw new Exception($"No service registered for type {type}");
        }

        /// <summary>
        /// Get a service of a certain type and Id
        /// </summary>
        public T Get<T>(int id)
        {
            var type = typeof(T);
            if (multiServices_.TryGetValue(type, out var dict) && dict.TryGetValue(id, out var instance))
                return (T)instance;

            throw new Exception($"No service registered for type {type} with id {id}");
        }

        /// <summary>
        /// Try to get a service of a certain type
        /// </summary>
        public bool TryGet<T>(out T service)
        {
            var type = typeof(T);
            if (singleServices_.TryGetValue(type, out var instance))
            {
                service = (T)instance;
                return true;
            }
            service = default(T);
            return false;
        }

        /// <summary>
        /// Try to get a service of a certain type and Id
        /// </summary>
        public bool TryGet<T>(int id, out T service)
        {
            var type = typeof(T);
            if (multiServices_.TryGetValue(type, out var dict) && dict.TryGetValue(id, out var instance))
            {
                service = (T)instance;
                return true;
            }
            service = default(T);
            return false;
        }

        /// <summary>
        /// Unregister a service of a certain type
        /// </summary>
        public void Unregister<T>()
        {
            singleServices_.Remove(typeof(T));
        }

        /// <summary>
        /// Unregister a service of a certain type and Id
        /// </summary>
        public void Unregister<T>(int id)
        {
            var type = typeof(T);
            if (multiServices_.TryGetValue(type, out var dict))
            {
                dict.Remove(id);
                if (dict.Count == 0)
                    multiServices_.Remove(type);
            }
        }
    }
}