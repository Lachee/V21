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
using DSharpPlus.Entities;
using System.Linq;

namespace V21Bot
{
	public class V21
	{
		public static V21 Instance { get; private set; }
        private HashSet<ulong> _mutedChannels;

		public DiscordClient Discord { get; }
        public CommandsNextModule Commands { get; }

        public InteractivityModule Interactivty { get; }
        public IRedisClient Redis { get; }
        public bool RedisAvailable { get; }
		public ImgurClient Imgur { get; }

		public BotConfig Config { get; }

        public DiscordUser Owner => Discord.CurrentApplication.Owner;

        public V21(BotConfig config)
		{
			Instance = this;
			Config = config;
            _mutedChannels = new HashSet<ulong>();

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
            Commands = Discord.UseCommandsNext(new CommandsNextConfiguration() { CustomPrefixPredicate = CheckPrefixPredicate });
			Commands.RegisterCommands(System.Reflection.Assembly.GetExecutingAssembly());
			Commands.CommandErrored += async (args) => await args.Context.RespondException(args.Exception);
          
            //Create Interactivity
            Interactivty = Discord.UseInteractivity(new InteractivityConfiguration() {
                PaginationBehaviour = TimeoutBehaviour.Delete,
                PaginationTimeout = new TimeSpan(0, 10, 0),
                Timeout = new TimeSpan(0, 10, 0),
            });
            
            //Create basic event handlers
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

        #region Helpers
        public async Task MuteChannel(DiscordChannel channel)
        {
            _mutedChannels.Add(channel.Id);
            if (RedisAvailable)
            {
                string mutedKey = RedisNamespace.Create("global", "muted");
                await Redis.SetAddAsync(mutedKey, channel.Id.ToString());
            }
        }
        public async Task UnmuteChannel(DiscordChannel channel)
        {
            _mutedChannels.Remove(channel.Id);
            if (RedisAvailable)
            {
                string mutedKey = RedisNamespace.Create("global", "muted");
                await Redis.SetRemoveAsync(mutedKey, channel.Id.ToString());
            }
        }
        public bool IsChannelMuted(DiscordChannel channel)
        {
            return _mutedChannels.Contains(channel.Id);
        }
        #endregion

        #region Commands
        private Task<int> CheckPrefixPredicate(DiscordMessage msg)
        {
            try
            {
                //Make sure it exists
                int index = msg.Content.IndexOf(Config.Prefix);
                if (index < 0) return Task.FromResult(-1);

                //Update the index and offset it by the preficx
                index += Config.Prefix.Length;

                //We are being told by the owner. Not allowed to ignore
                if (msg.Author.Id == Owner.Id)
                    return Task.FromResult(index);

                //We are in a muted channel, so abort
                if (IsChannelMuted(msg.Channel))
                    return Task.FromResult(-1);

                //We are fine, so just send the index
                return Task.FromResult(index);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                return Task.FromResult(-1);
            }
        }
        #endregion

        #region Initialization
        public async Task Initialize()
		{
            //Connect to redis
            if (RedisAvailable)
            {
                //Connect
                await Redis.Initialize();

                //Load muted channels
                string mutedKey = RedisNamespace.Create("global", "muted");
                _mutedChannels = new HashSet<ulong>((await Redis.SetGetAsync(mutedKey)).Select(v => ulong.Parse(v)));
            }

			//Connect to discord
			await Discord.ConnectAsync();
			
			//Get our user and assign the default avatar
			ResponseBuilder.DefaultBotImage = ResponseBuilder.AvatarAPI + Discord.CurrentUser.Id;
		}
		public async Task Deinitialize()
		{
            if (RedisAvailable)
            {
                Redis.Dispose();
            }
			await Discord.DisconnectAsync();
		}
        #endregion
    }

}
