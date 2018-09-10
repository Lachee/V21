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
			string token = "";
			string config = "configuration.json";
			string resources = "Resources/";
			bool windows7 = false;

			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "-token":
						token = args[++i];
						break;

					case "-tokenfile":

						//Get the file and make sure it exists
						string file = args[++i];
						if (!File.Exists(file))
						{
							Console.WriteLine("Token File '{0}' does not exist!", file);
							return;
						}

						//Load the token
						token = await File.ReadAllTextAsync(file);
						break;

					case "-config":
						config = args[++i];
						break;

					case "-resources":
						resources = args[++i];
						break;

					case "-win7":
						windows7 = true;
						break;

					default:
						Console.WriteLine("Unknown Command: {0}", args[i]);
						Console.WriteLine("Available Commands: -token, -tokenfile, -config, -win7");
						break;

				}
			}

			//Make sure the key is valid
			if (string.IsNullOrEmpty(token))
			{
				Console.WriteLine("Invalid key! Please use -key or -keyfile!");
				return;
			}

			//Create the bot
			V12Instance = new V21(token, null, windows7)
			{
				Resources = resources
			};

			//Instantiate and run the bot
			await V12Instance.Initialize();
			
			//Wait forever
			Console.WriteLine("Connected!");
			await Task.Delay(-1);

			//Dispose of the bot
			await V12Instance.Deinitialize();
			Console.WriteLine("Disconnected!");
		}
	}
}
