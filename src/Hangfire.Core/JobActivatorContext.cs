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
using System.Collections.Generic;
using System.Threading;
using Hangfire.Annotations;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

namespace Hangfire
{
    public class JobActivatorContext
    {
        internal JobActivatorContext(
            [NotNull] IStorageConnection connection,
            [NotNull] BackgroundJob backgroundJob,
            [NotNull] IJobCancellationToken cancellationToken)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (backgroundJob == null) throw new ArgumentNullException("backgroundJob");
            if (cancellationToken == null) throw new ArgumentNullException("cancellationToken");

            Connection = connection;
            BackgroundJob = backgroundJob;
            CancellationToken = cancellationToken;
        }

        [NotNull]
        public BackgroundJob BackgroundJob { get; private set; }

        [NotNull]
        public IJobCancellationToken CancellationToken { get; private set; }

        [NotNull]
        public IStorageConnection Connection { get; private set; }

        public void SetJobParameter(string name, object value)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            Connection.SetJobParameter(BackgroundJob.Id, name, JobHelper.ToJson(value));
        }

        public T GetJobParameter<T>(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            try
            {
                return JobHelper.FromJson<T>(Connection.GetJobParameter(BackgroundJob.Id, name));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(String.Format(
                    "Could not get a value of the job parameter `{0}`. See inner exception for details.",
                    name), ex);
            }
        }
    }
}