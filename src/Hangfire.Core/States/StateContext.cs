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
using Hangfire.Annotations;
using Hangfire.Common;

namespace Hangfire.States
{
    public class StateContext
    {
        public StateContext([NotNull] JobStorage storage, [NotNull] BackgroundJob backgroundJob)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            if (backgroundJob == null) throw new ArgumentNullException("backgroundJob");

            Storage = storage;
            BackgroundJob = backgroundJob;
        }

        internal StateContext(StateContext context)
            : this(context.Storage, context.BackgroundJob)
        {
        }

        [NotNull]
        public JobStorage Storage { get; private set; }

        [NotNull]
        public BackgroundJob BackgroundJob { get; private set; }

        [NotNull]
        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public string JobId { get { return BackgroundJob.Id; } }

        [CanBeNull]
        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public Job Job { get { return BackgroundJob.Job; } }

        [Obsolete("Please use BackgroundJob property instead. Will be removed in 2.0.0.")]
        public DateTime CreatedAt { get { return BackgroundJob.CreatedAt; } }
    }
}