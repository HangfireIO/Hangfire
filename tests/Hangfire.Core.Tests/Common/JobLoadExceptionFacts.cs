using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Common;
using Xunit;

namespace Hangfire.Core.Tests.Common
{
    public class JobLoadExceptionFacts
    {
        [Fact]
        public void Ctor_CreatesException_WithGivenMessageAnInnerException()
        {
            var innerException = new Exception();
            var exception = new JobLoadException("1", innerException);

            Assert.Equal("1", exception.Message);
            Assert.Same(innerException, exception.InnerException);
        }
    }
}
