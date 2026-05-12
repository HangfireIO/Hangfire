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
using System.Threading.Tasks;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Storage;

namespace Hangfire.Server
{
    internal sealed class JobServerResourceReporterProcess : IBackgroundProcessAsync
    {
        private readonly IJobServerResourceReporter _reporter;
        private readonly ILog _logger = LogProvider.GetLogger(typeof(JobServerResourceReporterProcess));
        private bool _attempted;

        public JobServerResourceReporterProcess(IJobServerResourceReporter reporter)
        {
            _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
        }

        public async Task ExecuteAsync(BackgroundProcessContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (!_reporter.IsConfigured)
            {
                context.ShutdownToken.WaitOrThrow(TimeSpan.FromSeconds(1));
                return;
            }

            if (_attempted)
            {
                context.ShutdownToken.WaitOrThrow(_reporter.Interval);
            }

            _attempted = true;

            try
            {
                await _reporter.ComputeCapacityAsync(context.ShutdownToken).ConfigureAwait(false);

                using (var connection = context.Storage.GetConnection())
                {
                    if (connection is JobStorageConnection storageConnection)
                    {
                        storageConnection.UpdateServer(context.ServerId, BackgroundServerProcess.GetServerContext(CopyProperties(context)));
                    }
                }
            }
            catch (OperationCanceledException) when (context.ShutdownToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                _reporter.ReportCapacityCheckFailure(ex);

                try
                {
                    using (var connection = context.Storage.GetConnection())
                    {
                        if (connection is JobStorageConnection storageConnection)
                        {
                            storageConnection.UpdateServer(context.ServerId, BackgroundServerProcess.GetServerContext(CopyProperties(context)));
                        }
                    }
                }
                catch (Exception updateException) when (updateException.IsCatchableExceptionType())
                {
                    _logger.WarnException(
                        $"{BackgroundServerProcess.GetServerTemplate(context.ServerId)} encountered an exception while updating failed capacity metadata",
                        updateException);
                }

                _logger.WarnException(
                    $"{BackgroundServerProcess.GetServerTemplate(context.ServerId)} encountered an exception while reporting server capacity",
                    ex);
            }
        }

        private static IDictionary<string, object> CopyProperties(BackgroundProcessContext context)
        {
            var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in context.Properties)
            {
                properties.Add(property.Key, property.Value);
            }

            return properties;
        }
    }
}
