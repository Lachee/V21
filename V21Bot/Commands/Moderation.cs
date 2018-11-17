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
                EnforcedNickname enforcement = await V21.Instance.Redis.ObjectGetAsync<EnforcedNickname>(redisKey);
                if (enforcement == null)
                {
                    await ctx.RespondException("Cannot remove the nickname enforcement as it does not exist.");
                    return;
                }

                //Make sure we are allowed to 
                if (ctx.Member.Id != V21.Instance.Owner.Id && ctx.Member.Roles.OrderByDescending(r => r.Position).Select(r => r.Position).First() < enforcement.HighestRole)
                {
                    await ctx.RespondException($"Cannot remove the enforcement as <@{enforcement.Responsible}> ({enforcement.ResponsibleName}) set it and out ranks you.");
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

                await V21.Instance.Redis.ObjectSetAsync(redisKey, enforcement);                
                await member.ModifyAsync(nickname: nickname, reason: $"Nickname enforcement by {enforcement.ResponsibleName}");

                await ctx.RespondAsync("The nickname enforcement has been added.");
            }
        }
    }
}
