extern alias srabbitmq;
namespace Servers;

using NLog;

using SRabbitMQ = srabbitmq::Services;

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
	
	private readonly Clients.FurnaceClient? furnace;
    private readonly Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Constructor for simple rpc furnace service adapter, connects to gRPC server 
	/// </summary>
	public FurnaceServiceAdapterLogic(){
		while(true){
			try{
				mLog.Info("Connecting to rabbitmq server");
				furnace = new Clients.FurnaceClient();
				furnace.GetFurnaceState(); // arbitrary call to check connection
				break;
			}
			catch(Exception e){
				mLog.Warn(e, "Failed to connect to rabbitmq server, retrying");
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
	public SRabbitMQ.CycleAttemptResult MeltGlass(SRabbitMQ.ClientDesc input)
	{
		return furnace!.MeltGlass(input);
	}

}
