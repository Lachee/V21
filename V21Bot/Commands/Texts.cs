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

		[Command("coffee")]
		[Description("Gives you a fun coffee fact")]
		public async Task Coffee(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			var coffee = DiscordEmoji.FromName(ctx.Client, ":coffee:");
			
			var lines = await File.ReadAllLinesAsync(Path.Combine(V21.Instance.Resources, "facts.coffee.txt"));
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
		public async Task Roll(CommandContext ctx, [RemainingText] params string[] messages)
		{
			//roll [op] value, op value, op value, op value
//roll 1d10
//roll 1d10 + 5
//roll 2d20 - 10
//roll 3d10 + 5d20 + 5
/*
 * 3D10:	3 + 10 + 2 
 * 5D20:	15 + 17 + 8 + 1
 * 5:		5
 * 
 */
		}

		private bool RollValue(Random random, string dice, out int value)
		{
			if (int.TryParse(dice, out value)) return true;
			var match = DiceRegex.Match(dice);
			if (match.Success)
			{
				int count = 0;
				int sides = 0;
				bool isNegative = false;

				if (!string.IsNullOrEmpty(match.Groups["count"].Value))
				{
					if (!int.TryParse(match.Groups["count"].Value, out count) || count < 1)
						return false;
				}

				if (!string.IsNullOrEmpty(match.Groups["sides"].Value))
				{
					if (!int.TryParse(match.Groups["sides"].Value, out sides))
						return false;

					if (sides >= -1 && sides <= 1)
						return false;
					
					if (sides < 0)
					{
						isNegative = true;
						sides *= -1;
					}
				}

				//Do the random iterations, make sure we capout the count tho
				for (int i = 0; i < count; i++)
					value += random.Next(sides);

				if (isNegative) value *= -1;
				return true;
			}

			return false;
		}
	}
}

