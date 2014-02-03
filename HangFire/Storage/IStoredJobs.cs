using System.Collections.Generic;

namespace HangFire.Storage
{
    public interface IStoredJobs
    {
        Dictionary<string, string> Get(string id);
        void SetParameter(string id, string name, string value);
        string GetParameter(string id, string name);
    }
}