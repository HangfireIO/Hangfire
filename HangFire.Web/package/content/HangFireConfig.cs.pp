using HangFire.Web;

[assembly: WebActivatorEx.PreApplicationStartMethod(
    typeof($rootnamespace$.HangFireConfig), "Start")]

namespace $rootnamespace$
{
    public class HangFireConfig
    {
        public static void Start()
        {
            HangFireAspNetHost.Instance.Start();
        }
    }
}