using System.Collections.Generic;

namespace HangFire.Storage.Monitoring
{
    public class JobList<TDto> : List<KeyValuePair<string, TDto>>
    {
        public JobList(IEnumerable<KeyValuePair<string, TDto>> source)
            : base(source)
        {
        }
    }
}