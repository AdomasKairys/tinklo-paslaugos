namespace Servers;

using SimpleRpc.Serialization.Hyperion;
using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Client;
using NLog;

using Services;

/// <summary>
/// Furnace state.
/// </summary>
public enum State : int
{
	Melting,
	Pouring
}

/// <summary>
/// Simple RPC Furnace contract
/// </summary>
public class FurnaceServiceAdapterLogic
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	
	private readonly IFurnaceService? furnace;
    private readonly Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Constructor for simple rpc furnace service adapter, connects to gRPC server 
	/// </summary>
	public FurnaceServiceAdapterLogic(){
		while(true){
			try{
				mLog.Info("Connecting to simple RPC server");
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

				furnace = sp.GetService<IFurnaceService>();
				furnace!.GetFurnaceState();
				break;
			}
			catch(Exception e){
				mLog.Warn(e, "Failed to connect to simple RPC server, retrying");
				Thread.Sleep(2000);
			}
		}
		mLog.Info("Connected succesfully");
	}
	/// <summary>
	/// Get next unique ID from the server. Is used by cars to acquire client ID's.
	/// </summary>
	/// <param name="input">Not used.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Unique ID.</returns>
	public int GetUniqueId()
	{
		return furnace!.GetUniqueId();
	}

	/// <summary>
	/// Get current light state.
	/// </summary>
	/// <param name="input">Not used.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Current light state.</returns>				
	public State GetFurnaceState()
	{
		return (State)furnace!.GetFurnaceState();
	}

	/// <summary>
	/// Try melting glass
	/// </summary>
	/// <param name="input">Client descriptor.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Attemp result descriptor.</returns>
	public CycleAttemptResult MeltGlass(ClientDesc input)
	{
		return furnace!.MeltGlass(input);
	}

}
