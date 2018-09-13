using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace V21Bot
{
	class Program
	{
		/// <summary>
		/// The current instance of the V12 bot
		/// </summary>
		public static V21 V12Instance { get; private set; }

		static void Main(string[] args)
		{
			var tokenSource = new CancellationTokenSource();
			try
			{
				try
				{
					Console.WriteLine("Starting Bot Task...");
					var task = MainAsync(args); // Task.Factory.StartNew(async () => await MainAsync(args), tokenSource.Token);

					Console.WriteLine("Waiting for end...");
					while (!task.Wait(250, tokenSource.Token))
					{
						if (Console.KeyAvailable)
						{
							var key = Console.ReadKey(true);
							if (key.Key == ConsoleKey.Q) break;
						}
					}

					Console.WriteLine("Terminating Task");
					tokenSource.Cancel();
				}
				catch (AggregateException ag)
				{
					throw ag.InnerException;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Exception: " + e.Message);
				Console.WriteLine(e.StackTrace);
			}
			finally
			{
				tokenSource.Dispose();
				Console.WriteLine("Bot terminated. Press anykey to exit...");
				Console.ReadKey(true);
			}
			

		}


		static async Task MainAsync(string[] args)
		{
			string configFile = "config.json";
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "-config":
						configFile = args[++i];
						break;

				}
			}


			//Load the config
			BotConfig config = new BotConfig();
			if (File.Exists(configFile))
			{
				string json = await File.ReadAllTextAsync(configFile);
				try
				{
					config = JsonConvert.DeserializeObject<BotConfig>(json);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					return;
				}
			}
			else
			{
				//Save the config 
				string json = JsonConvert.SerializeObject(config, Formatting.Indented);
				await File.WriteAllTextAsync(configFile, json);
			}

			//Create the bot
			V12Instance = new V21(config);


			//Instantiate and run the bot
			await V12Instance.Initialize();
			
			//Wait forever
			Console.WriteLine("Connected!");
			await Task.Delay(-1);

			//Dispose of the bot
			await V12Instance.Deinitialize();
			Console.WriteLine("Disconnected!");

			try
			{
				//Save the config again
				string json = JsonConvert.SerializeObject(V12Instance.Config, Formatting.Indented);
				await File.WriteAllTextAsync(configFile, json);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return;
			}
		}
	}
}
