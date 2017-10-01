using System;
using System.Runtime.Remoting.Messaging;
using Similarweb.Curly.Helpers;

namespace Similarweb.Curly
{
    public class CurlyContext
    {
        private static readonly string ContextName = nameof(ContextName);

        private static SimpleContainer EnsureContainer
        {
            get
            {
                var cont = (SimpleContainer)CallContext.LogicalGetData(ContextName);
                if (cont == null)
                    CallContext.LogicalSetData(ContextName, cont = new SimpleContainer());
                return cont;
            }
        }

        public static T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        public static object GetService(Type t)
        {
            return EnsureContainer.GetInstance(t, null);
        }

        public static IDisposable Push()
        {
            return EnsureContainer.PushScope();
        }

        public static IDisposable Push<T>(object implementation)
        {
            var d = EnsureContainer.PushScope();
            RegisterInstance<T>(implementation);
            return d;
        }
        public static IDisposable Push<T>()
        {
            var d = EnsureContainer.PushScope();
            RegisterInstance<T>(Activator.CreateInstance<T>());
            return d;
        }

        public static void RegisterInstance<T>(object implementation)
        {
            if (!(implementation is T))
                throw new InvalidOperationException($"Implementation does not implement {typeof(T)}");
            EnsureContainer.RegisterInstance(typeof(T), null, implementation);
        }

        public static void RegisterPerRequest<T, TImpl>() where TImpl : T
        {
            EnsureContainer.RegisterPerRequest(typeof(T), null, typeof(TImpl));
        }

        public void RegisterSingleton<T, TImpl>() where TImpl : T
        {
            EnsureContainer.RegisterSingleton(typeof(T), null, typeof(TImpl));
        }

        public void RegisterHandler<T>(Func<object> handler)
        {
            EnsureContainer.RegisterHandler(typeof(T), null, a => handler());
        }
    }
}