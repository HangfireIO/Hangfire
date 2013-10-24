using System;
using System.Collections.Generic;

namespace HangFire.Server
{
    internal class DisposableCollection<T> : List<T>
        where T : IDisposable
    {
        public void Dispose()
        {
            if (typeof (IStoppable).IsAssignableFrom(typeof (T)))
            {
                foreach (var item in this)
                {
                    ((IStoppable)item).Stop();
                }
            }

            foreach (var item in this)
            {
                item.Dispose();
            }
        }
    }
}
