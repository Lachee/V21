using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using V21Bot.Redis;

namespace V21Bot.Helper
{
    public static class Extensions
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static string EmojiYes = ":white_check_mark:";
        public static string EmojiNo = ":x:";

        public static DateTime CreationTime(this DiscordUser user)
        {
            ulong discord_epoch = user.Id >> 22;
            ulong unix_epoch = discord_epoch + 1420070400000L;
            return UnixEpoch.AddMilliseconds(unix_epoch);
        }

        /// <summary>
        /// Checks if the member is muted.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static async Task<bool> IsMutedAsync(this DiscordMember member)
        {
            var previousKey = RedisNamespace.Create(member.Guild.Id, "moderation", member.Id, "premute");
            return await V21.Instance.Redis.ExistsAsync(previousKey);
        }

        /// <summary>
        /// Sets the role to apply for muting
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        public static async Task SetMuteRoleAsync(this DiscordGuild guild, DiscordRole role)
        {
            var muteKey = RedisNamespace.Create(guild.Id, "moderation", "muterole");
            await V21.Instance.Redis.StoreStringAsync(muteKey, role.Id.ToString());
        }

        /// <summary>
        /// Gets the current mute role
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public static async Task<DiscordRole> GetMuteRoleAsync(this DiscordGuild guild)
        {
            string muteString;
            ulong muteId;
            DiscordRole muteRole;

            var muteKey = RedisNamespace.Create(guild.Id, "moderation", "muterole");
            muteString = await V21.Instance.Redis.FetchStringAsync(muteKey);

            if (muteString == null || !ulong.TryParse(muteString, out muteId) || (muteRole = guild.GetRole(muteId)) == null)
                return null;

            return muteRole;
        }

        /// <summary>
        /// Mutes a member
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="member"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public static async Task<bool> MuteAsync(this DiscordMember member, string reason, bool storeRoles = true)
        {
            if (member.IsOwner) return false;
            
            var muteKey = RedisNamespace.Create(member.Guild.Id, "moderation", "muterole");
            var previousKey = RedisNamespace.Create(member.Guild.Id, "moderation", member.Id, "premute");
            var reasonKey = RedisNamespace.Create(member.Guild.Id, "moderation", member.Id, "reason");

            //Get the mute role
            var muteRole = await member.Guild.GetMuteRoleAsync();
            if (muteRole == null) return false;

            //Fetch their current roles (excluding mute role) and store them in a hashset.
            if (storeRoles)
            {
                var userRoles = member.Roles
                    .Where(r => r.Id != muteRole.Id)
                    .Select(r => r.Id.ToString())
                    .ToHashSet();

                //Store the roles
                if (userRoles.Count > 0)
                    await V21.Instance.Redis.AddHashSetAsync(previousKey, userRoles);

                //Store the reason
                await V21.Instance.Redis.StoreStringAsync(reasonKey, reason);
            }

            //Replace their IDS
            await member.ReplaceRolesAsync(new DiscordRole[] { muteRole }, reason);
            return true;
        }

        /// <summary>
        /// Unmutes a member
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public static async Task<bool> UnmuteAsync(this DiscordMember member, string reason = "Unmuted")
        {
            if (member.IsOwner) return false;

            var previousKey = RedisNamespace.Create(member.Guild.Id, "moderation", member.Id, "premute");
            var reasonKey = RedisNamespace.Create(member.Guild.Id, "moderation", member.Id, "reason");

            //Get the mute role
            var muteRole = await member.Guild.GetMuteRoleAsync();
            if (muteRole == null) return false;

            //Fetch Previous Roles.
            var previousRoles = await V21.Instance.Redis.FetchHashSetAsync(previousKey);
            if (previousRoles != null && previousRoles.Count == 0)
            {

                //Prepare a list of actual roles to award
                var roles = member.Guild.Roles.Where(r => previousRoles.Contains(r.Id.ToString()));

                //Remove the old elmenents
                await V21.Instance.Redis.RemoveAsync(previousKey);

                //Replace their IDS
                await member.ReplaceRolesAsync(roles, reason);
            }
            else
            {
                //They literally had no roles, so just remove everything
                await member.ReplaceRolesAsync(new DiscordRole[0]);
            }

            //Remove the reason key
            await V21.Instance.Redis.RemoveAsync(reasonKey);
            return true;
        }


        /// <summary>
        /// Gets a guild member based of a discord user.
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<DiscordMember> GetMemberAsync(this DiscordGuild guild, DiscordUser user) => await guild.GetMemberAsync(user.Id);
        

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
