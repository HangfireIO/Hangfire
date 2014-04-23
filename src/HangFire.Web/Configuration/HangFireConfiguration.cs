using System.Configuration;

namespace HangFire.Web.Configuration
{
    internal class HangFireConfiguration
    {
        public static bool EnableRemoteMonitorAccess
        {
            get
            {
                var configurationValue = ConfigurationManager.AppSettings["hangfire:EnableRemoteMonitorAccess"];

                bool enableAccess;
                if (!bool.TryParse(configurationValue, out enableAccess))
                {
                    // Disable remote access by default.
                    return false;
                }

                return enableAccess;
            }
        }
    }
}
