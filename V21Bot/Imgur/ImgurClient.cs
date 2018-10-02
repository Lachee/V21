using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using V21Bot.Imgur.Models;
using V21Bot.Imgur.Response;

namespace V21Bot.Imgur
{
	public class ImgurClient
	{
		public string ApiUrl => "https://api.imgur.com/3/";

		public string ClientID { get; }
		public ImgurClient(string clientID)
		{
			this.ClientID = clientID;
		}

        /// <summary>
        /// View gallery images for a subreddit
        /// </summary>
        /// <param name="topic">pics - A valid subreddit name</param>
        /// <param name="page">time | top - defaults to time</param>
        /// <param name="sort">integer - the data paging number</param>
        /// <param name="window">Change the date range of the request if the sort is "top". Options are day | week | month | year | all. Defaults to week</param>
        public async Task<ImgurImage[]> GetSubredditGallery(string subreddit, int page = 0, SortMode sort = SortMode.Time, Window window = Window.Week)
        {
            try
            {
                var response = await GetRequest($"/gallery/r/{subreddit}/{sort}/{window}/{page}");
                if (!response.Success) throw new ImgurException(response);

                var list = response.Data.ToObject<ImgurImage[]>();
                return list;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new ImgurImage[0];
            }
        }

        /// <summary>
        /// View gallery images for a topic
        /// </summary>
        /// <param name="topic">pics - A valid subreddit name</param>
        /// <param name="page">time | top - defaults to time</param>
        /// <param name="sort">integer - the data paging number</param>
        /// <param name="window">Change the date range of the request if the sort is "top". Options are day | week | month | year | all. Defaults to week</param>
        public async Task<ImgurAlbum[]> GetSubredditTopic(string topic, int page = 0, SortMode sort = SortMode.Time, Window window = Window.Week)
        {
            try
            {
                var response = await GetRequest($"/gallery/t/{topic}/{sort}/{window}/{page}");
                if (!response.Success) throw new ImgurException(response);

                var list = response.Data["items"].ToObject<ImgurAlbum[]>();
                return list;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new ImgurAlbum[0];
            }
        }

        private async Task<ImgurResponse> GetRequest(string request)
		{
			using (WebClient client = new WebClient())
			{
				//string request = string.Format(requestFormat, parameters);

				client.Headers.Add("Authorization", $"Client-ID {ClientID}");
				string json = await client.DownloadStringTaskAsync(ApiUrl + request);
				return JsonConvert.DeserializeObject<ImgurResponse>(json);
			}
		}
	}
}
