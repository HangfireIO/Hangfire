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

namespace Hangfire
{
    public interface IJobExecutionContext : IJobCancellationToken
    {
        /// <summary>
        /// Report the current status of a job, potentially to an end-user, along with an
        /// estimate of the % of the job that is completed.
        /// </summary>
        /// <param name="percentComplete"></param>
        /// <param name="currentStatus"></param>
        void UpdateProgress(int percentComplete, string currentStatus);
        
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarn(string message);
        void LogError(string message);
        void LogFatal(string message);
    }
}
