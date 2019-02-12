using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using V21Bot.Helper;
using System.Threading.Tasks;
using V21Bot.Redis;
using V21Bot.Entities;
using System.Linq;

namespace V21Bot.Commands
{
    public class Moderation
    {
        [Command("nick")]
        [Description("Forces a user to have a set nickname. If a empty nickname is supplied the enforcement is removed.")]
        [RequirePermissions(DSharpPlus.Permissions.ManageNicknames)]
        public async Task ForceNickname(CommandContext ctx, [Description("The user to force the nickname")] DiscordMember member, [Description("The nickname to enforce. Make empty to remove enforcements."), RemainingText] string nickname)
        {
            //Make sure the member is valid
            if (member == null)
            {
                await ctx.RespondException("Cannot enforce a nickname for someone who isnt on this server.");
                return;
            }

            //Make sure its not ourself
            if (member == ctx.Member && ctx.Member.Id != V21.Instance.Owner.Id)
            {
                await ctx.RespondException("You cannot enforce / unenforce nicknames onto yourself.");
                return;
            }

            //Prepare the key for later use.
            string redisKey = EnforcedNickname.GetRedisNamespace(ctx.Guild, member);

            if (string.IsNullOrEmpty(nickname))
            {
                //We are removing the enforcement
                await ctx.TriggerTypingAsync();

                //Get the enforcement first
                EnforcedNickname enforcement = await V21.Instance.Redis.FetchObjectAsync<EnforcedNickname>(redisKey);
                if (enforcement == null)
                {
                    await ctx.RespondException("Cannot remove the nickname enforcement as it does not exist.");
                    return;
                }

                //Make sure we are allowed to 
                if (ctx.Member.Id != V21.Instance.Owner.Id && ctx.Member.Roles.OrderByDescending(r => r.Position).Select(r => r.Position).First() < enforcement.HighestRole)
                {
                    await ctx.RespondException($"Cannot remove the enforcement as {enforcement.ResponsibleName} ({enforcement.Responsible}) set it and out ranks you.");
                    return;
                }

                //We are allowed to so lets remove it
                if (await V21.Instance.Redis.RemoveAsync(redisKey))
                {
                    //yay we are all happy again!
                    await member.ModifyAsync(nickname: "", reason: $"Nickname enforcement removed by {ctx.User.Username}");
                    await ctx.RespondAsync("The nickname enforcement has been removed. :3");
                }
                else
                {
                    //Oh shit something happened
                    await ctx.RespondException($"The enforcement failed to be removed. Try running the command again? ;-;");
                }
            }
            else
            {
                //We are adding
                await ctx.TriggerTypingAsync();

                EnforcedNickname enforcement = new EnforcedNickname()
                {
                    Nickname = nickname,
                    HighestRole = ctx.Member.Roles.OrderByDescending(r => r.Position).Select(r => r.Position).First(),
                    Responsible = ctx.Member.Id,
                    ResponsibleName = ctx.Member.Username
                };

                await V21.Instance.Redis.StoreObjectAsync(redisKey, enforcement);
                await member.ModifyAsync(nickname: nickname, reason: $"Nickname enforcement by {enforcement.ResponsibleName}");

                await ctx.RespondAsync("The nickname enforcement has been added.");
            }
        }

        [Command("welcome")]
        [Description("Sets the welcome channel of a server")]
        [RequirePermissions(DSharpPlus.Permissions.ManageChannels)]
        public async Task SetWelcome(CommandContext ctx, [Description("The channel to send welcomes to. Set to null to remove welcomes.")] DiscordChannel channel, [Description("The message to send. Use {user} for a mention")]  string message)
        {
            await V21.Instance.SetWelcomeMessageChannel(ctx.Guild, channel, message);
            await ctx.RespondAsync("Welcome message has been " + (channel != null && message != null ? $"set to {channel.Mention} -> {message}." : "removed."));
        }

        [Command("send_welcome")]
        [Description("Sends a welcome")]
        [Hidden]
        [RequirePermissions(DSharpPlus.Permissions.ManageChannels)]
        public async Task SetWelcome(CommandContext ctx, [Description("Who to welcome.")] DiscordMember member)
        {
            await V21.Instance.SendWelcomeMessage(ctx.Guild, member);
        }

        [Command("ignore_commands")]
        [Description("Mutes the channel")]
        [RequirePermissions(DSharpPlus.Permissions.ManageChannels)]
        [Hidden]
        public async Task MuteChannel(CommandContext ctx, [Description("The channel to mute")] DiscordChannel channel, [Description("The mute state of the channel")] bool mute)
        {
            await ctx.TriggerTypingAsync();
            if (mute)
            {
                await V21.Instance.MuteChannel(channel);
                await ctx.RespondAsync(channel.Mention + " has been muted.");
            }
            else
            {
                await V21.Instance.UnmuteChannel(channel);
                await ctx.RespondAsync(channel.Mention + " has been unmuted.");
            }
        }



        [Command("mute")]
        [Aliases("bb", "blackbacon")]
        [RequirePermissions(DSharpPlus.Permissions.MuteMembers)]
        [Description("Mutes a member, storing all previous roles, removing them, then setting the BB role.")]
        public async Task AddMute(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
        {
            if (member.IsOwner)
            {
                await ctx.RespondAsync("Cannot mute my daddy OwO");
                return;
            }

            if (!await member.MuteAsync("Muted by " + ctx.Member + ": " + reason))
            {
                await ctx.RespondException("Failed to mute the member. Is the mute feature properly configured?");
                return;
            }


            await ctx.RespondAsync("The member " + member.Mention + " has been muted. " + (!string.IsNullOrWhiteSpace(reason) ? reason : ""));
        }

        [Command("unmute")]
        [Aliases("ubb", "unblackbacon")]
        [RequirePermissions(DSharpPlus.Permissions.MuteMembers)]
        [Description("Unmutes a member, storing all previous roles, removing them, then setting the BB role.")]
        public async Task RemoveMute(CommandContext ctx, DiscordMember member)
        {
            if (member.IsOwner)
            {
                await ctx.RespondAsync("Cannot mute my daddy OwO");
                return;
            }

            if (!await member.UnmuteAsync("Unmuted by " + ctx.Member))
            {
                await ctx.RespondException("Failed to unmute the member. Is the mute feature properly configured?");
                return;
            }

            await ctx.RespondAsync("The member " + member.Mention + " has been unmuted. ");
        }

        [Command("muterole")]
        [RequirePermissions(DSharpPlus.Permissions.MuteMembers)]
        [Description("Sets the mute role.")]
        public async Task RemoveMute(CommandContext ctx, DiscordRole role)
        {
            //Get the unmute ready
            await ctx.Guild.SetMuteRoleAsync(role);
            await ctx.RespondAsync("The role " + role + " is now the mute role.");
        }

        public async Task BulkMute(CommandContext ctx, DiscordMember member)
        {
            foreach(var c in ctx.Guild.Channels)
            {
                await c.AddOverwriteAsync(member, DSharpPlus.Permissions.None, DSharpPlus.Permissions.SendMessages, "Bulk Moderation");
            }
        }
    }
}
