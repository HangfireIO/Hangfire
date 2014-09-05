using System;
using Xunit;

namespace Hangfire.Core.Tests
{
    using Moq;

    public class JobActivatorFacts
    {
        [Fact, GlobalLock]
        public void SetCurrent_ThrowsAnException_WhenValueIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => JobActivator.Current = null);
        }

        [Fact, GlobalLock]
        public void GetCurrent_ReturnsPreviouslySetValue()
        {
            var activator = new JobActivator();
            JobActivator.Current = activator;

            Assert.Same(activator, JobActivator.Current);
        }

        [Fact]
        public void DefaultActivator_CanCreateInstanceOfClassWithDefaultConstructor()
        {
            var activator = new JobActivator();
            var context = new Mock<IJobActivationContext>();

            var instance = activator.ActivateJob(typeof(DefaultConstructor), context.Object);

            Assert.NotNull(instance);
        }

        [Fact]
        public void DefaultActivator_ThrowAnException_IfThereIsNoDefaultConstructor()
        {
            var activator = new JobActivator();
            var context = new Mock<IJobActivationContext>();

            Assert.Throws<MissingMethodException>(
                () => activator.ActivateJob(typeof(CustomConstructor), context.Object));
        }

        public class DefaultConstructor
        {
        }

        public class CustomConstructor
        {
// ReSharper disable once UnusedParameter.Local
            public CustomConstructor(string arg)
            {
            }
        }
    }
}
