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