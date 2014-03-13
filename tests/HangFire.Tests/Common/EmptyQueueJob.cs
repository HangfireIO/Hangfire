namespace HangFire.Tests
{
    [Queue("")]
    public class EmptyQueueJob : BackgroundJob
    {
        public override void Perform()
        {
        }
    }
}
