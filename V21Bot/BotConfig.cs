using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace V21Bot
{
	class BotConfig
	{
		public string Prefix { get; set; } = ">";
		public string DiscordKeyFile { get; set; } = "discord.key";
		public string ImgurKeyFile { get; set; } = "imgur.key";
		public string Resources { get; set; } = "Resources/";
		public bool WebSocket4Net { get; set; } = false;

		private string _discordkey;
		public string GetDiscordKey()
		{
			if (string.IsNullOrEmpty(_discordkey))
				if (File.Exists(DiscordKeyFile))
					_discordkey = File.ReadAllText(DiscordKeyFile);
			return _discordkey;
		}

		private string _imgurkey;
		public string GetImgurKey()
		{
			if (string.IsNullOrEmpty(_imgurkey))
				if (File.Exists(ImgurKeyFile))
					_imgurkey = File.ReadAllText(ImgurKeyFile);
			return _imgurkey;
		}
	}
}
