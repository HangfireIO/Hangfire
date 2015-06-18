using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hangfire.States
{
    public class ProcessingSubState : IState
    {
        public ProcessingSubState()
        {
        }

        public string Name { get { return ProcessingState.StateName; } }
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
