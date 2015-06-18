using Hangfire.Annotations;
using Hangfire.Logging;
using Hangfire.States;
using Hangfire.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Server
{
    internal class ServerJobCallback : IJobCallback
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private readonly string _jobId;
        private readonly CancellationToken _shutdownToken;
        private readonly IStorageConnection _connection;
        private readonly WorkerContext _workerContext;

        public ServerJobCallback(
            [NotNull] string jobId,
            [NotNull] IStorageConnection connection,
            [NotNull] WorkerContext workerContext,
            CancellationToken shutdownToken)
        {
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (connection == null) throw new ArgumentNullException("connection");
            if (workerContext == null) throw new ArgumentNullException("workerContext");

            _jobId = jobId;
            _shutdownToken = shutdownToken;
            _connection = connection;
            _workerContext = workerContext;
        }

        public CancellationToken ShutdownToken
        {
            get { return _shutdownToken; }
        }

        public void ThrowIfCancellationRequested()
        {
            _shutdownToken.ThrowIfCancellationRequested();

            if (IsJobAborted())
            {
                throw new JobAbortedException();
            }
        }

        private bool IsJobAborted()
        {
            var state = _connection.GetStateData(_jobId);

            if (state == null)
            {
                return true;
            }

            if (!state.Name.Equals(ProcessingState.StateName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!state.Data["ServerId"].Equals(_workerContext.ServerId))
            {
                return true;
            }

            if (state.Data["WorkerNumber"] != _workerContext.WorkerNumber.ToString(CultureInfo.InvariantCulture))
            {
                return true;
            }

            return false;
        }

        public void UpdateProgress(int percentComplete, string currentStatus)
        {
            using (var transaction = _connection.CreateWriteTransaction())
            {
                var state = new ProcessingSubState();
                var message = string.Format("{0}%: {1}", percentComplete, currentStatus);
                Logger.InfoFormat("[UpdateProgress] {0}", message);
                state.Reason = message;
                transaction.SetJobState(_jobId, state);
                transaction.Commit();
            }
        }

        private void Log(string loglevel, string message)
        {
            using (var transaction = _connection.CreateWriteTransaction())
            {
                var state = new ProcessingLogState(loglevel, message);
                Logger.InfoFormat("[Log] {0}: {1}", state.Name, state.Reason);
                transaction.AddJobState(_jobId, state);
                transaction.Commit();
            }
        }

        public void LogDebug(string message)
        {
            this.Log("DEBUG", message);
        }

        public void LogInfo(string message)
        {
            this.Log("INFO", message);
        }

        public void LogWarn(string message)
        {
            this.Log("WARN", message);
        }

        public void LogError(string message)
        {
            this.Log("ERROR", message);
        }

        public void LogFatal(string message)
        {
            this.Log("FATAL", message);
        }
    }
}
