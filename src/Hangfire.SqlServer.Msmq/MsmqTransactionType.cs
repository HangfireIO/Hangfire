namespace Hangfire.SqlServer.Msmq
{
    public enum MsmqTransactionType
    {
        /// <summary>
        /// Internal (MSMQ) transaction will be used to fetch pending background
        /// jobs, does not support remote queues.
        /// </summary>
        Internal,

        /// <summary>
        /// External (DTC) transaction will be used to fetch pending background
        /// jobs. Supports remote queues, but requires running MSDTC Service.
        /// </summary>
        Dtc
    }
}