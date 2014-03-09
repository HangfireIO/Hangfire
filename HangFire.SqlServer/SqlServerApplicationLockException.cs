using System;

namespace HangFire.SqlServer
{
    [Serializable]
    internal class SqlServerApplicationLockException : Exception
    {
        public SqlServerApplicationLockException(string message)
            : base(message)
        {
        }
    }
}