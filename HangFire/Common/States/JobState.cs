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

using System.Collections.Generic;

namespace HangFire.Common.States
{
    public abstract class JobState
    {
        protected JobState(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; private set; }

        public abstract string StateName { get; }

        public abstract void Apply(StateApplyingContext context);
        
        public virtual IDictionary<string, string> GetProperties(JobMethod data)
        {
            return new Dictionary<string, string>();
        }
    }
}
