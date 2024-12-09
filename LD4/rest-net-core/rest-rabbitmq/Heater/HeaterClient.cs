namespace Clients;

using System.Net.Http;

using NLog;

using Services;

/// <summary>
/// Heater client class
/// </summary>
class HeaterClient
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
				//connect to the server, get service client proxy

				var furnace = new FurnaceClient("http://127.0.0.1:5001", new HttpClient());

				//initialize client descriptor
				var client = new ClientDesc(){
					ClientId = furnace.GetUniqueId(),
					GeneratedValue = rnd.Next(500000, 8000000),
					ClientType = ClientType.Heater };

				//log identity data
				string s = $"I am a heater {client.ClientId}, I incresed the heat by {client.GeneratedValue} J.";
				mLog.Info(s);
				Console.Title = s;
					
				//heating
				while( true )
				{
					var isHeating = false;

					client.GeneratedValue = rnd.Next(500000, 8000000);
					mLog.Info($"I prepare to increase heat by {client.GeneratedValue} J.");
					//try increasing the heat
					while( !isHeating )
					{
						//read the furnace state
						var furnaceState = furnace.GetFurnaceState();

						//give some time for furnace to possibly switch, before taking action
						Thread.Sleep(500); 

						if( furnaceState == State.Melting )
						{
							//try passing 
							mLog.Info("Furnace is working, trying to increase heat.");							
							var par = furnace.MeltGlass(client);

							//handle result
							if( par.IsSuccess )
							{
								mLog.Info("Increased heat, life is good.");		
								isHeating = true;					
							}
							else
							{
								mLog.Info($"Failed because '{par.FailReason}'.");
							}
						}
						//pouring out glass, wait until finished pouring
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
		var self = new HeaterClient();
		self.Run();
	}
}
