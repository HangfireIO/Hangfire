using System;

namespace HangFire
{
    public class JobCompletedEventArgs : EventArgs
    {
        public JobCompletedEventArgs(string serializedJob)
        {
            SerializedJob = serializedJob;
        }

        public string SerializedJob { get; private set; }
    }
}