using System.Collections.Generic;
using Hangfire.Server;

namespace Hangfire
{
    public interface IServerComponentFactory
    {
        IEnumerable<IServerComponent> Create(JobStorage storage, string serverId, BackgroundJobServerOptions options);
    }
}