//based on, bu not limited to http://caliburnmicro.codeplex.com/wikipage?title=SimpleContainer

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Curly.Helpers
{
    /// <summary>
    /// Simple IOC container
    /// </summary>
    class SimpleContainer
    {
        readonly Stack<List<ContainerEntry>> _scopes = new Stack<List<ContainerEntry>>(new[] { new List<ContainerEntry>() });
        //readonly List<ContainerEntry> _entries = new List<ContainerEntry>();

        public IDisposable PushScope()
        {
            return new DisposableResource<SimpleContainer>(Push, Pop);
        }

        private SimpleContainer Push()
        {
            _scopes.Push(new List<ContainerEntry>());
            return this;
        }

        private void Pop(SimpleContainer container)
        {
            container._scopes.Pop();
        }

        public void RegisterInstance(Type service, string key, object implementation)
        {
            RegisterHandler(service, key, (container) => implementation);
        }

        public void RegisterPerRequest(Type service, string key, Type implementation)
        {
            RegisterHandler(service, key, (container) => BuildInstance(implementation));
        }

        public void RegisterPerLifetime(Func<object> lifeTime, Type service, string key, Type implementation)
        {
            WeakReference wr = null;
            object singleton = null;
            RegisterHandler(service, key, (container) =>
            {
                //GC.Collect();
                if (wr == null || !wr.IsAlive || singleton == null)
                {
                    wr = new WeakReference(lifeTime());
                    singleton = BuildInstance(implementation);
                }
                return singleton;
            });
        }

        public void RegisterPerLifetime(Func<object> lifeTime, Type service, string key, Func<SimpleContainer, object> handler)
        {
            WeakReference wr = null;
            object singleton = null;
            RegisterHandler(service, key, (container) =>
            {
                //GC.Collect();
                if (wr == null || !wr.IsAlive || singleton == null)
                {
                    wr = new WeakReference(lifeTime());
                    singleton = handler(container);
                }
                return singleton;
            });
        }

        public void RegisterSingleton(Type service, string key, Type implementation)
        {
            object singleton = null;
            RegisterHandler(service, key, (container) => (singleton ?? (singleton = BuildInstance(implementation))));
        }

        public void RegisterHandler(Type service, string key, Func<SimpleContainer, object> handler)
        {
            GetOrCreateEntry(service, key).Add(handler);
        }

        public T GetInstance<T>(string key)
        {
            return (T)GetInstance(typeof(T), key);
        }

        public object GetInstance(Type service, string key)
        {
            var entry = GetEntry(service, key);
            if (entry != null)
                return entry.Single()(this);

            if (typeof(Delegate).IsAssignableFrom(service))
            {
                var typeToCreate = service
                    .GetGenericArguments()[0];
                var factoryFactoryType = typeof(FactoryFactory<>).MakeGenericType(typeToCreate);
                var factoryFactoryHost = Activator.CreateInstance(factoryFactoryType);
                var factoryFactoryMethod = factoryFactoryType
                    .GetMethod("Create");
                return factoryFactoryMethod.Invoke(factoryFactoryHost, new object[] { this });
            }
            if (typeof(IEnumerable).IsAssignableFrom(service))
            {
                var listType = service
                    .GetGenericArguments()[0];
                var instances = GetAllInstances(listType).ToList();
                var array = Array.CreateInstance(listType, instances.Count);

                for (var i = 0; i < array.Length; i++)
                {
                    array.SetValue(instances[i], i);
                }

                return array;
            }

            return null;
        }

        public IEnumerable<object> GetAllInstances(Type service)
        {
            var entry = GetEntry(service, null);
            return entry != null ? entry.Select(x => x(this)) : new object[0];
        }

        public void BuildUp(object instance)
        {
            var injectables = from property in instance.GetType().GetProperties()
                where property.CanRead && property.CanWrite && property.PropertyType.IsInterface
                select property;

            foreach (PropertyInfo propertyInfo in injectables)
            {
                var injection = GetAllInstances(propertyInfo.PropertyType);
                if (injection.Any())
                    propertyInfo.SetValue(instance, injection.First(), null);
            }
        }

        ContainerEntry GetOrCreateEntry(Type service, string key)
        {
            var entry = GetTopEntry(service, key);
            if (entry == null)
            {
                entry = new ContainerEntry { Service = service, Key = key };
                _scopes.Peek().Add(entry);
            }

            return entry;
        }

        ContainerEntry GetEntry(Type service, string key)
        {
            //important note here
            //we should return entries only from one scope, if available
            return service == null
                ? _scopes.Select(a => a.FirstOrDefault(x => x.Key == key)).FirstOrDefault(a => a != null)
                : _scopes.Select(a => a.FirstOrDefault(x => x.Service == service && x.Key == key)).FirstOrDefault(a => a != null);
        }
        ContainerEntry GetTopEntry(Type service, string key)
        {
            //important note here
            //we should return entries only from one scope, if available
            return service == null
                ? _scopes.Peek().FirstOrDefault(x => x.Key == key)
                : _scopes.Peek().FirstOrDefault(x => x.Service == service && x.Key == key);
        }

        protected object BuildInstance(Type type)
        {
            var args = DetermineConstructorArgs(type);
            return ActivateInstance(type, args);
        }

        protected virtual object ActivateInstance(Type type, object[] args)
        {
            return args.Length > 0 ? Activator.CreateInstance(type, args) : Activator.CreateInstance(type);
        }

        object[] DetermineConstructorArgs(Type implementation)
        {
            var args = new List<object>();
            var constructor = SelectEligibleConstructor(implementation);

            if (constructor != null)
                args.AddRange(constructor.GetParameters().Select(info => GetInstance(info.ParameterType, null)));

            return args.ToArray();
        }

        static ConstructorInfo SelectEligibleConstructor(Type type)
        {
            return (from c in type.GetConstructors()
                orderby c.GetParameters().Length descending
                select c).FirstOrDefault();
        }

        class ContainerEntry : List<Func<SimpleContainer, object>>
        {
            public string Key;
            public Type Service;
        }

        class FactoryFactory<T>
        {
            public Func<T> Create(SimpleContainer container)
            {
                return () => (T)container.GetInstance(typeof(T), null);
            }
        }
    }
}
