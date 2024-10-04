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

	public CycleAttemptResult MeltingGlass(ClientDesc client)
	{
		return mLogic.MeltingGlass(client);
	}
}