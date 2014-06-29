// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Hangfire.Common;

namespace Hangfire.States
{
    public class FailedState : IState
    {
        public static readonly string StateName = "Failed";

        public FailedState(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            FailedAt = DateTime.UtcNow;
            Exception = exception;
        }

        public DateTime FailedAt { get; set; }
        public Exception Exception { get; set; }

        public string Name { get { return StateName; } }
        public string Reason { get; set; }
        public bool IsFinal { get { return false; } }
        public bool IgnoreJobLoadException { get { return false; } }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "FailedAt", JobHelper.SerializeDateTime(FailedAt) },
                { "ExceptionType", Exception.GetType().FullName },
                { "ExceptionMessage", Exception.Message },
                { "ExceptionDetails", Exception.ToString() }
            };
        }
    }
}
