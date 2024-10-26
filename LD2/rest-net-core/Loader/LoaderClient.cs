namespace Clients;

using System.Net.Http;

using NLog;

using Services;

/// <summary>
/// Loader client class
/// </summary>
class LoaderClient
{

	Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Configures logging subsystem.
	/// </summary>
	private void ConfigureLogging()
	{
		var config = new NLog.Config.LoggingConfiguration();

		var console =
			new NLog.Targets.ConsoleTarget("console")
			{
				Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
			};
		config.AddTarget(console);
		config.AddRuleForAllLevels(console);

		LogManager.Configuration = config;
	}
	/// <summary>
	/// Program body.
	/// </summary>
	private void Run() {
		//configure logging
		ConfigureLogging();

		//initialize random number generator
		var rnd = new Random();

		//run everythin in a loop to recover from connection errors
		while( true )
		{
			try {
				var furnace = new FurnaceClient("http://127.0.0.1:5000", new HttpClient());

				//initialize client descriptor
				var client = new ClientDesc(){
					ClientId = furnace.GetUniqueId(),
					GeneratedValue = rnd.Next(1, 5),
					ClientType = ClientType.Loader };

				//log identity data
				string s = $"I am a loader {client.ClientId}, I put {client.GeneratedValue} kg of glass.";
				mLog.Info(s);
				Console.Title = s;
					
				//loading
				while( true )
				{
					var isLoading = false;

					client.GeneratedValue = rnd.Next(1,5);
					mLog.Info($"I prepare to load {client.GeneratedValue} kg of glass into furnace.");
					//try increasing the heat
					while( !isLoading )
					{
						//read the furnace state
						var furnaceState = furnace.GetFurnaceState();

						//give some time for furnace to possibly switch, before taking action
						Thread.Sleep(500); 

						if( furnaceState == State.Melting )
						{
							//try passing 
							mLog.Info("Furnace is working, trying to load glass.");							
							var par = furnace.MeltGlass(client);

							//handle result
							if( par.IsSuccess )
							{
								mLog.Info("Loaded glass, life is good.");		
								isLoading = true;					
							}
							else
							{
								mLog.Info($"Failed because '{par.FailReason}'.");
							}
						}
						//pouring out glass, wait until winished
						else
						{
							mLog.Info($"Furnace is pouring, waiting.");
							Thread.Sleep(1500);
						}
					}
				}				
			}
			catch( Exception e )
			{
				//log whatever exception to console
				mLog.Warn(e, "Unhandled exception caught. Will restart main loop.");

				//prevent console spamming
				Thread.Sleep(2000);
			}
		}
	}

	/// <summary>
	/// Program entry point.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	static void Main(string[] args)
	{
		var self = new LoaderClient();
		self.Run();
	}
}
