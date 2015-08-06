using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.States
{
    public class ProcessingLogState : IState
    {
        public ProcessingLogState(string level, string message)
        {
            this.Level = level;
            this.Reason = message;
        }

        public string Name { get { return ProcessingState.StateName; } }
        public string Reason { get; private set; }
        public bool IsFinal { get { return false; } }
        public bool IgnoreJobLoadException { get { return false; } }

        public string Level { get; private set; }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "Level", this.Level }
            };
        }
    }
}
