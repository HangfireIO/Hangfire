namespace HangFire.Web.Pages
{
    partial class SucceededJobs
    {
        private int _from;
        private int _count;

        public SucceededJobs()
        {
            if (!int.TryParse(Request.QueryString["from"], out _from))
            {
                _from = 0;
            }

            if (!int.TryParse(Request.QueryString["count"], out _count))
            {
                _count = 10;
            }
        }

        public int From
        {
            get { return _from; }
            set { _from = value; }
        }

        public int Count
        {
            get { return _count; }
            set { _count = value; }
        }
    }
}
