using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.Storage
{
    public class SearchCriteria
    {
        public SearchMode SearchMode { get; set; }

        public string Text { get; set; }
    }

    public enum SearchMode
    {
        Contains,
        StartsWith,
        EndsWith
    }
}
