﻿// This file is part of Hangfire.
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
using Hangfire.Annotations;
using Hangfire.Common;

namespace Hangfire.States
{
    public class StateContext
    {
        public StateContext([NotNull] string jobId, [CanBeNull] Job job, DateTime createdAt)
        {
            if (String.IsNullOrEmpty(jobId)) throw new ArgumentNullException("jobId");
            
            JobId = jobId;
            Job = job;
            CreatedAt = createdAt;
        }

        internal StateContext(StateContext context)
            : this(context.JobId, context.Job, context.CreatedAt)
        {
        }

        [NotNull]
        public string JobId { get; private set; }
        [CanBeNull]
        public Job Job { get; private set; }
        public DateTime CreatedAt { get; private set; }
    }
}