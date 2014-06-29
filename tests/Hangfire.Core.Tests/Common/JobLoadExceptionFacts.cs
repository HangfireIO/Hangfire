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

        [Fact]
        public void Instance_CanBeSerializedAndDeserialized()
        {
            var exception = new JobLoadException("1", new Exception());
            JobLoadException deserializedException;

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, exception);

                stream.Position = 0;

                deserializedException = (JobLoadException) formatter.Deserialize(stream);
            }

            Assert.Equal("1", deserializedException.Message);
        }
    }
}
