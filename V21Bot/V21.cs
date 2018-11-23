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

            //Store pings to prevent ghostings
            //Discord.MessageCreated += async (evt) =>
            //{
            //    if (evt.MentionedUsers.Count == 0 && evt.MentionedRoles.Count == 0) return;
            //    if (evt.Author.IsBot) return;
            //    if (evt.Message.Embeds.Count != 0) return;
            //
            //    //Prepare namespaces and tll
            //    string pingNamespace = RedisNamespace.Create(evt.Guild.Id, "pings", evt.Message.Id);
            //    TimeSpan TTL = new TimeSpan(1, 0, 0, 0, 0);
            //
            //    //Store all the redis!                
            //    await Redis.HashSetAsync(pingNamespace, new Dictionary<string, string>()
            //    {
            //        { "author", evt.Author.Id.ToString() },
            //        { "username", evt.Author.Username },
            //        { "content", evt.Message.Content } ,
            //        { "user_c", evt.MentionedUsers.Count.ToString() },
            //        { "roles_c", evt.MentionedRoles.Count.ToString() },
            //    });
            //    await Redis.ExpireAsync(pingNamespace, TTL);
            //
            //    //string usersNamespace = RedisNamespace.Create(evt.Guild.Id, evt.Message.Id, "users");
            //    //string rolesNamespace = RedisNamespace.Create(evt.Guild.Id, evt.Message.Id, "roles");
            //    //await Redis.SetAddAsync(rolesNamespace, evt.MentionedRoles.Select(r => r.Id.ToString()).ToHashSet());
            //    //await Redis.ExpireAsync(rolesNamespace, TTL);
            //    //await Redis.SetAddAsync(usersNamespace, evt.MentionedUsers.Select(r => r.Id.ToString()).ToHashSet());
            //    //await Redis.ExpireAsync(usersNamespace, TTL);
            //};

            //Was a message deleted?
            //Discord.MessageDeleted += async (evt) =>
            //{
            //    string pingNamespace = RedisNamespace.Create(evt.Guild.Id, "pings", evt.Message.Id);
            //    var pingCache = await Redis.HashGetAsync(pingNamespace);
            //    if (pingCache != null)
            //    {
            //        StringBuilder msg = new StringBuilder();
            //        msg.AppendLine("👻 **Ghost Ping Detected**");
            //        msg.AppendLine(pingCache["content"]);
            //        msg.AppendLine($"- <@{pingCache["author"]}>");
            //        await evt.Channel.SendMessageAsync(msg.ToString());
            //    };
            //};

            //Discord.MessageUpdated += async (evt) =>
            //{
            //    string pingNamespace = RedisNamespace.Create(evt.Guild.Id, "pings", evt.Message.Id);
            //    var pingCache = await Redis.HashGetAsync(pingNamespace);
            //    if (pingCache != null)
            //    {
            //        if (evt.MentionedUsers.Count.ToString() != pingCache["user_c"] || evt.MentionedRoles.Count.ToString() != pingCache["roles_c"])
            //        {
            //            StringBuilder msg = new StringBuilder();
            //            msg.AppendLine("👻 **Ghost Ping Detected**");
            //            msg.AppendLine(pingCache["content"]);
            //            msg.AppendLine($"- <@{pingCache["author"]}>");
            //            await evt.Channel.SendMessageAsync(msg.ToString());
            //        }
            //    };
            //};

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

            //A member updated
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

            //A member joined
            Discord.GuildMemberAdded += async (args) =>
            {
                await Task.Delay(150);
                await SendWelcomeMessage(args.Guild, args.Member);
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
     
        public async Task SetWelcomeMessageChannel(DiscordGuild guild, DiscordChannel channel)
        {
            string key = RedisNamespace.Create(guild.Id, "welcome", "channel");
            if (channel == null)
                await Redis.RemoveAsync(key);            
            else            
                await Redis.StringSetAsync(key, channel.Id.ToString());
            
        }
        public async Task SendWelcomeMessage(DiscordGuild guild, DiscordMember user)
        {
            //Make sure the guild has the welcome message enabled
            string key = RedisNamespace.Create(guild.Id, "welcome", "channel");
            string value = await Redis.StringGetAsync(key);
            if (value != null)
            {
                var channel = guild.GetChannel(ulong.Parse(value));
                await channel.TriggerTypingAsync();

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder();
                embed.WithColor(DiscordColor.Green)
                    .WithDescription(
                        "📥 " + user.Mention + " _has joined the server_."
                    );
                embed.WithFooter(text: $"User Joined ({user.Id})", icon_url: $"https://d.lu.je/avatar/{user.Id}");

                var timespan = DateTime.UtcNow - user.CreationTime();
                if (timespan.TotalDays < 14) embed.AddField("Account Age", timespan.TotalDays.ToString("f1") + " days", true);

                await channel.SendMessageAsync(embed: embed);
            }
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
