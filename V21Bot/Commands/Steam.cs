using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using V21Bot.Helper;
using ImageMagick;
using V21Bot.Magicks;
using System.IO;
using System.Text.RegularExpressions;
using V21Bot.Steam;

namespace V21Bot.Commands
{
    [Group("steam")]
    public class Steam
    {
        [Command("compare")]
        public async Task CompareDiscussions(CommandContext ctx, ulong appid)
        {
            //Tell them that we are fetching
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":alarm_clock:"));

            //Fetch all the topics
            string redisHashKey = Redis.RedisNamespace.Create(ctx.Guild.Id, "steam", appid);
            var previousMapping = await V21.Instance.Redis.FetchHashMapAsync(redisHashKey);
            var newMapping = new Dictionary<string, string>();
            if (previousMapping.Count == 0)
            {
                await ctx.RespondAsync("Cannot compare because there is no stored entries.");
                return;
            }

            //Prepare the scrapper and get the disucssions
            DiscussionScrapper scrapper = new DiscussionScrapper(appid);
            var topicPage = await scrapper.GetTopicsAsync();
            
            //Iterate over every topic, posting the latest comment if it has changed
            foreach (var topic in topicPage.Topics)
            {
                //Add to the new mapping
                newMapping.Add(topic.Id, topic.PostCount.ToString());

                //Get the previous count.
                int previousCount = -1;
                if (previousMapping.TryGetValue(topic.Id, out var countString))
                    previousCount = int.Parse(countString);

                //If we are greater we should make a message about it.
                if (topic.PostCount > previousCount)
                {
                    if (topic.PostCount == 0)
                    {
                        //A new discussion started
                        await ctx.RespondAsync(embed: BuildEmbed(topicPage, topic));
                    }
                    else
                    {
                        //A new comment was created
                        var commentPage = await scrapper.GetCommentsAsync(topic);
                        await ctx.RespondAsync(embed: BuildEmbed(commentPage, commentPage.Comments.Last()));
                    }
                }
            }

            //Store the new hashmap
            await V21.Instance.Redis.RemoveAsync(redisHashKey);
            await V21.Instance.Redis.StoreHashMapAsync(redisHashKey, newMapping);
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
        }

        [Command("store")]
        public async Task StoreDiscussions(CommandContext ctx, ulong appid)
        {
            //Tell them that we are fetching
            await ctx.TriggerTypingAsync();

            //Prepare the scrapper and get the disucssions
            DiscussionScrapper scrapper = new DiscussionScrapper(appid);
            var topicPage = await scrapper.GetTopicsAsync();

            //Prepare the hash
            Dictionary<string, string> countHashMap = new Dictionary<string, string>();
            foreach(var topic in topicPage.Topics)
                countHashMap.Add(topic.Id, topic.PostCount.ToString());

            //Prepare the key
            string redisHashKey = Redis.RedisNamespace.Create(ctx.Guild.Id, "steam", appid);
            await V21.Instance.Redis.RemoveAsync(redisHashKey);
            await V21.Instance.Redis.StoreHashMapAsync(redisHashKey, countHashMap);

            //Done
            await ctx.RespondAsync($"Stored {countHashMap.Count} discussions");
        }

        [Command("latest")]
        public async Task GetDiscussion(CommandContext ctx, ulong appid)
        {
            //Tell them that we are fetching
            await ctx.TriggerTypingAsync();

            //Prepare the scrapper
            DiscussionScrapper scrapper = new DiscussionScrapper(appid);

            //Get the topics and then the comments on the first topic
            var topicPage = await scrapper.GetTopicsAsync();

            //Post the comments
            await ctx.RespondAsync(embed: BuildEmbed(topicPage, topicPage.Topics.First(t => !t.IsPinned)));
        }

        private DiscordEmbed BuildEmbed(TopicPage page, Topic topic)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder
                .WithTitle($"New Discussion: {topic.Title}")
                .WithUrl(topic.Link)
                .WithDescription(topic.Description)
                .WithTimestamp(topic.Posted.Date)
                .WithAuthor(
                    name: topic.Author,
                    url: $"https://steamcommunity.com/id/{topic.Author}",
                    icon_url: "https://s.lu.je/steam.png"
                )
                .WithFooter(page.AppTitle, "https://s.lu.je/steam.png")
                .WithColor(new DiscordColor(3102414));

            return builder.Build();
        }

        private DiscordEmbed BuildEmbed(CommentPage page, Comment comment)
        {
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder
                .WithTitle($"New Comment: {page.Title}")
                .WithUrl(comment.Link)
                .WithDescription(comment.Content)
                .WithTimestamp(comment.Posted)
                .WithAuthor(
                    name: comment.Author.Name,
                    url: comment.Author.Profile,
                    icon_url: comment.Author.AvatarURL
                )
                .WithFooter(page.AppTitle, "https://s.lu.je/steam.png")
                .WithColor(new DiscordColor(10716902));

            return builder.Build();
        }
	}
}

