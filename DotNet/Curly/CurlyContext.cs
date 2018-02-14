using System;
using System.Threading;
using Curly.Helpers;
#if !NETSTANDARD2_0
using System.Runtime.Remoting.Messaging;
#endif

namespace Curly
{
    public class CurlyContext
    {
#if !NETSTANDARD2_0
        private static readonly string ContextName = nameof(ContextName);
#else
        private static AsyncLocal<SimpleContainer> Context = new AsyncLocal<SimpleContainer>();
#endif
        private static SimpleContainer EnsureContainer
        {
            get
            {
#if !NETSTANDARD2_0
                var cont = (SimpleContainer)CallContext.LogicalGetData(ContextName);
                if (cont == null)
                    CallContext.LogicalSetData(ContextName, cont = new SimpleContainer());
#else
                var cont = Context.Value;
                if (cont == null)
                    Context.Value = cont = new SimpleContainer();
#endif
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