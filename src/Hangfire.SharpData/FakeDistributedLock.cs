using Hangfire.Storage;

namespace Hangfire.SharpData {
    public class FakeDistributedLock : IDistributedLock {

        public void Dispose() {
        }
    }
}