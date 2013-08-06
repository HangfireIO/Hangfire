using System;
using System.Collections.Generic;

namespace HangFire
{
    public abstract class Worker : IDisposable
    {
        public IDictionary<string, string> Args { get; set; }

        public abstract void Perform();

        public virtual void Dispose()
        {
        }
    }
}
