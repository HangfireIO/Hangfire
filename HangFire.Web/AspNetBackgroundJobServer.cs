// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System.Web.Hosting;

namespace HangFire.Web
{
    /// <summary>
    /// Represents the HangFire server that implements the
    /// <see cref="IRegisteredObject"/> interface. 
    /// </summary>
    public class AspNetBackgroundJobServer : BackgroundJobServer, IRegisteredObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetBackgroundJobServer"/>
        /// class with the number of workers and the list of queues that will 
        /// be processed by this instance of a server.
        /// </summary>
        /// <param name="workerCount">The number of workers.</param>
        /// <param name="queues">The list of queues that will be processed.</param>
        public AspNetBackgroundJobServer(int workerCount, params string[] queues)
            : base(workerCount, queues)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetBackgroundJobServer"/>
        /// class with the default number of workers and the specified list of
        /// queues that will be processed by this instance of a server.
        /// </summary>
        /// <param name="queues">The list of queues that will be processed.</param>
        public AspNetBackgroundJobServer(params string[] queues)
            : base(queues)
        {
        }

        /// <summary>
        /// Starts the server and places it in the list of registered
        /// objects in the application. 
        /// </summary>
        public override void Start()
        {
            base.Start();
            HostingEnvironment.RegisterObject(this);
        }

        /// <summary>
        /// Disposes the server and removes it from the list of registered
        /// objects in the application.
        /// </summary>
        public override bool Stop()
        {
            var wasStopped = base.Stop();
            if (wasStopped)
            {
                HostingEnvironment.UnregisterObject(this);
            }

            return wasStopped;
        }

        void IRegisteredObject.Stop(bool immediate)
        {
            Stop();
        }
    }
}
