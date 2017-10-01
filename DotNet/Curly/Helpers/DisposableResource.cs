using System;
using System.Diagnostics;

namespace Curly.Helpers
{
    class DisposableResource<T> : IDisposable
    {
        private readonly Action<T> _store;

        public DisposableResource(Func<T> init, Action<T> store)
        {
            Debug.Assert(init != null);
            Debug.Assert(store != null);

            _store = store;
            Object = init();
        }

        public T Object { get; private set; }

        ~DisposableResource()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
            // This method will remove current object from garbage collector's queue 
            // and stop calling finilize method twice 
        }

        public void Dispose(bool disposer)
        {
            if (disposer)
            {
                // dispose the managed objects
                _store(Object);
            }
            // dispose the unmanaged objects
        }
    }

}
