namespace Servers;

using Services;

public class FurnaceService : IFurnaceService
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	private readonly FurnaceLogic mLogic = new FurnaceLogic();


	public int GetUniqueId() 
	{
		return mLogic.GetUniqueId();
	}
		
	public Services.FurnaceState GetFurnaceState()
	{
		return mLogic.GetFurnaceState();
	}


	public bool Queue(ClientDesc client) 
	{
		return mLogic.Queue(client);
	}

	public bool IsFirstInLine(int clientId, ClientType clientType)
	{
		return mLogic.IsFirstInLine(clientId, clientType);
	}

	public CycleAttemptResult FurnacePass(ClientDesc client)
	{
		return mLogic.FurnacePass(client);
	}
}