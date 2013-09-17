namespace HangFire.Storage
{
    public class ScheduleDto
    {
        public string TimeStamp { get; set; }
        public string Type { get; set; }
        public string Queue { get; set; }
        public string Args { get; set; }
    }
}