using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HangFire.Common;

namespace HangFire.States
{
    public class DeletedState : IState
    {
        public static readonly string StateName = "Deleted";

        public DeletedState()
        {
            DeletedAt = DateTime.UtcNow;
        }

        public string Name
        {
            get { return StateName; }
        }

        public string Reason { get; set; }

        public bool IsFinal
        {
            get { return true; }
        }

        public DateTime DeletedAt { get; private set; }

        public Dictionary<string, string> SerializeData()
        {
            return new Dictionary<string, string>
            {
                { "DeletedAt", JobHelper.ToStringTimestamp(DeletedAt) }
            };
        }
    }
}
