using DSharpPlus;
using DSharpPlus.Net.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using V21Bot.Redis;
using V21Bot.Helper;

namespace V21Bot
{
	class V21
	{
		public static V21 Instance { get; private set; }

		public string Resources { get; set; }

		public DiscordClient Discord { get; }
		public CommandsNextModule Commands { get; }
		public IRedisClient Redis { get; }

		public V21(string token, DiscordConfiguration config = null, bool useWebsocketFix = false)
		{
			Instance = this;

			if (config == null)
			{
				config = new DiscordConfiguration()
				{
					UseInternalLogHandler = true,
					LogLevel = LogLevel.Warning
				};
			}

			//Setup the token
			config.Token = token;
			config.TokenType = TokenType.Bot;

			//Create the client
			Discord = new DiscordClient(config);
			if (useWebsocketFix)
				Discord.SetWebSocketClient<WebSocket4NetCoreClient>();

			//Create Commands
			Commands = Discord.UseCommandsNext(new CommandsNextConfiguration() { StringPrefix = ">" });
			Commands.RegisterCommands(System.Reflection.Assembly.GetExecutingAssembly());
			Commands.CommandErrored += async (args) => await args.Context.RespondException(args.Exception);
		
			//Create Redis
			Redis = new StackExchangeClient("localhost", 4);
			RedisTools.RootNamespace = "V21";
		}

		public async Task Initialize()
		{
			//Connect to redis
			await Redis.Initialize();

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
