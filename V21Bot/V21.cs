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
using DSharpPlus.Interactivity;
using V21Bot.Entities;

namespace V21Bot
{
	public class V21
	{
		public static V21 Instance { get; private set; }

		public DiscordClient Discord { get; }
        public CommandsNextModule Commands { get; }
        public InteractivityModule Interactivty { get; }
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
            Console.WriteLine("Creating Commands (Prefix: {0})", Config.Prefix);
			Commands = Discord.UseCommandsNext(new CommandsNextConfiguration() { StringPrefix = Config.Prefix });
			Commands.RegisterCommands(System.Reflection.Assembly.GetExecutingAssembly());
			Commands.CommandErrored += async (args) => await args.Context.RespondException(args.Exception);

            Interactivty = Discord.UseInteractivity(new InteractivityConfiguration() {
                PaginationBehaviour = TimeoutBehaviour.Delete,
                PaginationTimeout = new TimeSpan(0, 10, 0),
                Timeout = new TimeSpan(0, 10, 0),
            });

            Discord.MessageUpdated += async (args) =>
            {
                if (args.Author.IsBot) return;

                var app = await Discord.GetCurrentApplicationAsync();
                if (args.Author != app.Owner) return;

                int mpos = args.Message.GetStringPrefixLength(Config.Prefix);
                if (mpos < 0) return;

                string content = args.Message.Content.Substring(mpos).ToLowerInvariant();
                if (content.StartsWith("eval ") || content.StartsWith("evaluate ") || content.StartsWith("$ "))
                {
                    Console.WriteLine("Re-Executing Command...");
                    await Commands.SudoAsync(args.Author, args.Channel, args.Message.Content);
                }
            };

            Discord.GuildMemberUpdated += async (args) =>
            {
                //Only want this for nickname change
                if (args.NicknameAfter == args.NicknameBefore) return;

                //Check if the element exists
                string redisNamespace = EnforcedNickname.GetRedisNamespace(args.Guild, args.Member);
                EnforcedNickname enforcement = await Redis.ObjectGetAsync<EnforcedNickname>(redisNamespace);

                //Update the nickname if it does
                if (enforcement != null && enforcement.Nickname != args.NicknameAfter)
                    await args.Member.ModifyAsync(nickname: enforcement.Nickname, reason: $"Nickname enforcement by {enforcement.ResponsibleName}");
                
            };

            //Create Redis
            if (Config.UseRedis)
            {
                Redis = new StackExchangeClient("localhost", 4);
                RedisNamespace.SetRoot("V21");
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
