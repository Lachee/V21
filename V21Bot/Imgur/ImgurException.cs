using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace V21Bot.Imgur
{
	public class ImgurException : Exception
	{
		public string Error { get; }
		public string Request { get; }
		public string Method { get; }
		public int Status { get; }
		public string InnerMessage { get; }

		internal ImgurException(Imgur.Response.ImgurResponse response) : base("ImgurException " + response.Status)
		{
			var err = response.Data.ToObject<ImgurError>();
			Error	= err.Error;
			Request = err.Request;
			Method  = err.Method;
			Status = response.Status;

			InnerMessage = string.Format("ImgurException ({0}): {1} [{2}: {3}]", Status, Error, Method, Request);
		}

		public override string ToString()
		{
			return InnerMessage;
		}

		public struct ImgurError
		{
			[JsonProperty("error")]
			public string Error { get; }
			[JsonProperty("request")]
			public string Request { get; }
			[JsonProperty("method")]
			public string Method { get; }
		}
	}
}
