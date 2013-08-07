[assembly: WebActivatorEx.PreApplicationStartMethod(
    typeof(HangFire.Sample.App_Start.HangFireConfig), "PreStart")]

namespace HangFire.Sample.App_Start
{
    public static class HangFireConfig
    {
        public static void PreStart()
        {
            
        }
    }
}