using System;

namespace HangFire.Web
{
    internal class Pager
    {
        private readonly int _from;
        private readonly int _perPage;
        private readonly long _total;

        public Pager(int from, int perPage, long total)
        {
            if (from < 0) from = 0;
            if (perPage <= 0) perPage = 10;

            _from = from;
            _perPage = perPage;
            _total = total;
        }

        public string BaseLink { get; set; }

        public int CurrentPage
        {
            get { return _from / _perPage + 1; }
        }

        public int TotalPages
        {
            get { return (int)Math.Ceiling((double) _total / _perPage); }
        }

        public long Total
        {
            get { return _total; }
        }

        public string PageLink(int page)
        {
            if (page < 1 || page > TotalPages) return "#";

            return BaseLink + "?from=" + ((page - 1) * _perPage) + "&count=" + _perPage;
        }

        public string PreviousPageLink
        {
            get { return PageLink(CurrentPage - 1); }
        }

        public string NextPageLink
        {
            get { return PageLink(CurrentPage + 1); }
        }

        public bool HasPreviousPage
        {
            get { return CurrentPage > 1; }
        }

        public bool HasNextPage
        {
            get { return CurrentPage < TotalPages; }
        }

        public string PerPageLink(int perPage)
        {
            if (perPage <= 0) return "#";
            return BaseLink + "?from=0&count=" + perPage;
        }
    }
}
