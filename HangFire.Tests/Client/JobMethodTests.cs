using System;
using System.Collections.Generic;
using System.Linq;
using HangFire.Common;
using HangFire.Common.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HangFire.Tests.Client
{
    [TestClass]
    public class JobMethodTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_ThrowsAnException_WhenTheTypeIsNull()
        {
// ReSharper disable ObjectCreationAsStatement
            new JobMethod(null, typeof (TestJob).GetMethod("Perform"));
// ReSharper restore ObjectCreationAsStatement
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_ThrowsAnException_WhenTheMethodIsNull()
        {
// ReSharper disable ObjectCreationAsStatement
            new JobMethod(typeof (TestJob), null);
// ReSharper restore ObjectCreationAsStatement
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Ctor_ThrowsAnException_WhenTheTypeDoesNotContainTheGivenMethod()
        {
// ReSharper disable ObjectCreationAsStatement
            new JobMethod(typeof (JobMethod), typeof (TestJob).GetMethod("Perform"));
// ReSharper restore ObjectCreationAsStatement
        }

        [TestMethod]
        public void Ctor_CorrectlySets_PropertyValues()
        {
            var type = typeof (TestJob);
            var methodInfo = type.GetMethod("Perform");
            var method = new JobMethod(type, methodInfo);

            Assert.AreEqual(type, method.Type);
            Assert.AreEqual(methodInfo, method.Method);
            Assert.IsFalse(method.OldFormat);
        }

        [TestMethod]
        public void Serialize_CorrectlySerializes_AllTheData()
        {
            var type = typeof(TestJob);
            var methodInfo = type.GetMethod("Perform");
            var method = new JobMethod(type, methodInfo);
            var serializedData = method.Serialize();

            Assert.AreEqual(type.AssemblyQualifiedName, serializedData["Type"]);
            Assert.AreEqual(methodInfo.Name, serializedData["Method"]);
            Assert.AreEqual(JobHelper.ToJson(new Type[0]), serializedData["ParameterTypes"]);
        }

        [TestMethod]
        public void Deserialize_CorrectlyDeserializes_AllTheData()
        {
            var type = typeof(TestJob);
            var methodInfo = type.GetMethod("Perform");
            var serializedData = new Dictionary<string, string>
            {
                { "Type", type.AssemblyQualifiedName },
                { "Method", methodInfo.Name },
                { "ParameterTypes", JobHelper.ToJson(new Type[0]) }
            };

            var method = JobMethod.Deserialize(serializedData);

            Assert.AreEqual(type, method.Type);
            Assert.AreEqual(methodInfo, method.Method);
            Assert.IsFalse(method.OldFormat);
        }

        [TestMethod]
        [ExpectedException(typeof (ArgumentNullException))]
        public void Deserialize_ThrowsAnException_WhenSerializedDataIsNull()
        {
            JobMethod.Deserialize(null);
        }

        [TestMethod]
        [ExpectedException(typeof(JobLoadException))]
        public void Deserialize_WrapsAnException_WithTheJobLoadException()
        {
            var serializedData = new Dictionary<string, string>();
            JobMethod.Deserialize(serializedData);
        }

        [TestMethod]
        [ExpectedException(typeof (JobLoadException))]
        public void Deserialize_ThrowsAnException_WhenTypeCanNotBeFound()
        {
            var serializedData = new Dictionary<string, string>
            {
                { "Type", "NonExistingType" },
                { "Method", "Perform" },
                { "ParameterTypes", "" }
            };

            JobMethod.Deserialize(serializedData);
        }

        [TestMethod]
        [ExpectedException(typeof(JobLoadException))]
        public void Deserialize_ThrowsAnException_WhenMethodCanNotBeFound()
        {
            var serializedData = new Dictionary<string, string>
            {
                { "Type", typeof (TestJob).AssemblyQualifiedName },
                { "Method", "NonExistingMethod" },
                { "ParameterTypes", JobHelper.ToJson(new Type[0]) }
            };

            JobMethod.Deserialize(serializedData);
        }

        [TestMethod]
        public void GetTypeFilterAttributes_ReturnsCorrectAttributes()
        {
            var method = GetCorrectMethod();
            var nonCachedAttributes = method.GetTypeFilterAttributes(false).ToArray();
            var cachedAttributes = method.GetTypeFilterAttributes(true).ToArray();

            Assert.AreEqual(1, nonCachedAttributes.Length);
            Assert.AreEqual(1, cachedAttributes.Length);

            Assert.IsInstanceOfType(nonCachedAttributes[0], typeof(TestTypeAttribute));
            Assert.IsInstanceOfType(cachedAttributes[1], typeof(TestTypeAttribute));
        }

        [TestMethod]
        public void GetMethodFilterAttributes_ReturnsCorrectAttributes()
        {
            var method = GetCorrectMethod();
            var nonCachedAttributes = method.GetMethodFilterAttributes(false).ToArray();
            var cachedAttributes = method.GetMethodFilterAttributes(true).ToArray();
            
            Assert.AreEqual(1, nonCachedAttributes.Length);
            Assert.AreEqual(1, cachedAttributes.Length);

            Assert.IsInstanceOfType(nonCachedAttributes[0], typeof(TestMethodAttribute));
            Assert.IsInstanceOfType(cachedAttributes[0], typeof(TestMethodAttribute));
        }

        private static JobMethod GetCorrectMethod()
        {
            var type = typeof(TestJob);
            var methodInfo = type.GetMethod("Perform");
            return new JobMethod(type, methodInfo);
        }

        #region Old Client API tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void OldCtor_ThrowsAnException_WhenTheTypeIsNull()
        {
// ReSharper disable ObjectCreationAsStatement
            new JobMethod(null);
// ReSharper restore ObjectCreationAsStatement
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void OldCtor_ThrowsAnException_WhenTheGivenType_IsNotASuccessor_OfTheBackgroundJobClass()
        {
            new JobMethod(typeof(JobMethod));
        }

        [TestMethod]
        public void OldCtor_SetsTheCorrectValues_ForProperties()
        {
            var method = new JobMethod(typeof (TestJob));
            Assert.AreEqual(typeof(TestJob), method.Type);
            Assert.IsNull(method.Method);
            Assert.IsTrue(method.OldFormat);
        }

        [TestMethod]
        public void Serialization_OfTheOldFormat_CorrectlySerializesTheType()
        {
            var method = new JobMethod(typeof (TestJob));
            var serializedData = method.Serialize();

            Assert.AreEqual(1, serializedData.Count);
            Assert.AreEqual(typeof(TestJob).AssemblyQualifiedName, serializedData["Type"]);
        }

        [TestMethod]
        public void Deserialization_FromTheOldFormat_CorrectlySerializesBothTypeAndMethod()
        {
            var serializedData = new Dictionary<string, string>
            {
                { "Type", typeof (TestJob).AssemblyQualifiedName }
            };

            var method = JobMethod.Deserialize(serializedData);
            Assert.AreEqual(typeof(TestJob), method.Type);
            Assert.AreEqual(typeof(TestJob).GetMethod("Perform"), method.Method);
            Assert.IsTrue(method.OldFormat);
        }

        [TestMethod]
        public void SerializedData_IsNotBeingChanged_DuringTheDeserialization()
        {
            var serializedData = new Dictionary<string, string>
            {
                { "Type", typeof (TestJob).AssemblyQualifiedName }
            };

            JobMethod.Deserialize(serializedData);
            
            Assert.AreEqual(1, serializedData.Count);
        }

        [TestMethod]
        public void GetMethodFilterAttributes_ReturnsEmptyCollection_WhenNoMethodIsSpecified()
        {
            var method = new JobMethod(typeof(TestJob));
            var attributes = method.GetMethodFilterAttributes(false).ToArray();

            Assert.AreEqual(0, attributes.Length);
        }

        public class TestTypeAttribute : JobFilterAttribute
        {
        }

        public class TestMethodAttribute : JobFilterAttribute
        {
        }

        [TestTypeAttribute]
        public class TestJob : BackgroundJob
        {
            [TestMethodAttribute]
            public override void Perform()
            {
            }
        }

        #endregion
    }
}
