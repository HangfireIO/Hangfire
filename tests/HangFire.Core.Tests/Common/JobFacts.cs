using System;
using HangFire.Common;
using Xunit;

namespace HangFire.Core.Tests.Common
{
    public class JobFacts
    {
        private readonly MethodData _methodData;
        private readonly string[] _arguments;

        public JobFacts()
        {
            _methodData = MethodData.FromExpression(() => StaticMethod());
            _arguments = new[] { "hello", "world" };
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenMethodDataIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Job(null, new string[0]));
        }

        [Fact]
        public void Ctor_ShouldThrowAnException_WhenArgumentsArrayIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => new Job(_methodData, null));
        }

        [Fact]
        public void Ctor_ShouldInitializeAllProperties()
        {
            var job = new Job(_methodData, _arguments);

            Assert.Same(_methodData, job.MethodData);
            Assert.Same(_arguments, job.Arguments);
        }

        [Fact]
        public void FromStaticExpression_ShouldThrowException_WhenNullExpressionProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Job.FromExpression(null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromStaticExpression_ShouldReturnTheJob()
        {
            var job = Job.FromExpression(() => Console.WriteLine());

            Assert.NotNull(job);
        }

        [Fact]
        public void FromInstanceExpression_ShouldThrowException_WhenNullExpressionIsProvided()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => Job.FromExpression<JobFacts>(null));

            Assert.Equal("methodCall", exception.ParamName);
        }

        [Fact]
        public void FromInstanceExpression_ShouldReturnCorrectResult()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            var method = Job.FromExpression<JobFacts>(x => x.InstanceMethod());

            Assert.NotNull(method);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodContainsReferenceParameter()
        {
            string test = null;
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => MethodWithReferenceParameter(ref test)));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodContainsOutputParameter()
        {
            string test;
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => MethodWithOutputParameter(out test)));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNewExpressionIsGiven()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => new JobFacts()));
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenMethodIsNotPublic()
        {
            Assert.Throws<NotSupportedException>(
                () => Job.FromExpression(() => PrivateMethod()));
        }

        private static void PrivateMethod()
        {
        }

        public void InstanceMethod()
        {
        }

        public static void StaticMethod()
        {
        }

        public static void MethodWithReferenceParameter(ref string a)
        {
        }

        public static void MethodWithOutputParameter(out string a)
        {
            a = "hello";
        }
    }
}
