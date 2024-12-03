namespace Servers;

using Grpc.Net.Client;

using Services;
using ServicesGRPC;

/// <summary>
/// Furnace contract
/// </summary>
public class FurnaceService : IFurnaceService
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	private readonly Furnace.FurnaceClient? furnace;

	public FurnaceService(){
		try{
			GrpcChannel channel = GrpcChannel.ForAddress("http://127.0.0.1:5000");
			furnace = new Furnace.FurnaceClient(channel);
		}
		catch(Exception){
			//TODO: handle exeptions
		}
	}

	/// <summary>
	/// Gets unique id for client
	/// </summary>
	/// <returns>unique client id</returns>
	public int GetUniqueId()
	{
		if(furnace == null)
			throw new Exception();
		
		return furnace.GetUniqueId(new Empty()).Value;
	}
	/// <summary>
	/// Gets current furnace state
	/// </summary>
	/// <returns>Furnace state</returns>
	public Services.FurnaceState GetFurnaceState()
	{
		if(furnace == null)
			throw new Exception();

		return (Services.FurnaceState)furnace.GetFurnaceState(new Empty()).Value;
	}
	/// <summary>
	/// Main function that melts glass (increase heat or load glass)
	/// </summary>
	/// <param name="client">client information (heater or loader)</param>
	/// <returns>Result of success or failure</returns>
	public Services.CycleAttemptResult MeltingGlass(Services.ClientDesc client)
	{
		if(furnace == null)
			throw new Exception();
		ServicesGRPC.ClientDesc clientDesc = new ServicesGRPC.ClientDesc() {
			ClientId = client.ClientId,
			ClientType = (ServicesGRPC.ClientType) client.ClientType, 
			GeneratedValue= client.GeneratedValue};
		ServicesGRPC.CycleAttemptResult cycleAttemptResultGRPC = furnace.MeltGlass(clientDesc);
		Services.CycleAttemptResult cycleAttemptResultSRPC = new Services.CycleAttemptResult() {
			IsSuccess = cycleAttemptResultGRPC.IsSuccess,
		 	FailReason = cycleAttemptResultGRPC.FailReason};

		return  cycleAttemptResultSRPC;
	}

    FurnaceState IFurnaceService.GetFurnaceState()
    {
        throw new NotImplementedException();
    }

    public CycleAttemptResult MeltingGlass(ClientDesc client)
    {
        throw new NotImplementedException();
    }
}