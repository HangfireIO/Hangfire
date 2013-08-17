using HangFire.Web;

[assembly: WebActivatorEx.PreApplicationStartMethod(
    typeof(HangFire.MvcSample.App_Start.HangFireConfig), "Start")]

namespace HangFire.MvcSample.App_Start
{
    public class HangFireConfig
    {
        public static void Start()
        {
            //HangFireAspNetHost.Instance.Start();
        }
    }
}