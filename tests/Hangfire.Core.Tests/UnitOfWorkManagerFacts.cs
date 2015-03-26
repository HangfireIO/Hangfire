using System;
using Hangfire.UnitOfWork;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class UnitOfWorkManagerFacts
    {
        [Fact, GlobalLock]
        public void SetCurrent_ThrowsAnException_WhenValueIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => UnitOfWorkManager.Current = null);
        }

        [Fact, GlobalLock]
        public void GetCurrent_ReturnsPreviouslySetValue()
        {
            var unitOfWorkManager = new UnitOfWorkManager();
            UnitOfWorkManager.Current = unitOfWorkManager;

            Assert.Same(unitOfWorkManager, UnitOfWorkManager.Current);
        }
    }
}