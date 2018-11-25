using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace V21Bot.Entities
{
    public class WelcomeMessage
    {
        [Redis.Serialize.RedisProperty("channel")]
        public ulong ChannelId { get; set; }

        [Redis.Serialize.RedisProperty("message")]
        public string Message { get; set; }

        public async Task<DiscordMessage> SendWelcome(DiscordMember member)
        {
            if (member == null) return null;

            var channel = member.Guild.GetChannel(ChannelId);
            if (channel == null) return null;

            string content = Message.Replace("{user}", "<@" + member.Id + ">");
            return await channel.SendMessageAsync(content);
        }
    }
}
