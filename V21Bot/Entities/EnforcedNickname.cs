using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using V21Bot.Redis;
using V21Bot.Redis.Serialize;

namespace V21Bot.Entities
{
    class EnforcedNickname
    {
        [RedisProperty("nick")]
        public string Nickname { get; set; }

        [RedisProperty("min")]
        public int HighestRole { get; set; }

        [RedisProperty("admin")]
        public ulong Responsible { get; set; }

        [RedisProperty("admin_name")]
        public string ResponsibleName { get; set; }

        public static string GetRedisNamespace(DiscordGuild guild, DiscordMember user)
        {
            return RedisNamespace.Create(guild.Id, "nicknames", user.Id);
        }
    }
}
