using System;

namespace HangFire
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class QueueAttribute : Attribute
    {
        public QueueAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
