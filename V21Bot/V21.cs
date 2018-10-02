using DSharpPlus;
using DSharpPlus.Net.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using V21Bot.Redis;
using V21Bot.Helper;
using V21Bot.Imgur;

namespace V21Bot
{
	public class V21
	{
		public static V21 Instance { get; private set; }

		public DiscordClient Discord { get; }
		public CommandsNextModule Commands { get; }
		public IRedisClient Redis { get; }
        public bool RedisAvailable { get; }
		public ImgurClient Imgur { get; }

		public BotConfig Config { get; }

		public V21(BotConfig config)
		{
			Instance = this;
			Config = config;

			//Setup the token
			var dconf = new DiscordConfiguration();
			dconf.Token = Config.GetDiscordKey();
			dconf.TokenType = TokenType.Bot;

			//Create the client
			Discord = new DiscordClient(dconf);
			if (Config.WebSocket4Net)
				Discord.SetWebSocketClient<WebSocket4NetCoreClient>();

			//Create Commands
			Commands = Discord.UseCommandsNext(new CommandsNextConfiguration() { StringPrefix = Config.Prefix });
			Commands.RegisterCommands(System.Reflection.Assembly.GetExecutingAssembly());
			Commands.CommandErrored += async (args) => await args.Context.RespondException(args.Exception);

            //Create Redis
            if (Config.UseRedis)
            {
                Redis = new StackExchangeClient("localhost", 4);
                RedisTools.RootNamespace = "V21";
                RedisAvailable = true;
            }
            else
            {
                Redis = null;
                RedisAvailable = false;
            }

			//Create Imgur
			Imgur = new ImgurClient(Config.GetImgurKey());
		}

		public async Task Initialize()
		{
			//Connect to redis
            if (RedisAvailable) await Redis.Initialize();

			//Connect to discord
			await Discord.ConnectAsync();
			
			//Get our user and assign the default avatar
			ResponseBuilder.DefaultBotImage = ResponseBuilder.AvatarAPI + Discord.CurrentUser.Id;

		}
		

		public async Task Deinitialize()
		{
			Redis.Dispose();
			await Discord.DisconnectAsync();
		}
	}
}
