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

using System;
using System.Collections.Generic;
using HangFire.Common;
using HangFire.Common.States;

namespace HangFire.States
{
    public class ProcessingState : State
    {
        public static readonly string Name = "Processing";
        private readonly string _serverName;

        public ProcessingState(string serverName)
        {
            if (String.IsNullOrWhiteSpace(serverName)) {throw new ArgumentNullException("serverName");}
            _serverName = serverName;
        }

        public override string StateName { get { return Name; } }

        public override IDictionary<string, string> GetData(JobMethod data)
        {
            return new Dictionary<string, string>
                {
                    { "StartedAt", JobHelper.ToStringTimestamp(DateTime.UtcNow) },
                    { "ServerName", _serverName }
                };
        }
    }
}
