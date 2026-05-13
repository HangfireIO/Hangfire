using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Hangfire.Core.Tests.Stubs;
using Hangfire.Dashboard;
using Hangfire.Storage;
using Moq;
using Xunit;

namespace Hangfire.Core.Tests.Dashboard
{
    public class CommandDispatcherFacts
    {
        [Fact]
        public void Dispatch_Sets401StatusCode_WhenNotPermitted()
        {
            var options = new DashboardOptions
            {
                IsReadOnlyFunc = _ => true
            };
            var context = new DashboardContextStub(options);
            var dispatcher = new CommandDispatcher((DashboardContext _) => false);
            dispatcher.Dispatch(context);
            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task ServerDrainCommand_Returns422_WhenResourceCommandAuthorizationDenies()
        {
            var storage = new Mock<JobStorage>();
            storage.Setup(x => x.HasFeature(JobStorageFeatures.Connection.ServerResourceCommands)).Returns(true);
            var options = new DashboardOptions
            {
                ResourceCommandAuthorization = _ => false
            };
            var context = CreateServerCommandContext(storage.Object, options, "/servers/actions/drain/server-1");

            await context.Item1.Dispatch(context.Item2);

            Assert.Equal(422, context.Item2.Response.StatusCode);
            storage.Verify(x => x.GetConnection(), Times.Never);
        }

        [Fact]
        public async Task ServerDrainCommand_Returns422_WhenStorageDoesNotSupportResourceCommands()
        {
            var storage = new Mock<JobStorage>();
            var context = CreateServerCommandContext(storage.Object, new DashboardOptions(), "/servers/actions/drain/server-1");

            await context.Item1.Dispatch(context.Item2);

            Assert.Equal(422, context.Item2.Response.StatusCode);
            storage.Verify(x => x.GetConnection(), Times.Never);
        }

        [Fact]
        public async Task ServerDrainCommand_PersistsRemoteCommandAndAuditEvent()
        {
            var connection = new Mock<JobStorageConnection>();
            ServerResourceCommand command = null;
            ServerResourceEvent resourceEvent = null;
            connection.Setup(x => x.SaveServerResourceCommand("server/1", It.IsAny<ServerResourceCommand>()))
                .Callback<string, ServerResourceCommand>((_, x) => command = x);
            connection.Setup(x => x.AddServerResourceEvent(It.IsAny<ServerResourceEvent>()))
                .Callback<ServerResourceEvent>(x => resourceEvent = x);

            var storage = CreateCommandStorage(connection.Object);
            var context = CreateServerCommandContext(storage.Object, new DashboardOptions(), "/servers/actions/drain/server%2F1", "admin");

            await context.Item1.Dispatch(context.Item2);

            Assert.Equal((int)HttpStatusCode.NoContent, context.Item2.Response.StatusCode);
            Assert.Equal("drain", command.Command);
            Assert.Equal("server/1", command.ServerId);
            Assert.Equal("Dashboard command", command.Reason);
            Assert.Equal("admin", command.CreatedBy);
            Assert.Equal("command-created", resourceEvent.EventType);
            Assert.Equal(JobServerAllocationState.Draining, resourceEvent.AllocationState);
            Assert.Equal("Dashboard command", resourceEvent.Reason);
            Assert.Equal("admin", resourceEvent.Source);
        }

        [Fact]
        public async Task ServerResumeCommand_PersistsRemoteCommandAndAuditEvent()
        {
            var connection = new Mock<JobStorageConnection>();
            ServerResourceCommand command = null;
            ServerResourceEvent resourceEvent = null;
            connection.Setup(x => x.SaveServerResourceCommand("server-1", It.IsAny<ServerResourceCommand>()))
                .Callback<string, ServerResourceCommand>((_, x) => command = x);
            connection.Setup(x => x.AddServerResourceEvent(It.IsAny<ServerResourceEvent>()))
                .Callback<ServerResourceEvent>(x => resourceEvent = x);

            var storage = CreateCommandStorage(connection.Object);
            var context = CreateServerCommandContext(storage.Object, new DashboardOptions(), "/servers/actions/resume/server-1");

            await context.Item1.Dispatch(context.Item2);

            Assert.Equal((int)HttpStatusCode.NoContent, context.Item2.Response.StatusCode);
            Assert.Equal("resume", command.Command);
            Assert.Equal("server-1", command.ServerId);
            Assert.Null(command.Reason);
            Assert.Equal("command-created", resourceEvent.EventType);
            Assert.Equal(JobServerAllocationState.Available, resourceEvent.AllocationState);
            Assert.Equal("dashboard", resourceEvent.Source);
        }

        private static Mock<JobStorage> CreateCommandStorage(JobStorageConnection connection)
        {
            var storage = new Mock<JobStorage>();
            storage.Setup(x => x.HasFeature(JobStorageFeatures.Connection.ServerResourceCommands)).Returns(true);
            storage.Setup(x => x.GetConnection()).Returns(connection);
            return storage;
        }

        private static (IDashboardDispatcher, DashboardContextStubWithRequest) CreateServerCommandContext(
            JobStorage storage,
            DashboardOptions options,
            string path,
            string userName = null)
        {
            var route = DashboardRoutes.Routes.FindDispatcher(path);
            var context = new DashboardContextStubWithRequest(storage, options, userName);
            context.UriMatch = route.Item2;
            return (route.Item1, context);
        }

        private sealed class DashboardContextStubWithRequest : DashboardContext
        {
            private readonly string _userName;

            public DashboardContextStubWithRequest(JobStorage storage, DashboardOptions options, string userName)
                : base(storage, options)
            {
                _userName = userName;
                Request = new DashboardRequestStub();
                Response = new DashboardResponseStub();
            }

            public override string GetUserName()
            {
                return _userName;
            }
        }

        private sealed class DashboardRequestStub : DashboardRequest
        {
            public override string Method => "POST";
            public override string Path => "/";
            public override string PathBase => "/";
            public override string LocalIpAddress => "127.0.0.1";
            public override string RemoteIpAddress => "127.0.0.1";

            public override string GetQuery(string key)
            {
                return null;
            }

            public override Task<IList<string>> GetFormValuesAsync(string key)
            {
                return Task.FromResult<IList<string>>(new List<string>());
            }
        }
    }
}
