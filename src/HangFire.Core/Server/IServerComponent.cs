using System.Threading;

namespace HangFire.Server
{
    public interface IServerComponent
    {
        void Execute(CancellationToken cancellationToken);
    }
}