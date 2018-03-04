namespace Hangfire.SqlServer
{
    internal class Constants
    {
        public static readonly string DefaultSchema = "HangFire";

        // Limits.
        public const int CounterKeyMaxLength = 100;
        public const int HashKeyMaxLength = 100;
        public const int HashFieldMaxLength = 100;
        public const int JobParameterNameMaxLength = 40;
        public const int ListKeyMaxLength = 100;
        public const int ServerIdMaxLength = 100;
        public const int SetKeyMaxLength = 100;
        public const int SetValueMaxLength = 256;
        public const int StateNameMaxLength = 20;
        public const int StateReasonMaxLength = 100;
    }
}
