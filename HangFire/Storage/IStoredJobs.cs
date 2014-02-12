using System.Collections.Generic;
using HangFire.Server;

namespace HangFire.Storage
{
    public interface IStoredJobs
    {
        Dictionary<string, string> Get(string id);
        void SetParameter(string id, string name, string value);
        string GetParameter(string id, string name);

        void Complete(JobPayload payload);
    }
}