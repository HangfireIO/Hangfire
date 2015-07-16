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
using Hangfire.Storage;

namespace Hangfire.States
{
    internal class DefaultStateChangeProcess : IStateChangeProcess
    {
        private readonly StateHandlerCollection _handlers;
        private readonly IJobFilterProvider _filterProvider;

        public DefaultStateChangeProcess([NotNull] StateHandlerCollection handlers)
            : this(handlers, JobFilterProviders.Providers)
        {
        }

        public DefaultStateChangeProcess(
            [NotNull] StateHandlerCollection handlers,
            [NotNull] IJobFilterProvider filterProvider)
        {
            if (handlers == null) throw new ArgumentNullException("handlers");
            if (filterProvider == null) throw new ArgumentNullException("filterProvider");

            _handlers = handlers;
            _filterProvider = filterProvider;
        }

        public void ElectState(IStorageConnection connection, ElectStateContext context)
        {
            var filterInfo = GetFilters(context.Job);
            foreach (var filter in filterInfo.ElectStateFilters)
            {
                filter.OnStateElection(context);
            }
        }

        public void ApplyState(IWriteOnlyTransaction transaction, ApplyStateContext context)
        {
            var filterInfo = GetFilters(context.Job);
            var filters = filterInfo.ApplyStateFilters;

            foreach (var state in context.TraversedStates)
            {
                transaction.AddJobState(context.JobId, state);
            }

            foreach (var handler in _handlers.GetHandlers(context.OldStateName))
            {
                handler.Unapply(context, transaction);
            }

            foreach (var filter in filters)
            {
                filter.OnStateUnapplied(context, transaction);
            }

            transaction.SetJobState(context.JobId, context.NewState);

            foreach (var handler in _handlers.GetHandlers(context.NewState.Name))
            {
                handler.Apply(context, transaction);
            }

            foreach (var filter in filters)
            {
                filter.OnStateApplied(context, transaction);
            }

            if (context.NewState.IsFinal)
            {
                transaction.ExpireJob(context.JobId, context.JobExpirationTimeout);
            }
            else
            {
                transaction.PersistJob(context.JobId);
            }
        }

        private JobFilterInfo GetFilters(Job job)
        {
            return new JobFilterInfo(_filterProvider.GetFilters(job));
        }
    }
}