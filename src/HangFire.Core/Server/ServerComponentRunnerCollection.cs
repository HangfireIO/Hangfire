using System;
using System.Collections.Generic;

namespace HangFire.Server
{
    internal class ServerComponentRunnerCollection : IServerComponentRunner
    {
        private readonly IEnumerable<IServerComponentRunner> _runners;

        public ServerComponentRunnerCollection(IEnumerable<IServerComponentRunner> runners)
        {
            if (runners == null) throw new ArgumentNullException("runners");

            _runners = runners;
        }

        public void Start()
        {
            foreach (var runner in _runners)
            {
                runner.Start();
            }
        }

        public void Stop()
        {
            foreach (var runner in _runners)
            {
                runner.Stop();
            }
        }

        public void Dispose()
        {
            Stop();

            foreach (var runner in _runners)
            {
                runner.Dispose();
            }
        }
    }
}