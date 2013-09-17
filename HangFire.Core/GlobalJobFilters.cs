using HangFire.Filters;

namespace HangFire
{
    public static class GlobalJobFilters
    {
        static GlobalJobFilters()
        {
            Filters = new GlobalJobFilterCollection();
        }

        public static GlobalJobFilterCollection Filters { get; private set; }
    }
}
