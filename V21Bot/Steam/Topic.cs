using System;
using System.Collections.Generic;
using System.Text;

namespace V21Bot.Steam
{
    class TopicPage : Page
    {
        /// <summary>
        /// List of topics in the page.
        /// </summary>
        public List<Topic> Topics { get; internal set; }
        public TopicPage()
        {
            Topics = new List<Topic>();
        }
    }

    class Topic
    {
        /// <summary>
        /// The ID of the topic
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The title of the topic
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Original author of the topic
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Link to the topic
        /// </summary>
        public string Link { get; set; }
        
        /// <summary>
        /// Short description of the topic
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The number of posts in the topic
        /// </summary>
        public int PostCount { get; set; }

        /// <summary>
        /// The time the post was originally made
        /// </summary>
        public Modification Posted { get; set; }

        /// <summary>
        /// The time the post was last edit
        /// </summary>
        public Modification Edited { get; set; }

        /// <summary>
        /// Total number of pages in the topic
        /// </summary>
        public int PageCount => (int)Math.Ceiling(PostCount / (double)Page.POST_PER_PAGE);

        /// <summary>
        /// Is the topic pinned?
        /// </summary>
        public bool IsPinned { get; set; }

        public struct Modification
        {
            /// <summary>
            /// Author of the modification
            /// </summary>
            public string Author { get; set; }

            /// <summary>
            /// The time the modification was made.
            /// </summary>
            public DateTime Date { get; set; }
        }
    }
}
