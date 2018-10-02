using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace V21Bot.Imgur.Models
{
    public class ImgurAlbum
    {
        [JsonProperty("id")]
        public string ID { get; private set; }
        [JsonProperty("title")]
        public string Title { get; private set; }
        [JsonProperty("description")]
        public string Description { get; private set; }
        [JsonProperty("cover")]
        public string Cover { get; private set; }
        [JsonProperty("cover_width")]
        public int CoverWidth { get; private set; }
        [JsonProperty("cover_height")]
        public int CoverHeight { get; private set; }
        [JsonProperty("account_url")]
        public string AccountUrl { get; private set; }
        [JsonProperty("account_id")]
        public int AccountID { get; private set; }
        [JsonProperty("privacy")]
        public string Privacy { get; private set; }
        [JsonProperty("layout")]
        public string Layout { get; private set; }
        [JsonProperty("views")]
        public int Views { get; private set; }
        [JsonProperty("link")]
        public string Link { get; private set; }
        [JsonProperty("ups")]
        public int Ups { get; private set; }
        [JsonProperty("downs")]
        public int Downs { get; private set; }
        [JsonProperty("points")]
        public int Points { get; private set; }
        [JsonProperty("score")]
        public int Score { get; private set; }
        [JsonProperty("is_album")]
        public bool IsAlbum { get; private set; }
        [JsonProperty("nsfw")]
        public bool NSFW { get; private set; }
        [JsonProperty("topic")]
        public string Topic { get; private set; }

        [JsonProperty("images_count")]
        public int ImageCount { get; private set; }

        [JsonProperty("images")]
        public ImgurImage[] Images { get; private set; }

        [JsonProperty("in_most_viral")]
        public bool InMostViral { get; private set; }
    }
}
