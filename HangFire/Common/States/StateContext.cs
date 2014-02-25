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

namespace HangFire.Common.States
{
    public class StateContext
    {
        internal StateContext(StateContext context)
            : this(context.JobId, context.JobMethod)
        {
        }

        internal StateContext(string jobId, JobMethod jobMethod)
        {
            JobId = jobId;
            JobMethod = jobMethod;
        }

        public string JobId { get; private set; }
        public JobMethod JobMethod { get; private set; }
    }
}