using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Xunit;

namespace HangFire.Core.Tests
{
    internal class StaticLockAttribute : BeforeAfterTestAttribute
    {
        private readonly ConcurrentDictionary<Type, object> _locks
            = new ConcurrentDictionary<Type, object>(); 
        private readonly object _globalLock = new object();

        public bool IsGlobal { get; set; }
        
        public override void Before(MethodInfo methodUnderTest)
        {
            var type = GetType(methodUnderTest);
            _locks.TryAdd(type, new object());

            Monitor.Enter(IsGlobal ? _globalLock : _locks[type]);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Monitor.Exit(IsGlobal ? _globalLock : _locks[GetType(methodUnderTest)]);
        }

        private static Type GetType(MethodInfo methodInfo)
        {
            return methodInfo.DeclaringType;
        }
    }
}
