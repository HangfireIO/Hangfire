using System;
using System.Collections.Generic;
using System.Threading;
using Hangfire.Profiling;
using Hangfire.States;
using Hangfire.Storage;
using Moq;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Hangfire.Core.Tests.States
{
    public class StateChangeContextFacts
    {
        private const string JobId = "SomeJob";

        private readonly Mock<JobStorage> _storage;
        private readonly Mock<IStorageConnection> _connection;
        private readonly Mock<IState> _newState;
        private readonly string[] _expectedStates;
        private readonly CancellationToken _token;
        private readonly Mock<IProfiler> _profiler;
        private readonly Dictionary<string, object> _customData;

        public StateChangeContextFacts()
        {
            _storage = new Mock<JobStorage>();
            _connection = new Mock<IStorageConnection>();
            _newState = new Mock<IState>();
            _expectedStates = new[] { "Succeeded", "Failed" };
            _token = new CancellationToken(true);
            _profiler = new Mock<IProfiler>();
            _customData = new Dictionary<string, object>();
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenStorageIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateChangeContext(
                    null,
                    _connection.Object,
                    JobId,
                    _newState.Object));

            Assert.Equal("storage", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateChangeContext(
                    _storage.Object,
                    null,
                    JobId,
                    _newState.Object));

            Assert.Equal("connection", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenBackgroundJobIdIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateChangeContext(
                    _storage.Object,
                    _connection.Object,
                    null,
                    _newState.Object));

            Assert.Equal("backgroundJobId", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenNewStateIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateChangeContext(
                    _storage.Object,
                    _connection.Object,
                    JobId,
                    null));

            Assert.Equal("newState", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenProfilerIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new StateChangeContext(
                    _storage.Object,
                    _connection.Object,
                    JobId,
                    _newState.Object,
                    null,
                    false,
                    CancellationToken.None,
                    null));

            Assert.Equal("profiler", exception.ParamName);
        }

        [Fact]
        public void Ctor1_CorrectlySets_AllTheProperties()
        {
            var context = new StateChangeContext(
                _storage.Object,
                _connection.Object,
                JobId,
                _newState.Object);

            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Equal(JobId, context.BackgroundJobId);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Null(context.ExpectedStates);
            Assert.Equal(CancellationToken.None, context.CancellationToken);
            Assert.NotNull(context.Profiler);
            Assert.Null(context.CustomData);
        }

        [Fact]
        public void Ctor2_CorrectlySets_AllTheProperties()
        {
            var context = new StateChangeContext(
                _storage.Object,
                _connection.Object,
                JobId,
                _newState.Object,
                _expectedStates);

            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Equal(JobId, context.BackgroundJobId);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Equal(_expectedStates, context.ExpectedStates);
            Assert.Equal(CancellationToken.None, context.CancellationToken);
            Assert.NotNull(context.Profiler);
            Assert.Null(context.CustomData);
        }

        [Fact]
        public void Ctor3_CorrectlySets_AllTheProperties()
        {
            var context = new StateChangeContext(
                _storage.Object,
                _connection.Object,
                JobId,
                _newState.Object,
                _expectedStates,
                _token);

            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Equal(JobId, context.BackgroundJobId);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Equal(_expectedStates, context.ExpectedStates);
            Assert.Equal(_token, context.CancellationToken);
            Assert.NotNull(context.Profiler);
            Assert.Null(context.CustomData);
        }

        [Fact]
        public void InternalCtor_CorrectlySets_AllTheProperties()
        {
            var context = new StateChangeContext(
                _storage.Object,
                _connection.Object,
                JobId,
                _newState.Object,
                _expectedStates,
                false,
                _token,
                _profiler.Object,
                _customData);

            Assert.Same(_storage.Object, context.Storage);
            Assert.Same(_connection.Object, context.Connection);
            Assert.Equal(JobId, context.BackgroundJobId);
            Assert.Same(_newState.Object, context.NewState);
            Assert.Equal(_expectedStates, context.ExpectedStates);
            Assert.False(context.DisableFilters);
            Assert.Equal(_token, context.CancellationToken);
            Assert.Same(_profiler.Object, context.Profiler);
            Assert.Same(_customData, context.CustomData);
        }
    }
}
