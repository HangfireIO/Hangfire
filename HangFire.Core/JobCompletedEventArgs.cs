using System;

namespace HangFire
{
    internal class JobCompletedEventArgs : EventArgs
    {
        public JobCompletedEventArgs(string serializedJob)
        {
            SerializedJob = serializedJob;
        }

        public string SerializedJob { get; private set; }
    }
}