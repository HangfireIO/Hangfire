﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire
{
    public class JobCallback : JobCancellationToken
    {
        public JobCallback(bool canceled) : base(canceled) { }

        public void UpdateProgress(int percentComplete, string currentStatus)
        {
            // TODO: I have no idea what needs to go here.
        }

        public void Log(string message)
        {
            // TODO: I have no idea what needs to go here.
        }

        public static new IJobCallback Null { get { return null; } }
    }
}
