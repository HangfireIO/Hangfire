namespace HangFire.Tests
{
    public class CustomConstructorJob : BackgroundJob
    {
        public CustomConstructorJob(string parameter)
        {
        }

        public override void Perform()
        {
        }
    }
}
