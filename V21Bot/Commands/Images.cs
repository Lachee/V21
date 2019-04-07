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
using V21Bot.Imgur.Models;

namespace V21Bot.Commands
{
	public class Images
	{
		public static int CacheLifetime = 5 * 60 * 60;
		public static string EmojiGenerating = ":paintbrush:";
		public static string EmojiFolder = ":file_folder:";

		public static string OutputFile = null;

        [Command("avatar")]
        [Aliases("avi", "pfp", "ava")]
        public async Task Avatar(CommandContext ctx, [Description("Optional user to get the avatar off")] DiscordUser user = null)
        {
            if (user == null)
                user = ctx.User;

            string format = "";
            if (user.AvatarHash != null && user.AvatarHash.StartsWith("a_")) format = ".gif";
            await ctx.RespondAsync($"https://d.lu.je/avatar/{user.Id}{format}");
        }

        [Command("cute")]
        [Aliases("aww", "pet", "animals", "animal")]
        [Description("Gives a cute animal")]
        public async Task Cute(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            Random random = new Random();
            Imgur.Models.ImgurImage[] images = await V21.Instance.Imgur.GetSubredditGallery("aww", random.Next(0, 250));
            Imgur.Models.ImgurImage image = null;

            if (images.Length == 0)
            {
                await ctx.RespondAsync(":interrobang: Found no images to display?");
                return;
            }

            string[] allowedTypes = { "" };
            do
            {
                await ctx.TriggerTypingAsync();
                image = images[random.Next(images.Length)];
            }
            while (image == null 
                    || !image.Type.StartsWith("image")
                    || !image.Link.StartsWith("https") 
                    || image.NSFW.GetValueOrDefault(false)
                    || !image.Animated);

            var builder = new ResponseBuilder(ctx)
                .WithImageUrl(image.Link)
                .WithUrl(image.Link)
                .WithAuthor(image.Title, image.Link);

            await ctx.RespondAsync(embed: builder);
        }

        [Command("spook")]
        [Aliases("skele", "scarry", "spooky", "skeli")]
        [Description("Gives a cute animal")]
        public async Task Spook(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            Random random = new Random();
            Imgur.Models.ImgurAlbum[] albums = await V21.Instance.Imgur.GetSubredditTopic("skeletonwar", 0);
            Imgur.Models.ImgurImage image = null;

            if (albums.Length == 0)
            {
                await ctx.RespondAsync(":interrobang: Found no images to display?");
                return;
            }

            string[] allowedTypes = { "" };
            do
            {
                await ctx.TriggerTypingAsync();
                var album = albums[random.Next(albums.Length)];
                for (int i = 0; i < album.Images.Length; i++)
                {
                    var img = album.Images[i];
                    if (img.Animated && !img.NSFW.GetValueOrDefault(false) && img.Link.StartsWith("https"))
                    {
                        image = img;
                        break;
                    }
                }
            }
            while (image == null 
                    || !image.Type.StartsWith("image")
                    || !image.Link.StartsWith("https") 
                    || image.NSFW.GetValueOrDefault(false)
                    || !image.Animated);

            var builder = new ResponseBuilder(ctx)
                .WithImageUrl(image.Link)
                .WithUrl(image.Link)
                .WithAuthor(image.Title, image.Link);

            await ctx.RespondAsync(embed: builder);
        }

        [Command("triggered")]
		[Aliases("trigger")]
		[Description("Triggers us or the supplied image")]
		public async Task Triggered(CommandContext ctx, [Description("Optional user to trigger")] DiscordUser user = null)
		{
			//If the user hasnt been assigned, we will set it to ourself
			if (user == null) user = ctx.User;

			//If the url is null, we will set it to the user specified
			string url = user.GetAvatarUrl(DSharpPlus.ImageFormat.Png, 256);
			
			//Generate the image
			await DownloadAndGenerate(ctx, new TriggeredMagick(), url);
		}

        [Command("pats")]
        [Aliases("pat")]
        [Description("Pats the discord user")]
        public async Task Pats(CommandContext ctx, [Description("The user to pat")] DiscordUser user = null)
        {
            //If the user hasnt been assigned, we will set it to ourself
            if (user == null) user = ctx.User;

            //If the url is null, we will set it to the user specified
            string url = user.GetAvatarUrl(DSharpPlus.ImageFormat.Png, 256);

            //Generate the image
            await DownloadAndGenerate(ctx, new PatsMagick(), url, ctx.Member.IsOwner);
        }

        [Command("hyper-pats")]
        [Aliases("hpat", "hp")]
        [Description("Pats the discord user")]
        public async Task Pats(CommandContext ctx, double speed, [Description("The user to pat")] DiscordUser user = null)
        {
            //If the user hasnt been assigned, we will set it to ourself
            if (user == null) user = ctx.User;

            //If the url is null, we will set it to the user specified
            string url = user.GetAvatarUrl(DSharpPlus.ImageFormat.Png, 256);

            //Generate the image
            await DownloadAndGenerate(ctx, new PatsMagick() { FrameCount = 60, FrameDelay = 1, Speed = speed } , url, ctx.Member.IsOwner);
        }

        #region Image Generation and Sending

        private async Task DownloadAndGenerate(CommandContext ctx, IMagick magick, string url, bool forceRecache = false)
		{
			//Trigger typing 
			await ctx.TriggerTypingAsync();

			string url64 = Utilities.Hash(url);
			string cachekey = Redis.RedisNamespace.Create("images", magick.Name, url64);

			//Check the cache for any images, if we have some, we dont need to resuse it.
			byte[] data = await FetchCachedImage(cachekey);
			if (!forceRecache && data != null && data.Length > 0)
			{
				//React to the message, say its okay and we understand the request
				DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, EmojiFolder);
				await ctx.Message.CreateReactionAsync(emoji);

				//Send it as a stream
				using (MemoryStream stream = new MemoryStream(data, false))
					await ctx.RespondWithFileAsync(stream, magick.GetFilename(ctx.User.Username));
			}
			else
			{
				//Prepare the data
				data = await DownloadImage(ctx, url);
				if (data == null) return;
			
				//Do the image effect
				data = await ExecuteMagick(ctx, magick, data);
				if (data == null) return;

				//Cache the bytes
				await StoreCachedImage(cachekey, data, CacheLifetime);

				//Write the bytes if requested
				if (!string.IsNullOrEmpty(OutputFile))
					await File.WriteAllBytesAsync(OutputFile, data);

				//Send it as a stream
				using (MemoryStream stream = new MemoryStream(data, false))
					await ctx.RespondWithFileAsync(stream, magick.GetFilename(ctx.User.Username));
			}
		}

		private async Task<byte[]> ExecuteMagick(CommandContext ctx, IMagick magick, byte[] source)
		{
			try
			{
				//React to the message, say its okay and we understand the request
				DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, EmojiGenerating);
				await ctx.Message.CreateReactionAsync(emoji);

				//Trigger the typing before we generate the image
				await ctx.TriggerTypingAsync();

				//Generate the image
				return await Task.Run<byte[]>(() =>
				{
					var image = new MagickImage(source);
					return magick.Generate(V21.Instance.Config.Resources, image);
				});				

			}
			catch (Exception e)
			{
				await ctx.RespondException(e, false);
				return null;
			}
		}
		private async Task<byte[]> DownloadImage(CommandContext ctx, string url)
		{
			try
			{
				//Prepare the data
				byte[] data = null;

				//Download and validate the URL.
				using (WebClient client = new WebClient())
					data = await client.DownloadDataTaskAsync(url);

				//Make sure its correct
				if (data == null || data.Length == 0)
				{
					await ctx.RespondEmbed(ResponseBuilder.ErrorColour, "Failed to download the image. Received 0 bytes or no data at all!");
					return null;
				}

				return data;
			}
			catch (Exception e)
			{
				//Catch any errors.
				await ctx.RespondException(e, false);
				return null;
			}
		}

		#endregion
		#region Caching

		/// <summary>
		/// Fetches a image stored in the cache. Returns null if does not exist.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private async Task<byte[]> FetchCachedImage(string key)
		{
			if (V21.Instance == null || V21.Instance.Redis == null)
				return null;

			string data = await V21.Instance.Redis.FetchStringAsync(key, null);
			if (data == null) return null;

			return System.Convert.FromBase64String(data);
		}

		/// <summary>
		/// Stores a image in cache.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="image"></param>
		/// <param name="TTL"></param>
		/// <returns></returns>
		private async Task StoreCachedImage(string key, byte[] image, int TTL)
		{
			if (V21.Instance == null || V21.Instance.Redis == null) return;

			//Get the data
			string data = System.Convert.ToBase64String(image);

			//Encode the image as bytes
			await V21.Instance.Redis.StoreStringAsync(key, data, TimeSpan.FromSeconds(TTL));
		}

		#endregion
	}
}

