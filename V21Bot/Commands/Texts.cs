﻿using DSharpPlus.CommandsNext;
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
using System.Data;

namespace V21Bot.Commands
{
    public class Texts
    {
        /// <summary>
        /// Mapping of all the characters from ASCII to AESTHETIC with extra THICCC
        /// </summary>
        private readonly Dictionary<char, char> AestheticMap = new Dictionary<char, char>()
        {
            { ' ', '　' },

            { 'a', 'ａ' }, { 'g', 'ｇ' }, { 'm', 'ｍ' }, { 's', 'ｓ' },
            { 'b', 'ｂ' }, { 'h', 'ｈ' }, { 'n', 'ｎ' }, { 't', 'ｔ' },
            { 'c', 'ｃ' }, { 'i', 'ｉ' }, { 'o', 'ｏ' }, { 'u', 'ｕ' },
            { 'd', 'ｄ' }, { 'j', 'ｊ' }, { 'p', 'ｐ' }, { 'v', 'ｖ' },
            { 'e', 'ｅ' }, { 'k', 'ｋ' }, { 'q', 'ｑ' }, { 'w', 'ｗ' },
            { 'f', 'ｆ' }, { 'l', 'ｌ' }, { 'r', 'ｒ' }, { 'x', 'ｘ' },
            { 'y', 'ｙ' }, { 'z', 'ｚ' },

            { 'A', 'Ａ' }, { 'G', 'Ｇ' }, { 'M', 'Ｍ' }, { 'S', 'Ｓ' },
            { 'B', 'Ｂ' }, { 'H', 'Ｈ' }, { 'N', 'Ｎ' }, { 'T', 'Ｔ' },
            { 'C', 'Ｃ' }, { 'I', 'Ｉ' }, { 'O', 'Ｏ' }, { 'U', 'Ｕ' },
            { 'D', 'Ｄ' }, { 'J', 'Ｊ' }, { 'P', 'Ｐ' }, { 'V', 'Ｖ' },
            { 'E', 'Ｅ' }, { 'K', 'Ｋ' }, { 'Q', 'Ｑ' }, { 'W', 'Ｗ' },
            { 'F', 'Ｆ' }, { 'L', 'Ｌ' }, { 'R', 'Ｒ' }, { 'X', 'Ｘ' },
            { 'Y', 'Ｙ' }, { 'Z', 'Ｚ' },

            { '0', '０' }, { '!', '！' }, { '5', '５' }, { '^', '＾' },
            { '1', '１' }, { '@', '＠' }, { '6', '６' }, { '&', '＆' },
            { '2', '２' }, { '#', '＃' }, { '7', '７' }, { '*', '＊' },
            { '3', '３' }, { '$', '＄' }, { '8', '８' }, { '(', '（' },
            { '4', '４' }, { '%', '％' }, { '9', '９' }, { ')', '）' },

            { '-', '－' }, { '_', '＿' }, { '<', '＜' }, { '>', '＞' },
            { '+', '＋' }, { '=', '＝' }, { '\\', '＼' }, { '|', '｜' },
            { '[', '［' }, { ']', '］' }, { '/', '／' }, { '?', '？' },
            { '{', '｛' }, { '}', '｝' }, { '"', '＂' }, { '\'', '＇' },
            { '.', '．' }, { ',', '，' }, { ':', '：' }, { ';', '；' },
        };
        private readonly Regex DiceRegex = new Regex(@"(?'count'\d{1,2})[dD](?'sides'\d{1,3})", RegexOptions.Compiled);

        [Command("vote-react")]
        public async Task ReactVote(CommandContext ctx, DiscordMessage message, int amount)
        {
            string[] emojis = new string[]
               {
                ":regional_indicator_a:",
                ":regional_indicator_b:",
                ":regional_indicator_c:",
                ":regional_indicator_d:",
                ":regional_indicator_e:",
                ":regional_indicator_f:",
                ":regional_indicator_g:",
                ":regional_indicator_h:",
                ":regional_indicator_i:",
                ":regional_indicator_j:",
                ":regional_indicator_k:",
                ":regional_indicator_l:",
                ":regional_indicator_m:",
                ":regional_indicator_n:",
                ":regional_indicator_o:",
                ":regional_indicator_p:",
                ":regional_indicator_q:",
                ":regional_indicator_r:",
                ":regional_indicator_s:",
                ":regional_indicator_t:",
                ":regional_indicator_u:",
                ":regional_indicator_v:",
                ":regional_indicator_w:",
                ":regional_indicator_x:",
                ":regional_indicator_y:",
                ":regional_indicator_z:"
               };

            if (amount > emojis.Length)
            {
                await ctx.RespondException("Max vote length is " + emojis.Length);
                return;
            }

            await ctx.RespondAsync("Working...");
            for (int i = 0; i < amount; i++)
            {
                DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, emojis[i]);
                await message.CreateReactionAsync(emoji);
            }
            await ctx.RespondAsync("Done!");
        }

        [Command("vote")]
        public async Task Vote(CommandContext ctx, DiscordChannel channel, int amount, [RemainingText] string content)
        {            
            if (channel == null)
            {
                await ctx.RespondException("Channel cannot be null");
                return;
            }

            //Post the message
            var message = await channel.SendMessageAsync(content);
            await ReactVote(ctx, message, amount);
        }

        [Command("rate")]
        [Description("Rates a user")]
        public async Task Rate(CommandContext ctx, DiscordUser user)
        {
            //We rate 2 aspects, username and avatar. So generate 2 randoms
            int rateAvatar = 0;
            int rateUid = 0;

            //Rate the avatar
            if (user.AvatarHash != null)
            {
                Random randAvatar = new Random(user.AvatarHash.GetHashCode());
                rateAvatar = randAvatar.Next(0, 10);
            }

            //Rate the User ID
            Random randUser = new Random(user.Id.GetHashCode());
            rateUid = randUser.Next(0, 10);

            //I am the bestest there is
            if (user.Id == 130973321683533824L)
            {
                rateAvatar = 11;
                rateUid = 11;
            }

            //Now tell them:
            int total = (rateAvatar + rateUid) / 2;
            string sas = "";
            if (total >= 10) sas = "Ooooooh Mmmmyyyyy.";
            if (total > 8) sas = "Hello mister ;)"; 
            if (total < 5) sas = "I am sorry, I dont think this will work out between us.";
            if (total < 2) sas = "Maybe a pats could make you feel better?";
            if (total <= 0) sas = "OOF.";

            if (rateAvatar < 3 && rateAvatar > 0) sas += " Your picture just doesnt do it for me.";
            if (rateAvatar <= 0) sas += " Your picture is just beyond horrible. I am so sorry.";
            if (rateAvatar >= 10) sas += " Your picture is just so handsome!";

            await ctx.RespondAsync("I give " + user.Mention + " a **" + total + "**. " + sas);
        }

		[Command("coffee")]
		[Description("Gives you a fun coffee fact")]
		public async Task Coffee(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			var coffee = DiscordEmoji.FromName(ctx.Client, ":coffee:");
			
			var lines = await File.ReadAllLinesAsync(Path.Combine(V21.Instance.Config.Resources, "facts.coffee.txt"));
			if (lines.Length < 2)
			{
				await ctx.RespondAsync("I am sorry, but there are not enough facts for coffee yet!");
				return;
			}

			//RND
			Random rnd = new Random();
			int factno = rnd.Next(1, lines.Length);

			//The reference and the factoid
			string reference = lines[0];
			string factoid = lines[factno];

			ResponseBuilder builder = new ResponseBuilder(ctx);
			builder.WithDescription("```\n{0}\n``` **Reference:** [{1}]({1})", factoid, reference)
				.WithAuthor("Coffee Factoid #" + factno, "", "https://emojipedia-us.s3.dualstack.us-west-1.amazonaws.com/thumbs/120/twitter/147/hot-beverage_2615.png")
				.WithColor(new DiscordColor("6b5033"));

			await ctx.RespondAsync(embed: builder);
		}	


		[Command("aesthetic")]
		[Aliases("fw", "ae", "vaporwave", "vw")]
		[Description("Echos your message back but with aesthetic")]
		public async Task Aesthetic(CommandContext ctx, [Description("The text to make aesthetic")][RemainingText] params string[] messages)
		{
			if (messages.Length == 0) return;
			await ctx.TriggerTypingAsync();

			StringBuilder vape = new StringBuilder();
			for(int i = 0; i < messages.Length; i++)
			{
				if (i > 0) vape.Append(AestheticMap[' ']);
				for(int j = 0; j < messages[i].Length; j++)
				{
					char c;
					if (!AestheticMap.TryGetValue(messages[i][j], out c)) c = messages[i][j];
					vape.Append(c);
				}
			}

			string msg = vape.ToString();
			if (!string.IsNullOrEmpty(msg)) 
				await ctx.RespondAsync(vape.ToString());
		}

		[Command("roll")]
		[Aliases("r")]
		[Description("Rolls dice")]
		public async Task Roll(CommandContext ctx, [RemainingText] string expression)
		{
            //Prepare the expresion and result
            string expr = Regex.Replace(expression, "\\d*d\\d+", DiceRegexReplacer); 
            double result = 0;
            
            //Evaluate
            DataTable table = new DataTable();
            table.Columns.Add("expression", typeof(string), expr);
            var row = table.NewRow();
            table.Rows.Add(row);
            result = double.Parse((string)row["expression"]);
            
            //Return the result
            await ctx.RespondAsync(content: $"🎲 `{result}`\n        `{expr}`");
		}

        private string DiceRegexReplacer(Match match)
        {
            int tally = 0;
            uint count, sides;
            string[] parts = match.Value.Split('d');
            

            //Make sure its parsed correctly
            if (parts.Length != 2) return match.Value;
            if (!uint.TryParse(parts[0], out count)) count = 1;
            if (!uint.TryParse(parts[1], out sides)) sides = 6;

            //Roll the dice numerous times
            Random random = new Random();
            for (uint i = 0; i < count; i++)
                tally += random.Next(1, (int) sides);

            //return the tally
            return tally.ToString();
        }
	}
}

