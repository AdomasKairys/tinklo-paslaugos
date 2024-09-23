namespace Clients;

using Microsoft.Extensions.DependencyInjection;

using SimpleRpc.Serialization.Hyperion;
using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Client;

using NLog;

using Services;


/// <summary>
/// Client example.
/// </summary>
class HeaterClient
{
	/// <summary>
	/// A set of names to choose from.
	/// </summary>
	private readonly List<string> NAMES = 
		new List<string> { 
			"John", "Peter", "Jack", "Steve"
		};

	/// <summary>
	/// A set of surnames to choose from.
	/// </summary>
	private readonly List<string> SURNAMES = 
		new List<String> { 
			"Johnson", "Peterson", "Jackson", "Steveson" 
		};


	/// <summary>
	/// Logger for this class.
	/// </summary>
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

				sc.AddSimpleRpcProxy<IFurnaceService>("funrcaveService"); //must be same as on line 77

				var sp = sc.BuildServiceProvider();

				var furnace = sp.GetService<IFurnaceService>();

				//initialize client descriptor
				var client = new ClientDesc();

				client.ClientNameSurname =
					NAMES[rnd.Next(NAMES.Count)] + 
					" " +
					SURNAMES[rnd.Next(SURNAMES.Count)];

				//get unique client id
				client.ClientId = furnace.GetUniqueId();

				//log identity data
				string s = $"I am a heater {client.ClientId}, Operator {client.ClientNameSurname}.";
				mLog.Info(s);
				Console.Title = s;
					
				//heating
				while( true )
				{
					var isWaiting = false;
					var isHeating = false;

					mLog.Info("I prepare to increase heat.");

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
							mLog.Info("Furnace is working, trying to increase heat.");							
							var par = furnace.Pass(client);

							//handle result
							if( par.IsSuccess )
							{
								mLog.Info("Increased heat, life is good.");		
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
							mLog.Info("Furnace is pouring, trying to enter queue.");							
							var inQueue = furnace.Queue(client);

							//success? wait for light and queue
							if( inQueue )
							{
								mLog.Info("I'm in queue now. Waiting for light.");

								while( !isWaiting && !isHeating )
								{
									//determine state of light and queue
									furnaceState = furnace.GetFurnaceState();
									var firstInLine = furnace.IsFirstInLine(client.ClientId);

									//give some time for light to possibly switch, before taking action
									Thread.Sleep(rnd.Next(500)); 

									//can pass? try it
									if( furnaceState == FurnaceState.Melting && firstInLine )
									{
										//try passing
										mLog.Info("Light is green and I an ready, trying to pass");
										var par = furnace.Pass(client);

										//handle result
										if( par.IsSuccess )
										{
											mLog.Info("Increased heat, life is good.");		
											isHeating = true;					
										}
										else
										{
											mLog.Info($"Failed because '{par.FailReason}'.");
											isWaiting = true;
										}
									}
									//no passing yet, wait
									else
									{
										mLog.Info("Waiting some more.");
										Thread.Sleep(500 + rnd.Next(1500));
									}
								}
							}
							//could not queue (maybe light has changed)
							else
							{
								mLog.Info("Queuing failed. Will check the light again.");
							}
						}
					}

					//managed to crash? reflect on it
					if( isWaiting )
					{
						mLog.Info("Meditating on my mistakes...");
						Thread.Sleep(500 + rnd.Next(1500));
						mLog.Info("It is a new day and a new car.");
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
