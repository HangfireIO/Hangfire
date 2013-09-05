using System;

namespace HangFire
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class QueueNameAttribute : Attribute
    {
        public QueueNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
