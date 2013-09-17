namespace HangFire.Storage
{
    public class ProcessingJobDto
    {
        public string ServerName { get; set; }
        public string Type { get; set; }
        public string Args { get; set; }
        public string StartedAt { get; set; }
    }
}