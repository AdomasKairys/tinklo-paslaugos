extern alias simplerpc;

namespace Servers;

using NLog;

using Services;
using SimpleRpc = simplerpc::Services;

/// <summary>
/// Simple RPC Furnace contract
/// </summary>
public class FurnaceServiceAdapter : SimpleRpc.IFurnaceService
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	private readonly FurnaceClient? furnace;
    private readonly Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Constructor for simple rpc furnace service adapter, connects to REST server 
	/// </summary>
	public FurnaceServiceAdapter(){
		while(true){
			try{
				furnace = new FurnaceClient();
				furnace.GetFurnaceState(); // arbitrary call to check connection
				break;
			}
			catch(Exception e){
				mLog.Warn(e, "Failed to connect to REST server, retrying");
				Thread.Sleep(2000);
			}
		}
		mLog.Info("Connected succesfully");
	}

	/// <summary>
	/// Gets unique id for client
	/// </summary>
	/// <returns>unique client id</returns>
	public int GetUniqueId()
	{
		return furnace!.GetUniqueId();
	}
	/// <summary>
	/// Gets current furnace state
	/// </summary>
	/// <returns>Furnace state</returns>
	public SimpleRpc.FurnaceState GetFurnaceState()
	{
		return (SimpleRpc.FurnaceState)furnace!.GetFurnaceState();
	}
	/// <summary>
	/// Main function that melts glass (increase heat or load glass)
	/// </summary>
	/// <param name="client">client information (heater or loader)</param>
	/// <returns>Result of success or failure</returns>
	public SimpleRpc.CycleAttemptResult MeltingGlass(SimpleRpc.ClientDesc client)
	{
		ClientDesc clientDesc = new ClientDesc() {
			ClientId = client.ClientId,
			ClientType = (ClientType) client.ClientType, 
			GeneratedValue= client.GeneratedValue};
		CycleAttemptResult cycleAttemptResultRabbitMQ = furnace!.MeltGlass(clientDesc);
		SimpleRpc.CycleAttemptResult cycleAttemptResultSRPC = new SimpleRpc.CycleAttemptResult() {
			IsSuccess = cycleAttemptResultRabbitMQ.IsSuccess,
		 	FailReason = cycleAttemptResultRabbitMQ.FailReason};
		return  cycleAttemptResultSRPC;
	}
}