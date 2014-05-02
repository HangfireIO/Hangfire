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
using System.Collections;
using System.Collections.Generic;

namespace HangFire.Server
{
    internal class ServerSupervisorCollection : IServerSupervisor, ICollection<IServerSupervisor>
    {
        private readonly List<IServerSupervisor> _supervisors;

        public ServerSupervisorCollection(IEnumerable<IServerSupervisor> supervisors)
        {
            if (supervisors == null) throw new ArgumentNullException("supervisors");

            _supervisors = new List<IServerSupervisor>(supervisors);
        }

        public int Count { get { return _supervisors.Count; } }
        public bool IsReadOnly { get { return false; } }

        public void Start()
        {
            foreach (var supervisor in _supervisors)
            {
                supervisor.Start();
            }
        }

        public void Stop()
        {
            foreach (var supervisor in _supervisors)
            {
                supervisor.Stop();
            }
        }

        public void Dispose()
        {
            Stop();

            foreach (var supervisor in _supervisors)
            {
                supervisor.Dispose();
            }
        }

        public IEnumerator<IServerSupervisor> GetEnumerator()
        {
            return _supervisors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _supervisors).GetEnumerator();
        }

        public void Add(IServerSupervisor item)
        {
            _supervisors.Add(item);
        }

        public void Clear()
        {
            _supervisors.Clear();
        }

        public bool Contains(IServerSupervisor item)
        {
            return _supervisors.Contains(item);
        }

        public void CopyTo(IServerSupervisor[] array, int arrayIndex)
        {
            _supervisors.CopyTo(array, arrayIndex);
        }

        public bool Remove(IServerSupervisor item)
        {
            return _supervisors.Remove(item);
        }
    }
}