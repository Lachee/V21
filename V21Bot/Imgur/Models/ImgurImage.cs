using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace V21Bot.Imgur.Models
{
    [JsonObject(ItemRequired = Required.Default, MemberSerialization = MemberSerialization.OptIn, ItemReferenceLoopHandling = ReferenceLoopHandling.Error)]
	public class ImgurImage
	{
		[JsonProperty("id")]
		public string ID { get; private set; }
		[JsonProperty("title")]
		public string Title { get; private set; }
		[JsonProperty("description")]
		public string Description { get; private set; }
		[JsonProperty("datetime")]
		public ulong DateTime { get; private set; }
		[JsonProperty("type")]
		public string Type { get; private set; }
		[JsonProperty("animated")]
		public bool Animated { get; private set; }
		[JsonProperty("width")]
		public int Width { get; private set; }
		[JsonProperty("height")]
		public int Height { get; private set; }
		[JsonProperty("size")]
		public ulong Size { get; private set; }
		[JsonProperty("views")]
		public ulong Views { get; private set; }
		[JsonProperty("bandwidth")]
		public ulong Bandwidth { get; private set; }
		[JsonProperty("nsfw")]
		public bool? NSFW { get; private set; }
		[JsonProperty("section")]
		public string Section { get; private set; }
		[JsonProperty("link")]
		public string Link { get; private set; }
		[JsonProperty("ups")]
		public int? Ups { get; private set; }
		[JsonProperty("downs")]
		public int? Downs { get; private set; }
		[JsonProperty("score")]
		public int? Score { get; private set; }
	}
}
