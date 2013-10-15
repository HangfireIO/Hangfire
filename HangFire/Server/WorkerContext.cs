using System;
using System.Collections.Generic;
using ServiceStack.Redis;

namespace HangFire.Server
{
    public class WorkerContext : ServerContext
    {
        internal WorkerContext(WorkerContext workerContext)
            : this (workerContext, workerContext.WorkerNumber)
        {
        }

        internal WorkerContext(ServerContext serverContext, int workerNumber)
            : base(serverContext)
        {
            WorkerNumber = workerNumber;
        }

        public int WorkerNumber { get; private set; }
        
    }
}