using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.States
{
    public class ProcessingLogState : IState
    {
        public static readonly string StateName = "Log";

        public ProcessingLogState(string level, string message)
        {
            this.Name = level;
            this.Reason = message;
        }

        public string Name { get; set; }
        public string Reason { get; set; }
        public bool IsFinal { get { return false; } }
        public bool IgnoreJobLoadException { get { return false; } }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
            };
        }
    }
}
