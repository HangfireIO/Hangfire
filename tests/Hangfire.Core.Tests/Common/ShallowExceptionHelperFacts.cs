using System;
using Hangfire.Common;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class ShallowExceptionHelperFacts
    {
        [Fact]
        public void PreserveOriginalStackTrace_CanBeCalledTwice_WithoutThrowingAnyException()
        {
            try
            {
                throw new InvalidOperationException("Hello, world!");
            }
            catch (Exception ex)
            {
                ex.PreserveOriginalStackTrace();
                ex.PreserveOriginalStackTrace();
            }
        }
    }
}