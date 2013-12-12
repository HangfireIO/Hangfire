using System;
using System.Runtime.Serialization;

namespace HangFire.Client
{
    /// <summary>
    /// The exception that is thrown when a job could not
    /// be loaded from the storage due to missing or incorrect 
    /// information about its type or method.
    /// </summary>
    [Serializable]
    public class JobLoadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobLoadException"/>
        /// class.
        /// </summary>
        public JobLoadException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobLoadException"/>
        /// class with a given message.
        /// </summary>
        public JobLoadException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobLoadException"/>
        /// class with a given message and information about inner exception.
        /// </summary>
        public JobLoadException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobLoadException"/>
        /// class with given serialization info and streaming context.
        /// </summary>
        protected JobLoadException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}