using System;
using Hangfire.Common;
using Hangfire.Server;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Hangfire.Core.Tests
{
    public class JobParameterInjectionFilterFacts
    {
        private readonly PerformContextMock _context;

        public JobParameterInjectionFilterFacts()
        {
            _context = new PerformContextMock();
        }

        [Fact]
        public void OnPerforming_ThrowsArgumentNullException_WhenContextIsNull()
        {
            var filter = CreateFilter();

            var exception = Assert.Throws<ArgumentNullException>(() => filter.OnPerforming(null));

            Assert.Equal("context", exception.ParamName);
        }

        [Fact]
        public void OnPerforming_HandlesParameterlessMethods_WithoutDoingAnything()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Parameterless());
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            _context.Connection.Verify(x => x.GetJobParameter(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void OnPerforming_DoesNotModifyNonDecoratedParameters()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => NonDecorated("hello", 1));
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            _context.Connection.Verify(x => x.GetJobParameter(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.Equal("hello", _context.BackgroundJob.Job.Args[0]);
            Assert.Equal(1, _context.BackgroundJob.Job.Args[1]);
        }

        [Fact]
        public void OnPerforming_ModifiesParameters_DecoratedWithASpecialAttribute_ThatHaveNullValue()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Decorated(null));
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "Result123")).Returns("\"Hello!\"");
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            Assert.Equal("Hello!", _context.BackgroundJob.Job.Args[0]);
        }
        
        [Fact]
        public void OnPerforming_RewritesValueTypeParameters_DecoratedWithASpecialAttribute_ThatHaveDefaultValue()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Decorated(default(int)));
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "Result123")).Returns("12345");
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            Assert.Equal(12345, _context.BackgroundJob.Job.Args[0]);
        }

        [Fact]
        public void OnPerforming_RewritesParameters_ThatDoNotHaveNullValue()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Decorated("NonNull"));
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "Result123")).Returns("\"Hello!\"");
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            Assert.Equal("Hello!", _context.BackgroundJob.Job.Args[0]);
        }

        [Fact]
        public void OnPerforming_RewritesValueTypeParameters_DecoratedWithASpecialAttribute_ThatHaveNonDefaultValue()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Decorated(123456));
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "Result123")).Returns("54321");
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            Assert.Equal(54321, _context.BackgroundJob.Job.Args[0]);
        }

        [Fact]
        public void OnPerforming_DoesNotRewriteDefaultValueWithNull_WhenJobParameterIsNull()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Decorated(0));
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "Result123")).Returns<string>(null);
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            Assert.Equal(0, _context.BackgroundJob.Job.Args[0]);
        }

        [Fact]
        public void OnPerforming_DoesNotRewriteNonDefaultValueWithNull_WhenJobParameterIsNull()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Decorated(123456));
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "Result123")).Returns<string>(null);
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            Assert.Equal(123456, _context.BackgroundJob.Job.Args[0]);
        }

        [Fact]
        public void OnPerforming_ThrowsDeserializationException_WhenParameterCanNotBeDeserialized()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Decorated(null));
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "Result123")).Returns("adk;hg");
            var filter = CreateFilter();

            Assert.ThrowsAny<JsonException>(() => filter.OnPerforming(_context.GetPerformingContext()));
        }

        [Fact]
        public void OnPerforming_ModifiesParameters_BasedOnFromResultAttribute()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => DecoratedWithFromResult(null));
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "AntecedentResult")).Returns("\"Result\"");
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            Assert.Equal("Result", _context.BackgroundJob.Job.Args[0]);
        }

        [Fact]
        public void OnPerforming_CanModifyMultipleParameters()
        {
            _context.BackgroundJob.Job = Job.FromExpression(() => Decorated(null, null));
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "Param1")).Returns("\"Value1\"");
            _context.Connection.Setup(x => x.GetJobParameter(_context.BackgroundJob.Id, "Param2")).Returns("2");
            var filter = CreateFilter();

            filter.OnPerforming(_context.GetPerformingContext());

            Assert.Equal("Value1", _context.BackgroundJob.Job.Args[0]);
            Assert.Equal(2, _context.BackgroundJob.Job.Args[1]);
        }

        [Fact]
        public void OnPerformed_DoesNotThrow_AnyException()
        {
            var filter = CreateFilter();
            filter.OnPerformed(new PerformedContext(_context.Object, null, false, null));
        }

        private JobParameterInjectionFilter CreateFilter()
        {
            return new JobParameterInjectionFilter();
        }

        public static void Parameterless()
        {
        }

        public static void NonDecorated(string arg1, int arg2)
        {
        }

        public static void Decorated([FromParameter("Result123")] string value)
        {
        }

        public static void Decorated([FromParameter("Result123")] int value)
        {
        }

        public static void DecoratedWithFromResult([FromResult] string value)
        {
        }

        public static void Decorated([FromParameter("Param1")] string value1, [FromParameter("Param2")] int? value2)
        {
        }
    }
}
