namespace Servers;

using Grpc.Net.Client;
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
	
	private readonly Furnace.FurnaceClient? furnace;
    private readonly Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Constructor for simple rpc furnace service adapter, connects to gRPC server 
	/// </summary>
	public FurnaceServiceAdapterLogic(){
		while(true){
			try{
				mLog.Info("Connecting to gRPC server");
				GrpcChannel channel = GrpcChannel.ForAddress("http://127.0.0.1:5000");
				furnace = new Furnace.FurnaceClient(channel);
				furnace.GetFurnaceState(new Empty()); // arbitrary call to check connection
				break;
			}
			catch(Exception e){
				mLog.Warn(e, "Failed to connect to gRPC server, retrying");
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
		return furnace!.GetUniqueId(new Empty()).Value;
	}

	/// <summary>
	/// Get current light state.
	/// </summary>
	/// <param name="input">Not used.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Current light state.</returns>				
	public State GetFurnaceState()
	{
		return (State)furnace!.GetFurnaceState(new Empty()).Value;
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
