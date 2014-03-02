using System;

namespace HangFire.SqlServer
{
    [Serializable]
    public class SqlServerApplicationLockException : Exception
    {
        public SqlServerApplicationLockException(string message)
            : base(message)
        {
        }
    }
}