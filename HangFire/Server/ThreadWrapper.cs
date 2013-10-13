using System;
using System.Threading;

namespace HangFire.Server
{
    internal class ThreadWrapper : IDisposable
    {
        private readonly IThreadWrappable _wrappable;
        private readonly Thread _thread;

        public ThreadWrapper(IThreadWrappable wrappable)
        {
            if (wrappable == null) throw new ArgumentNullException("wrappable");

            _wrappable = wrappable;

            _thread = new Thread(_wrappable.Work)
                {
                    Name = String.Format("HangFire.{0}", wrappable.GetType().Name)
                };
            _thread.Start();
        }

        public void Dispose()
        {
            _wrappable.Dispose(_thread);

            var disposable = _wrappable as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}