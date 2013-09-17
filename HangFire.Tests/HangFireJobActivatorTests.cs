using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HangFire.Tests
{
    [TestClass]
    public class HangFireJobActivatorTests
    {
        [TestMethod]
        public void Activate_ReturnsTheJobInstance_WhenTheJobHasDefaultConstructor()
        {
            var activator = new JobActivator();
            var job = activator.ActivateJob(typeof(DefaultConstructorJob));
            Assert.IsNotNull(job);
        }

        [TestMethod]
        [ExpectedException(typeof(MissingMethodException))]
        public void Activate_ThrowsActivationException_WhenTheJobHasNoDefaultConstructor()
        {
            var activator = new JobActivator();
            activator.ActivateJob(typeof(CustomConstructorJob));
        }

        public class DefaultConstructorJob : BackgroundJob
        {
            public override void Perform()
            {
            }
        }

        public class CustomConstructorJob : BackgroundJob
        {
            public CustomConstructorJob(string dependency)
            {
            }

            public override void Perform()
            {
            }
        }
    }
}