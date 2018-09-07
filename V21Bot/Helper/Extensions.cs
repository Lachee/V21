using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace V21Bot.Helper
{
    public static class Extensions
    {
        public static string EmojiYes = ":white_check_mark:";
        public static string EmojiNo = ":x:";

        /// <summary>
        /// Creates a new Discord Message and adds a Yes / No element to it.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="content">The content to send</param>
        /// <param name="embed">The embed to send</param>
        /// <param name="timeout">How long before the interactions are no longer actionable</param>
        /// <param name="delete">Should the message be automatically deleted?</param>
        /// <returns></returns>
        public static async Task<DialogResponse> RespondDialogAsync(this CommandContext ctx, string content = null, DiscordEmbed embed = null, int timeout = 60, bool delete = false)
        {
            //Prepare the interaction emoji
            DiscordEmoji emojiOK = DiscordEmoji.FromName(ctx.Client, EmojiYes);
            DiscordEmoji emojiNO = DiscordEmoji.FromName(ctx.Client, EmojiNo);

            //Create the message
            var message = await ctx.RespondAsync(content, embed: embed);
            await message.CreateReactionAsync(emojiOK);
            await message.CreateReactionAsync(emojiNO);

            //Create the interactivity
            var interactivity = ctx.Client.GetInteractivityModule();
            var reaction = await interactivity.WaitForReactionAsync(emoji => emoji == emojiOK || emoji == emojiNO, ctx.User, TimeSpan.FromSeconds(timeout));

            //If we are auto delete, delete it
            if (delete)
            {
                await message.DeleteAsync("Interactivity ended");
            }
            else
            {
                await message.DeleteOwnReactionAsync(emojiOK);
                await message.DeleteOwnReactionAsync(emojiNO);
            }

            //If its null, then its ignored
            if (reaction == null)
                return DialogResponse.Ignore;

            return reaction.Emoji == emojiOK ? DialogResponse.Yes : DialogResponse.No;
        }   
    }

    public enum DialogResponse
    {
        Ignore = 0,
        No = 1,
        Yes = 2,
    }
}
