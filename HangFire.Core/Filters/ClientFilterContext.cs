namespace HangFire
{
    public class ClientFilterContext
    {
        internal ClientFilterContext(JobDescription jobDescription)
        {
            JobDescription = jobDescription;
        }

        public JobDescription JobDescription { get; private set; }
    }
}