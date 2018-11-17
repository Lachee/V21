using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V21Bot.Helper
{
	public class ResponseBuilder : DiscordEmbedBuilder
	{
		public static string AvatarAPI { get; set; } = "https://d.lu.je/avatar/";
		public static string DefaultBotImage { get; set; } = "https://i.imgur.com/KLNIH31.png";
		public static string DefaultBotUrl { get; set; } = "http://hyperlight.chickatrice.net/";

		public static DiscordColor DefaultColour { get; set; } = new DiscordColor(12794879);
        public static DiscordColor ErrorColour { get; set; } = DiscordColor.DarkRed;
        public static DiscordColor WarningColour { get; set; } = DiscordColor.Orange;

		public string BotImage { get; set; } = DefaultBotImage;
		public string BotUrl { get; set; } = DefaultBotUrl;
		
		public ResponseBuilder (CommandContext ctx, Exception exception, bool showStackTrace) : this(ctx)
        {
            //Set our colour
            Color = ErrorColour;

            if (exception is AggregateException)
            {
                //Aggregate exceptions, we should log the first one only.
                AggregateException aggregate = exception as AggregateException;
                this.Description = string.Format("An `AggregateException` has occured during the {0} command. Here is the first inner exception: ```{1}``` " + (showStackTrace ? "**Stacktrace** ```haskell\n{2}\n```" : ""),
                    ctx.Command.Name, aggregate.InnerException.Message, aggregate.InnerException.StackTrace);
            }
            else
            {
                this.Description = string.Format("An exception has occured during the {0} command: ```{1}``` " + (showStackTrace ? "**Stacktrace** ```haskell\n{2}\n```" : ""), 
                    ctx.Command.Name, exception.Message, exception.StackTrace);

            }

		}
		public ResponseBuilder(CommandContext ctx) : this(ctx, ctx.Command.Name + " Response") { }
		public ResponseBuilder(CommandContext ctx, string title) : base()
		{
			if (title.Length > 1)
			{
				var c = title[0];
				title = c.ToString().ToUpperInvariant() + title.Substring(1);
			}

			Footer = new DiscordEmbedBuilder.EmbedFooter()
			{
				Text = "Response to " + ctx.User.Username,
				IconUrl = AvatarAPI + ctx.User.Id
			};
			Author = new DiscordEmbedBuilder.EmbedAuthor()
			{
				Name = title,
				Url = BotUrl,
				IconUrl = BotImage
			};
            Color = DefaultColour;
			Timestamp = DateTimeOffset.Now;
		}		

		public ResponseBuilder WithDescription(string format, params object[] args)
		{
			this.Description = string.Format(format, args);
			return this;
		}
    }

    public static class ResponseBuilderExtensions
    {
        public static async Task<DiscordMessage> RespondEmbed(this CommandContext ctx, string description) =>
            await ctx.RespondAsync(embed: new ResponseBuilder(ctx).WithDescription(description));

        public static async Task<DiscordMessage> RespondEmbed(this CommandContext ctx, string format, params object[] args) =>
                await ctx.RespondAsync(embed: new ResponseBuilder(ctx).WithDescription(format, args));

        public static async Task<DiscordMessage> RespondEmbed(this CommandContext ctx, DiscordColor color, string description) =>
            await ctx.RespondAsync(embed: new ResponseBuilder(ctx).WithDescription(description).WithColor(color));

        public static async Task<DiscordMessage> RespondEmbed(this CommandContext ctx, DiscordColor color, string format, params object[] args) =>
            await ctx.RespondAsync(embed: new ResponseBuilder(ctx).WithDescription(format, args).WithColor(color));

        public static async Task<DiscordMessage> RespondException(this CommandContext ctx, Exception exception, bool showStackTrace = true) =>
            await ctx.RespondAsync(embed: new ResponseBuilder(ctx, exception, showStackTrace));

        public static async Task<DiscordMessage> RespondException(this CommandContext ctx, string exception) =>
            await ctx.RespondAsync(embed: new ResponseBuilder(ctx).WithDescription("An error has occured during the {0} command: ```\n{1}\n```", ctx.Command.Name, exception).WithColor(ResponseBuilder.ErrorColour));

    }
}
