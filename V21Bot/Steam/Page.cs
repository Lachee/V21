using System;
using System.Collections.Generic;
using System.Text;

namespace V21Bot.Steam
{
    class Page
    {
        internal const int POST_PER_PAGE = 15;

        public string AppTitle { get; internal set; }
        public string URL { get; internal set; }
        public int CurrentPage { get; internal set; }
        public int TotalElements { get; internal set; }
        public int TotalPages => (int)Math.Ceiling(TotalElements / (double) POST_PER_PAGE);
    }
}
