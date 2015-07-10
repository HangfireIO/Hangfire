using System;
using System.Threading;
using Hangfire.Annotations;

namespace Hangfire.Server
{
    internal class InfiniteLoopComponent : IServerComponent
    {
        public InfiniteLoopComponent([NotNull] IServerComponent innerComponent)
        {
            if (innerComponent == null) throw new ArgumentNullException("innerComponent");
            InnerComponent = innerComponent;
        }

        public IServerComponent InnerComponent { get; private set; }

        public void Execute(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InnerComponent.Execute(cancellationToken);
            }
        }

        public override string ToString()
        {
            return InnerComponent.ToString();
        }
    }
}