using HangFire.Server;

namespace HangFire.Storage
{
    public interface IPersistentJob
    {
        StateAndInvocationData GetStateAndInvocationData(string id);
        void SetParameter(string id, string name, string value);
        string GetParameter(string id, string name);

        void Complete(JobPayload payload);
    }
}