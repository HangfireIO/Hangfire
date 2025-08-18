// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
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
using System.Linq;
using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;

namespace Hangfire
{
    public abstract class JobStorage
    {
        private TimeSpan _jobExpirationTimeout = TimeSpan.FromDays(1);

        [NotNull]
        public static JobStorage Current
        {
            get => GlobalConfiguration.Configuration.ResolveService<JobStorage>();

            [Obsolete]
            set => GlobalConfiguration.Configuration.UseStorageUnsafe(value);
        }

        public TimeSpan JobExpirationTimeout
        {
            get => _jobExpirationTimeout;
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentException("JobStorage.JobExpirationTimeout value should be equal or greater than zero.", nameof(value));
                }

                _jobExpirationTimeout = value;
            }
        }

        public virtual bool LinearizableReads => false;

        [NotNull]
        public abstract IMonitoringApi GetMonitoringApi();

        [NotNull]
        public abstract IStorageConnection GetConnection();

        [NotNull]
        public virtual IStorageConnection GetReadOnlyConnection()
        {
            return GetConnection();
        }

#pragma warning disable 618
        [Obsolete($"Please use the `{nameof(GetStorageWideProcesses)}` and/or `{nameof(GetServerRequiredProcesses)}` methods instead, and enable `{nameof(JobStorageFeatures)}.{nameof(JobStorageFeatures.ProcessesInsteadOfComponents)}`. Will be removed in 2.0.0.")]
        [NotNull]
        public virtual IEnumerable<IServerComponent> GetComponents()
        {
            return Enumerable.Empty<IServerComponent>();
        }
#pragma warning restore 618

        [NotNull]
        public virtual IEnumerable<IBackgroundProcess> GetServerRequiredProcesses()
        {
            return Enumerable.Empty<IBackgroundProcess>();
        }

        [NotNull]
        public virtual IEnumerable<IBackgroundProcess> GetStorageWideProcesses()
        {
            return Enumerable.Empty<IBackgroundProcess>();
        }

        [NotNull]
        public virtual IEnumerable<IStateHandler> GetStateHandlers()
        {
            return Enumerable.Empty<IStateHandler>();
        }

        public virtual void WriteOptionsToLog([NotNull] ILog logger)
        {
        }

        public virtual bool HasFeature([NotNull] string featureId)
        {
            if (featureId == null) throw new ArgumentNullException(nameof(featureId));
            return false;
        }
    }
}
