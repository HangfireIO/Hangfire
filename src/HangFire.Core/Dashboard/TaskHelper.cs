using System.Threading.Tasks;

namespace HangFire.Dashboard
{
    internal class TaskHelper
    {
        public static Task FromResult<T>(T result)
        {
            // TODO: replace with .NET 4.5's Task.FromResult
            var taskSource = new TaskCompletionSource<T>();
            taskSource.SetResult(result);
            return taskSource.Task;
        }
    }
}
