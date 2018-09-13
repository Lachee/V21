using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace V21Bot.Imgur.Response
{
	interface IImgurData { }
	class ImgurResponse
	{
		[JsonProperty("success")]
		public bool Success { get; private set; }

		[JsonProperty("status")]
		public int Status { get; private set; }
		
		[JsonProperty("data")]
		public JToken Data { get; private set; }
	}
	
}
