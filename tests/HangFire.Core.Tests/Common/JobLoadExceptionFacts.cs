using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using HangFire.Common;
using Xunit;

namespace HangFire.Core.Tests.Common
{
    public class JobLoadExceptionFacts
    {
        [Fact]
        public void DefaultCtor_CreatesNewInstance()
        {
            var exception = new JobLoadException();
            Assert.NotNull(exception);
        }

        [Fact]
        public void Ctor_CreatesException_WithAGivenMessage()
        {
            var exception = new JobLoadException("1");
            Assert.Equal("1", exception.Message);
        }

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
            var exception = new JobLoadException("1");
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
