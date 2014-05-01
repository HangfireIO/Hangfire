// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

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
        /// class with the default options and places it to the list of registered
        /// objects in the application. .
        /// </summary>
        public AspNetBackgroundJobServer()
        {
            HostingEnvironment.RegisterObject(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetBackgroundJobServer"/>
        /// class with the given options and places it to the list of registered 
        /// objects in the application. 
        /// </summary>
        public AspNetBackgroundJobServer(BackgroundJobServerOptions options)
            : base(options)
        {
            HostingEnvironment.RegisterObject(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetBackgroundJobServer"/>
        /// class with the given options and job storage, and places it to the list 
        /// of registered 
        /// objects in the application. 
        /// </summary>
        public AspNetBackgroundJobServer(BackgroundJobServerOptions options, JobStorage storage)
            : base(options, storage)
        {
            HostingEnvironment.RegisterObject(this);
        }

        /// <summary>
        /// Disposes the server and removes it from the list of registered
        /// objects in the application.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            HostingEnvironment.UnregisterObject(this);
        }

        void IRegisteredObject.Stop(bool immediate)
        {
            Dispose();
        }
    }
}
