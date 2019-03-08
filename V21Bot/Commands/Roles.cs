using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using V21Bot.Helper;
using V21Bot.Redis;
using DSharpPlus;

namespace V21Bot.Commands
{
	public class Roles
	{
		const byte TOTAL_COLOURS = 16;
		const string ROLE_PREFIX = "Colour ";


		[Command("colour")]
		[Aliases("color")]
		[Hidden]
		[RequireOwner]
		[Description("Gives current user a colour")]
		public async Task Colour(CommandContext ctx, [Description("Hexadecimal representation of a RGB colour")] string colour)
		{
			//Convert the bytes
			byte[] bytes = null;
			try
			{
				colour = colour.TrimStart('#').ToUpperInvariant();
				bytes = StringToByteArray(colour);

			}
			catch (Exception e)
			{
				await ctx.RespondAsync(":question: Cannot parse the colour. Please provide the hexadecimal colour in the following format: ```#RRGGBB``` ```" + e.Message + "```");
				return;
			}

			//Make sure its valid
			if (bytes == null || bytes.Length == 0)
			{
				await ctx.RespondAsync(":question: Please supply a valid hexadecimal colour in the following format: ```#RRGGBB```");
				return;
			}
			

			//Update the colours
			byte R = 0, G = 0, B = 0;
			if (bytes.Length >= 1) R = G = B = RoundTo(bytes[0], TOTAL_COLOURS);
			if (bytes.Length >= 2) G = B = RoundTo(bytes[1], TOTAL_COLOURS);
			if (bytes.Length >= 3) B = RoundTo(bytes[2], TOTAL_COLOURS);

			DiscordColor discordColor = new DiscordColor(R, G, B);
			bool wasCreated = false;

			//Check if the role exists
			string hex = "#" + R.ToString("X2") + G.ToString("X2") + B.ToString("X2");
			string roleName = ROLE_PREFIX + hex;

			//Search every role in the server, looking for target role and removing any the user maybe in
			DiscordRole targetRole = null;
			foreach(var r in ctx.Guild.Roles)
			{
				if (r.Name.StartsWith(ROLE_PREFIX))
				{
					if (r.Name.Equals(roleName)) targetRole = r;
					if (ctx.Member.Roles.Contains(r)) await ctx.Member.RevokeRoleAsync(r, "Colour Role Command Changed");
				}
			}

			//Create the role if it doesnt exist
			if (targetRole == null)
			{
				//It does not, so create it
				targetRole = await ctx.Guild.CreateRoleAsync(roleName, color: discordColor, reason: "Colour Role Command Create");
				wasCreated = true;
			}

			//Now assign the role
			await ctx.Member.GrantRoleAsync(targetRole, "Colour Role Command Assign");

			DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
			{
				Color = discordColor,
				Title = "Role Granted",
				Footer = new DiscordEmbedBuilder.EmbedFooter() { IconUrl = "https://d.lu.je/avatar/" + ctx.User.Id, Text = ctx.User.Username },
				Description = wasCreated ? "Generated colour role `" + hex + "` then granted it!" : "Granted existing colour role `" + hex + "`!"
			};
			await ctx.RespondAsync("Assigned the role for you " + ctx.User.Mention, embed: builder);
		}

        [Command("rolemap")]
        [Aliases("rm")]
        [RequirePermissions(DSharpPlus.Permissions.ManageRoles)]
        [Description("Maps a reaction to a role on a message")]
        public async Task RoleMap(CommandContext ctx, DiscordMessage message, DiscordEmoji emoji, DiscordRole role, bool remove = false)
        {
            if (!V21.Instance.RedisAvailable)
            {
                await ctx.RespondException("Cannot create mapping because redis is unavailable.");
                return;
            }

            var guildEmoji = GetGuildEmoji(ctx.Client, emoji);
            if (guildEmoji == null)
            {
                await ctx.RespondException("Cannot create that mapping because I do not have access to that emoji. Is it from another guild?");
                return;
            }

            await ctx.TriggerTypingAsync();
            var redis = V21.Instance.Redis;
            string key = RedisNamespace.Create(ctx.Guild.Id, "rolemap", message.Id, emoji.Id);

            if (remove)
            {
                await redis.RemoveAsync(key);
                await message.DeleteOwnReactionAsync(emoji);
            }
            else
            {
                await redis.StoreStringAsync(key, role.Id.ToString());
                await message.CreateReactionAsync(emoji);
                await ctx.RespondAsync("Added mapping for emoji " + emoji + " to role " + role.Name);
            }
        }

        [Command("rolemapgame")]
        [Aliases("rmg")]
        [RequirePermissions(DSharpPlus.Permissions.ManageRoles)]
        [Description("Maps a game to a role")]
        public async Task RoleMapGame(CommandContext ctx, DiscordRole role, [RemainingText] string game)
        {
            if (!V21.Instance.RedisAvailable)
            {
                await ctx.RespondException("Cannot create mapping because redis is unavailable.");
                return;
            }

            var redis = V21.Instance.Redis;
            string key = RedisNamespace.Create(ctx.Guild.Id, "rolemap", "game", game.ToLowerInvariant().Replace(" ", ""));
            await redis.StoreStringAsync(key, role.Id.ToString());
            await ctx.RespondAsync("Added mapping for game " + game + " to role " + role.Name);
        }

        [Command("rolemapgameremove")]
        [Aliases("rmgr")]
        [RequirePermissions(DSharpPlus.Permissions.ManageRoles)]
        [Description("Removes a game from the role")]
        public async Task RoleMapGameRemove(CommandContext ctx, [RemainingText] string game)
        {
            if (!V21.Instance.RedisAvailable)
            {
                await ctx.RespondException("Cannot create mapping because redis is unavailable.");
                return;
            }

            var redis = V21.Instance.Redis;
            string key = RedisNamespace.Create(ctx.Guild.Id, "rolemap", "game", game.ToLowerInvariant().Replace(" ", ""));
            await redis.RemoveAsync(key);
            await ctx.RespondAsync("Removed mapping for " + game);
        }

        public static byte[] StringToByteArray(string hex)
		{
			if (hex.Length <= 1) return new byte[0];

			int NumberChars = hex.Length;
			byte[] bytes = new byte[NumberChars / 2];
			for (int i = 0; i < NumberChars; i += 2) bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}
		public static byte RoundTo(int value, int to)
		{
			double div = value / (double)to;
			double round = Math.Round(div);
			double res = round * to;
			return (byte) Math.Clamp(res, 0, 255);
		}

        private DiscordEmoji GetGuildEmoji(DiscordClient client, DiscordEmoji emoji)
        {
            if (!emoji.RequireColons) return emoji;
            return DiscordEmoji.FromGuildEmote(client, emoji.Id);
        }
    }

}
