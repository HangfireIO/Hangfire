using System;
using System.Collections.Generic;
using HangFire.States;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class JobSteps : Steps
    {
        public const string DefaultJobId = "some-id";
        private static readonly Type DefaultJobType = typeof(TestJob);

        [Given(@"a job")]
        public void GivenAJob()
        {
            Redis.Client.SetRangeInHash(
                String.Format("hangfire:job:{0}", DefaultJobId),
                new Dictionary<string, string>
                    {
                        { "Type", DefaultJobType.AssemblyQualifiedName },
                        { "Args", JobHelper.ToJson(new Dictionary<string, string>()) },
                        { "State", EnqueuedState.Name },
                    });
        }
    }
}