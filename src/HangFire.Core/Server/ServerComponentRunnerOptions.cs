// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;

namespace HangFire.Server
{
    internal class ServerComponentRunnerOptions
    {
        private int _maxRetryAttempts;
        private TimeSpan _shutdownTimeout;

        public ServerComponentRunnerOptions()
        {
            MaxRetryAttempts = 10;
            ShutdownTimeout = TimeSpan.FromSeconds(5);
            MinimumLogVerbosity = false;
        }

        public int MaxRetryAttempts
        {
            get { return _maxRetryAttempts; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        "value",
                        "MaxRetryAttempts property value must be greater or equal to 0.");
                }

                _maxRetryAttempts = value;
            }
        }

        public TimeSpan ShutdownTimeout
        {
            get { return _shutdownTimeout; }
            set
            {
                if (value != TimeSpan.Zero && value == value.Duration().Negate())
                {
                    throw new ArgumentOutOfRangeException(
                        "value",
                        "ShutdownTimeout property value must be positive.");    
                }

                _shutdownTimeout = value;
            }
        }

        public bool MinimumLogVerbosity { get; set; }
    }
}