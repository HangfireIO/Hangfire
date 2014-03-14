using System;

namespace HangFire.SqlServer
{
    [Serializable]
    internal class SqlServerDistributedLockException : Exception
    {
        public SqlServerDistributedLockException(string message)
            : base(message)
        {
        }
    }
}