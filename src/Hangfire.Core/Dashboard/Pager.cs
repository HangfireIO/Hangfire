// This file is part of Hangfire. Copyright © 2013-2014 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;

namespace Hangfire.Dashboard
{
    public class Pager
    {
        private const int PageItemsCount = 7;
        private const int DefaultRecordsPerPage = 10;

        private int _startPageIndex = 1;
        private int _endPageIndex = 1;

        public Pager(int from, int perPage, long total)
        {
            FromRecord = from >= 0 ? from : 0;
            RecordsPerPage = perPage > 0 ? perPage : DefaultRecordsPerPage;
            TotalRecordCount = total;
            CurrentPage = FromRecord / RecordsPerPage + 1;
            TotalPageCount = (int)Math.Ceiling((double)TotalRecordCount / RecordsPerPage);

            PagerItems = GenerateItems();
        }

        public string BasePageUrl { get; set; }

        public int FromRecord { get; }
        public int RecordsPerPage { get; }
        public int CurrentPage { get; }

        public int TotalPageCount { get; }
        public long TotalRecordCount { get; }

        internal ICollection<Item> PagerItems { get; }

        public virtual string PageUrl(int page)
        {
            if (page < 1 || page > TotalPageCount) return "#";

            return BasePageUrl + "?from=" + (page - 1) * RecordsPerPage + "&count=" + RecordsPerPage;
        }

        public string RecordsPerPageUrl(int perPage)
        {
            if (perPage <= 0) return "#";
            return BasePageUrl + "?from=0&count=" + perPage;
        }

        private ICollection<Item> GenerateItems()
        {
            // start page index
            _startPageIndex = CurrentPage - PageItemsCount / 2;
            if (_startPageIndex + PageItemsCount > TotalPageCount)
                _startPageIndex = TotalPageCount + 1 - PageItemsCount;
            if (_startPageIndex < 1)
                _startPageIndex = 1;

            // end page index
            _endPageIndex = _startPageIndex + PageItemsCount - 1;
            if (_endPageIndex > TotalPageCount)
                _endPageIndex = TotalPageCount;

            var pagerItems = new List<Item>();
            if (TotalPageCount == 0) return pagerItems;

            AddPrevious(pagerItems);

            // first page
            if (_startPageIndex > 1) 
                pagerItems.Add(new Item(1, false, ItemType.Page));

            // more page before numeric page buttons
            AddMoreBefore(pagerItems);

            // numeric page
            AddPageNumbers(pagerItems);

            // more page after numeric page buttons
            AddMoreAfter(pagerItems);

            // last page
            if (_endPageIndex < TotalPageCount)
                pagerItems.Add(new Item(TotalPageCount, false, ItemType.Page));

            // Next page
            AddNext(pagerItems);

            return pagerItems;
        }

        private void AddPrevious(ICollection<Item> results)
        {
            var item = new Item(CurrentPage - 1, CurrentPage == 1, ItemType.PrevPage);
            results.Add(item);
        }

        private void AddMoreBefore(ICollection<Item> results)
        {
            if (_startPageIndex > 2)
            {
                var index = _startPageIndex - 1;
                if (index < 1) index = 1;
                var item = new Item(index, false, ItemType.MorePage);
                results.Add(item);
            }
        }

        private void AddMoreAfter(ICollection<Item> results)
        {
            if (_endPageIndex < TotalPageCount - 1)
            {
                var index = _startPageIndex + PageItemsCount;
                if (index > TotalPageCount) { index = TotalPageCount; }
                var item = new Item(index, false, ItemType.MorePage);
                results.Add(item);
            }
        }

        private void AddPageNumbers(ICollection<Item> results)
        {
            for (var pageIndex = _startPageIndex; pageIndex <= _endPageIndex; pageIndex++)
            {
                var item = new Item(pageIndex, false, ItemType.Page);
                results.Add(item);
            }
        }

        private void AddNext(ICollection<Item> results)
        {
            var item = new Item(CurrentPage + 1, CurrentPage >= TotalPageCount, ItemType.NextPage);
            results.Add(item);
        }

        internal class Item
        {
            public Item(int pageIndex, bool disabled, ItemType type)
            {
                PageIndex = pageIndex;
                Disabled = disabled;
                Type = type;
            }

            public int PageIndex { get; }
            public bool Disabled { get; }
            public ItemType Type { get; }
        }

        internal enum ItemType
        {
            Page,
            PrevPage,
            NextPage,
            MorePage
        }
    }
}
