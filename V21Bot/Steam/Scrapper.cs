using AngleSharp;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace V21Bot.Steam
{
    class DiscussionScrapper
    {
        private static readonly Regex DataRowRegex = new Regex("<span class=\"topic_hover_data\">(.*)<\\/span>, <span class=\"\">(.*)<\\/span>", RegexOptions.Compiled);
        public ulong AppId { get; }        

        public DiscussionScrapper(ulong appid)
        {
            AppId = appid;
        }

        /// <summary>
        /// Gets a list of comments on the topic. Comments are reverse order from topics, where the newest ones are last.
        /// </summary>
        /// <param name="topic">The topic to get the comments for</param>
        /// <param name="pageNumber">The page to get the comments from. Negative values will wrap around (ie: -1 will give the last page.)</param>
        /// <returns></returns>
        public async Task<CommentPage> GetCommentsAsync(Topic topic, int pageNumber = -1)
        {
            //Update the page number so negatives wrap around.
            if (pageNumber < 0)
                pageNumber = topic.PageCount + 1 + pageNumber;

            //Get the document
            string url = topic.Link + $"?ctp={pageNumber}";
            var document = await GetPageAsync(url);

            //Prepare the page to send back
            CommentPage page = new CommentPage()
            {
                URL = url,
                CurrentPage = pageNumber,
                TotalElements = GetElementCount(document),
                AppTitle = document.QuerySelector(".apphub_AppName").TextContent,
                Title = topic.Title
            };

            //Get the original post
            page.OriginalPost = new Comment();
            page.OriginalPost.Posted = topic.Posted.Date;
            page.OriginalPost.Id = topic.Id;
            page.OriginalPost.Link = topic.Link;
            page.OriginalPost.Content = document.QuerySelector(".forum_op .content").InnerHtml;

            var eOpAuthor = document.QuerySelector("a.forum_op_author");
            page.OriginalPost.Author = new Comment.AuthorDetail()
            {
                Name = eOpAuthor.TextContent.Trim(),
                Profile = eOpAuthor.GetAttribute("href"),
                AvatarURL = document.QuerySelector(".forum_op_avatar img").GetAttribute("src")
            };

            //Iterate over the comments
            var eCommentList = document.QuerySelectorAll("div.commentthread_comment");
            foreach (var eComment in eCommentList)
            {
                //Prepare the comment
                Comment comment = new Comment();

                var rawTimeStr = eComment.QuerySelector("span.commentthread_comment_timestamp").TextContent.Trim();
                comment.Posted = ParseDate(rawTimeStr);

                comment.Content = eComment.QuerySelector("div.commentthread_comment_text").InnerHtml;
                comment.Id = eComment.QuerySelector("div.forum_comment_permlink a").GetAttribute("href").TrimStart('#', ' ');
                comment.Link = $"{url}#{comment.Id}";

                comment.Author = new Comment.AuthorDetail()
                {
                    Name = eComment.QuerySelector("a.commentthread_author_link bdi").TextContent,
                    Profile = eComment.QuerySelector("a.commentthread_author_link").GetAttribute("href"),
                    AvatarURL = eComment.QuerySelector(".commentthread_comment_avatar img").GetAttribute("src")
                };

                //Add the comment to the page
                page.Comments.Add(comment);
            }

            return page;
        }

        /// <summary>
        /// Gets a list of topics on the front page of the apps discussion at the moment.
        /// </summary>
        /// <returns></returns>
        public async Task<TopicPage> GetTopicsAsync(int pageNumber = 0)
        {
            //Get the document
            string url = $"https://steamcommunity.com/app/{AppId}/discussions/?fp={pageNumber}";
            var document = await GetPageAsync(url);

            //Prepare the page to send back
            TopicPage page = new TopicPage()
            {
                URL = url,
                CurrentPage = pageNumber,
                TotalElements = GetElementCount(document),
                AppTitle = document.QuerySelector(".apphub_AppName").TextContent
            };
            
            //Iterate over the topics
            var eTopicList = document.QuerySelectorAll("div.forum_topic");
            foreach (var eTopic in eTopicList)
            {
                //Prepare the topic
                Topic topic = new Topic();

                //Prepare title and author
                topic.Title         = eTopic.QuerySelector(".forum_topic_name").TextContent.Trim();
                topic.Author        = eTopic.QuerySelector(".forum_topic_op").TextContent.Trim();
                topic.PostCount     = int.Parse(eTopic.QuerySelector(".forum_topic_reply_count").TextContent.Replace(",","").Trim());
                topic.Link          = eTopic.QuerySelector("a.forum_topic_overlay").GetAttribute("href");
                topic.Id            = topic.Link.Split('/').SkipLast(1).Last();

                if (topic.Title.StartsWith("PINNED:"))
                {
                    topic.Title = topic.Title.Remove(0, 7).Trim();
                    topic.IsPinned = true;
                }

                //Get the metadata and set the topic description from it
                var parser = new HtmlParser();
                var eMetadata = parser.ParseDocument(eTopic.GetAttribute("data-tooltip-forum"));
                topic.Description = eMetadata.QuerySelector("div.topic_hover_text").TextContent.Trim();

                //Using the metadata, we will also generate the edit dates.
                Topic.Modification? posted = null;
                Topic.Modification? edited = null;
                var dataRows = eMetadata.QuerySelectorAll("div.topic_hover_row").Select(m => m.InnerHtml.Trim());
                foreach (var row in dataRows)
                {
                    //Match the following:
                    //Posted by: <span class="topic_hover_data">supersmo</span>, <span class="">8 Nov, 2017 @ 7:28pm</span>			</div>
                    //Last post: <span class="topic_hover_data">supersmo</span>, <span class="">8 Nov, 2017 @ 7:28pm</span>			</div>
                    var matches = DataRowRegex.Match(row);

                    //Prepare the raw time and a value to hold the parse time
                    var rawTimeStr = matches.Groups[2].Value;
                    DateTime time = ParseDate(rawTimeStr);

                    //Create the new entry
                    var entry = new Topic.Modification()
                    {
                        Author = matches.Groups[1].Value,
                        Date = time
                    };
                        
                    //Update the correct field
                    if (row.StartsWith("Posted"))   posted = entry;
                    else                            edited = entry;
                }

                //Add the new dates
                topic.Posted = posted.GetValueOrDefault();
                topic.Edited = edited.GetValueOrDefault(topic.Posted);

                //Add to the array
                page.Topics.Add(topic);
            }

            //We finished parsing, return the page
            return page;
        }

        private DateTime ParseDate(string date)
        {
            string[] parts = date.Split('@');
            if (parts.Length == 1)
            {
                parts = date.Split(' ');
                if (parts.Length == 1)
                {
                    return DateTime.Parse(date);
                }
                else
                {
                    int quantity = int.Parse(parts[0]);
                    if (parts[1].Contains("hour")) return DateTime.Now - new TimeSpan(quantity, 0, 0);
                    if (parts[1].Contains("minute")) return DateTime.Now - new TimeSpan(0, quantity, 0);
                    return DateTime.Now - new TimeSpan(0, 0, quantity);
                }
            }
            
            var dateDT = DateTime.Parse(parts[0]);
            var timeDT = DateTimeOffset.Parse(parts[1]);
            return new DateTime(dateDT.Year, dateDT.Month, dateDT.Day, timeDT.Hour, timeDT.Minute, timeDT.Second);
        }

        private int GetElementCount(AngleSharp.Dom.IDocument document)
        {
            var eSummary = document.QuerySelector("div.forum_paging_summary");
            var span = eSummary.QuerySelectorAll("span").Where(c => c.Id.Contains("pagetotal")).First();
            var spanText = span.TextContent.Replace(",", "").Trim();
            return int.Parse(spanText);
        }

        private Task<AngleSharp.Dom.IDocument> GetPageAsync(string url)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            return context.OpenAsync(url);
        }
    }
}
