using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;

namespace HangFire.Tests
{
    [Binding]
    public class JobActivatorSteps
    {
        private BackgroundJob _jobInstance;
        private Exception _exception;

        [When(@"I call the `Activate` method with the '(\w+)' type argument")]
        public void WhenICallTheActivateMethodWithTheTypeArgument(string type)
        {
            try
            {
                Type jobType = null;

                if (type == "TestJob") jobType = typeof (TestJob);
                else if (type == "CustomConstructorJob") jobType = typeof (CustomConstructorJob);

                var activator = new JobActivator();
                _jobInstance = activator.ActivateJob(jobType);
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [Then(@"Activator should return an instance of the '(\w+)' type")]
        public void ThenActivatorShouldReturnAnInstanceOfTheType(string type)
        {
            Assert.AreEqual(type, _jobInstance.GetType().Name);
        }

        [Then(@"Activator throws a '(.+)'")]
        public void ThenActivatorThrowsAnException(string exceptionType)
        {
            Assert.IsNotNull(_exception);
            Assert.IsInstanceOfType(_exception, Type.GetType(exceptionType, true));
        }
    }
}
