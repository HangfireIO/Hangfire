using System;
using Hangfire.Common;

namespace Hangfire.Core.Tests
{
    class BackgroundJobMock
    {
        private readonly Lazy<BackgroundJob> _object;

        public BackgroundJobMock()
        {
            Id = "JobId";
            Job = Job.FromExpression(() => SomeMethod());
            CreatedAt = DateTime.UtcNow;

            _object = new Lazy<BackgroundJob>(
                () => new BackgroundJob(Id, Job, CreatedAt));
        }

        public string Id { get; set; }
        public Job Job { get; set; }
        public DateTime CreatedAt { get; set; }

        public BackgroundJob Object => _object.Value;

        public static void SomeMethod() { }
    }
}
