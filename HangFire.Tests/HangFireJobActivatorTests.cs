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
            var activator = new HangFireJobActivator();
            var job = activator.ActivateJob(typeof(DefaultConstructorJob));
            Assert.IsNotNull(job);
        }

        [TestMethod]
        [ExpectedException(typeof(MissingMethodException))]
        public void Activate_ThrowsActivationException_WhenTheJobHasNoDefaultConstructor()
        {
            var activator = new HangFireJobActivator();
            activator.ActivateJob(typeof(CustomConstructorJob));
        }

        public class DefaultConstructorJob : HangFireJob
        {
            public override void Perform()
            {
            }
        }

        public class CustomConstructorJob : HangFireJob
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