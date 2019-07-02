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

            //The game => Role feature
            Discord.PresenceUpdated += async (evt) =>
            {
                if (evt.Game == null) return;

                //Get the game then the role
                string gameName = evt.Game.Name.ToLowerInvariant().Replace(" ", "");
                string key = RedisNamespace.Create(evt.Guild.Id, "rolemap", "game", gameName);
                string roleIdStr = await Redis.FetchStringAsync(key, null);
                if (roleIdStr != null && ulong.TryParse(roleIdStr, out var roleId))
                {
                    var role = evt.Guild.GetRole(roleId);
                    if (role != null)
                    {
                        if (!evt.Member.Roles.Contains(role))
                        {
                            Console.WriteLine("Applying Role {0} to {1} because game mapped {2}", role, evt.Member, gameName);
                            await evt.Member.GrantRoleAsync(role, "Role Game Map");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid Role!");
                        await Redis.RemoveAsync(key);
                    }
                }
            };

            //The deg think feature
            Discord.MessageCreated += async (evt) =>
            {
                if (evt.Message.Author.IsBot) return;
                if (evt.Message.Content.Contains("deg ") || evt.Message.Content.TrimEnd('!', '?', '.', ':', '*').EndsWith(" deg"))
                {
                    await evt.Message.CreateReactionAsync(DiscordEmoji.FromName(Discord, ":dethinks:"));
                    return;
                }
            };

            //The rolemap feature
            Discord.MessageReactionAdded += async (evt) =>
            {
                string key = RedisNamespace.Create(evt.Channel.GuildId, "rolemap", evt.Message.Id, evt.Emoji.Id);

                string roleString = await Redis.FetchStringAsync(key);
                ulong roleId;
                DiscordRole role;

                if (roleString != null && ulong.TryParse(roleString, out roleId) && (role = evt.Channel.Guild.GetRole(roleId)) != null)
                {
                    var member = await evt.Channel.Guild.GetMemberAsync(evt.User);
                    member?.GrantRoleAsync(role, "Rolemap Reaction");
                }
            };

            Discord.MessageReactionRemoved += async (evt) =>
            {
                string key = RedisNamespace.Create(evt.Channel.GuildId, "rolemap", evt.Message.Id, evt.Emoji.Id);

                string roleString = await Redis.FetchStringAsync(key);
                ulong roleId;
                DiscordRole role;

                if (roleString != null && ulong.TryParse(roleString, out roleId) && (role = evt.Channel.Guild.GetRole(roleId)) != null)
                {
                    var member = await evt.Channel.Guild.GetMemberAsync(evt.User);
                    member?.RevokeRoleAsync(role, "Rolemap Reaction");
                }
            };
            
            //Command handling (mute channels and re-executing)
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
                if (args.NicknameAfter != args.NicknameBefore)
                {

                    //Check if the element exists
                    string redisNamespace = EnforcedNickname.GetRedisNamespace(args.Guild, args.Member);
                    EnforcedNickname enforcement = await Redis.FetchObjectAsync<EnforcedNickname>(redisNamespace);

                    //Update the nickname if it does
                    if (enforcement != null && enforcement.Nickname != args.NicknameAfter)
                        await args.Member.ModifyAsync(nickname: enforcement.Nickname, reason: $"Nickname enforcement by {enforcement.ResponsibleName}");
                }

                //Only want this for role change
                if (args.RolesBefore != args.RolesAfter)
                {
                    bool ismuted = await args.Member.IsMutedAsync();
                    if (ismuted)
                    {
                        var muteRole = await args.Guild.GetMuteRoleAsync();
                        if (args.RolesAfter.Count != 1 || !args.RolesAfter.Contains(muteRole))
                        {
                            Console.WriteLine("Re-Enforcing Mute of " + args.Member.Username + ". They changed their roles.");
                            await args.Member.MuteAsync("Mute evasion by role change.", false);
                        }
                    }
                }
            };

            //A member joined
            Discord.GuildMemberAdded += async (args) =>
            {
                //Send the welcome message
                await SendWelcomeMessage(args.Guild, args.Member);

                //Check if they need to be muted
                bool ismuted = await args.Member.IsMutedAsync();
                if (ismuted)
                {
                    Console.WriteLine("Re-Enforcing Mute of " + args.Member.Username + ". They rejoined.");
                    await args.Member.MuteAsync("MUte evasion by leave.", false);
                }
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
                await Redis.AddHashSetAsync(mutedKey, channel.Id.ToString());
            }
        }
        public async Task UnmuteChannel(DiscordChannel channel)
        {
            _mutedChannels.Remove(channel.Id);
            if (RedisAvailable)
            {
                string mutedKey = RedisNamespace.Create("global", "muted");
                await Redis.RemoveHashSetASync(mutedKey, channel.Id.ToString());
            }
        }
        public bool IsChannelMuted(DiscordChannel channel)
        {
            return _mutedChannels.Contains(channel.Id);
        }
     
        public async Task SetWelcomeMessageChannel(DiscordGuild guild, DiscordChannel channel, string message)
        {
            string key = RedisNamespace.Create(guild.Id, "welcome");
            if (channel == null || message == null)
                await Redis.RemoveAsync(key);
            else
                await Redis.StoreObjectAsync(key, new WelcomeMessage() { ChannelId = channel.Id, Message = message });
            
        }
        public async Task SendWelcomeMessage(DiscordGuild guild, DiscordMember member)
        {
            //Make sure the guild has the welcome message enabled
            string key = RedisNamespace.Create(guild.Id, "welcome");
            WelcomeMessage value = await Redis.FetchObjectAsync<WelcomeMessage>(key);
            if (value != null) await value.SendWelcome(member);
        }   
        #endregion

        #region Commands
        private Task<int> CheckPrefixPredicate(DiscordMessage msg)
        {
            try
            {
                if (msg.Author.IsBot) return Task.FromResult(-1);

                //Make sure it exists
                int index = msg.Content.StartsWith(Config.Prefix) ? 0 : -1;
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
                _mutedChannels = new HashSet<ulong>((await Redis.FetchHashSetAsync(mutedKey)).Select(v => ulong.Parse(v)));
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
