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