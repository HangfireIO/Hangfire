using System;
using System.Runtime.Serialization;

namespace HangFire
{
    [Serializable]
    public class JobInvocationException : Exception
    {
        public JobInvocationException()
        {
        }

        public JobInvocationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public JobInvocationException(string message)
            : base(message)
        {
        }

        public JobInvocationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class JobActivationException : Exception
    {
        public JobActivationException()
        {
        }

        public JobActivationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public JobActivationException(string message)
            : base(message)
        {
        }

        public JobActivationException(string message, Exception innerException)
            : base(message, innerException)
        {
        } 
    }
}
