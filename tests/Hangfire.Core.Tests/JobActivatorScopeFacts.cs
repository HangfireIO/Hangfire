using System;
using Moq;
using Xunit;
using Moq.Protected;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Core.Tests
{
    public class JobActivatorScopeFacts
    {
        [Fact]
        public void JobActivatorScope_Current_SetOnEnter_RestoredOnExit()
        {
            var activator = new JobActivator();

            using (var outer = activator.BeginScope(null))
            {
                Assert.Same(outer, JobActivatorScope.Current);

                using (var scope = activator.BeginScope(null))
                {
                    Assert.Same(scope, JobActivatorScope.Current);
                }

                Assert.Same(outer, JobActivatorScope.Current);
            }
        }

        [Fact]
        public void JobActivatorScope_DisposeCalledMultipleTimes_DisposedOnce()
        {
            int disposeCount = 0;
            var scope = Mock.Of<JobActivatorScope>();
            Mock.Get(scope).Protected().Setup("DisposeScope").Callback(() => disposeCount++);

            scope.Dispose();
            scope.Dispose();

            Assert.Equal(1, disposeCount);
        }

        [Fact]
        public void JobActivatorScope_Current_RestoredAfterManualDispose()
        {
            var activator = new JobActivator();

            using (var outer = activator.BeginScope(null))
            {
                Assert.Same(outer, JobActivatorScope.Current);

                using (var scope = activator.BeginScope(null))
                {
                    Assert.Same(scope, JobActivatorScope.Current);

                    scope.Dispose();

                    Assert.Same(outer, JobActivatorScope.Current);
                }

                Assert.Same(outer, JobActivatorScope.Current);
            }
        }

        [Fact]
        public void JobActivatorScope_ThrowsInvalidOperationException_OnWrongDisposeOrder()
        {
            var activator = new JobActivator();
            
            using (var outer = activator.BeginScope(null))
            {
                using (activator.BeginScope(null))
                {
                    Assert.Throws<InvalidOperationException>(() => outer.Dispose());
                }
            }
        }
        
        [Fact]
        public void JobActivatorScope_ThrowsObjectDisposedException_OnResolveAfterDispose()
        {
            var activator = new JobActivator();

            using (var scope = activator.BeginScope(null))
            {
                scope.Dispose();

                Assert.Throws<ObjectDisposedException>(() => scope.Resolve(typeof(DefaultConstructor)));
            }
        }

        [Fact]
        public async Task JobActivatorScope_Current_DoesntChangeBetweenAwaits()
        {
            var activator = new JobActivator();

            using (activator.BeginScope(null))
            {
                var prev = JobActivatorScope.Current;

                await Task.Yield();

                Assert.Same(prev, JobActivatorScope.Current);

                await Task.Yield();

                Assert.Same(prev, JobActivatorScope.Current);
            }
        }

        [Fact]
        public async Task JobActivatorScope_Current_SameForChildTask()
        {
            var activator = new JobActivator();

            using (activator.BeginScope(null))
            {
                var prev = JobActivatorScope.Current;

                await Task.Run(() => Assert.Same(prev, JobActivatorScope.Current)).ConfigureAwait(false);

                Assert.Same(prev, JobActivatorScope.Current);
            }
        }

        [Fact]
        public Task JobActivatorScope_Current_BranchesInParallelChildTasks()
        {
            var activator = new JobActivator();

            Task[] tasks;

            using (activator.BeginScope(null))
            {
                var prev = JobActivatorScope.Current;

                tasks = new[] {
                    Task.Run(() =>
                    {
                        Assert.Same(prev, JobActivatorScope.Current);
                        
                        using (var child = activator.BeginScope(null))
                        {
                            Assert.Same(child, JobActivatorScope.Current);
                            Assert.Same(prev, child.ParentScope);
                        }

                        Assert.Same(prev, JobActivatorScope.Current);

                        Assert.Throws<ObjectDisposedException>(() => prev.Resolve(typeof(DefaultConstructor)));
                    }),
                    Task.Run(() => 
                    {
                        Assert.Same(prev, JobActivatorScope.Current);

                        using (var child = activator.BeginScope(null))
                        {
                            Assert.Same(child, JobActivatorScope.Current);
                            Assert.Same(prev, child.ParentScope);
                        }

                        Assert.Same(prev, JobActivatorScope.Current);

                        Assert.Throws<ObjectDisposedException>(() => prev.Resolve(typeof(DefaultConstructor)));
                    })
                };
            }

            return Task.WhenAll(tasks);
        }

        private class DefaultConstructor
        {
        }
    }
}
