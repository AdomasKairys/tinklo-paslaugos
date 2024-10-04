namespace Clients;

using Microsoft.Extensions.DependencyInjection;

using SimpleRpc.Serialization.Hyperion;
using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Client;

using NLog;

using Services;


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
				//connect to the server, get service client proxy
				var sc = new ServiceCollection();
				sc
					.AddSimpleRpcClient(
						"furnaceService", //must be same as on line 86
						new HttpClientTransportOptions
						{
							Url = "http://127.0.0.1:5000/simplerpc",
							Serializer = "HyperionMessageSerializer"
						}
					)
					.AddSimpleRpcHyperionSerializer();

				sc.AddSimpleRpcProxy<IFurnaceService>("furnaceService"); //must be same as on line 77
				
				var sp = sc.BuildServiceProvider();

				var furnace = sp.GetService<IFurnaceService>();

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
					var isWaiting = false;
					var isHeating = false;

					mLog.Info("I prepare to load glass into furnace.");

					//try increasing the heat
					while( !isWaiting && !isHeating )
					{
						//read the furnace state
						var furnaceState = furnace.GetFurnaceState();

						//give some time for furnace to possibly switch, before taking action
						Thread.Sleep(rnd.Next(500)); 

						if( furnaceState == FurnaceState.Melting )
						{
							//try passing 
							mLog.Info("Furnace is working, trying to load glass.");							
							var par = furnace.MeltingGlass(client);

							//handle result
							if( par.IsSuccess )
							{
								mLog.Info("Loaded glass, life is good.");		
								isHeating = true;					
							}
							else
							{
								mLog.Info($"Failed because '{par.FailReason}'.");
								isWaiting = true;
							}
						}
						//pouring out glass, queue until finished pouring
						else
						{
							while( !isWaiting && !isHeating )
							{
								furnaceState = furnace.GetFurnaceState();

								Thread.Sleep(rnd.Next(500)); 

								if( furnaceState == FurnaceState.Melting )
								{
									mLog.Info("Furnace is melting trying to load glass. ");
									var par = furnace.MeltingGlass(client);

									if( par.IsSuccess )
									{
										mLog.Info("Loaded glass, life is good.");		
										isHeating = true;					
									}
									else
									{
										mLog.Info($"Failed because '{par.FailReason}'.");
										isWaiting = true;
									}
								}
								else
								{
									mLog.Info("Waiting some more.");
									Thread.Sleep(500 + rnd.Next(2500));
								}
							}
						}
					}

					if( isWaiting )
					{
						mLog.Info("Meditating on my mistakes...");
						Thread.Sleep(500 + rnd.Next(1500));
						mLog.Info("It is a new day.");
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
