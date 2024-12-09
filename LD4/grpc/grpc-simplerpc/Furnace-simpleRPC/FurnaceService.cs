namespace Servers;

using Services;

/// <summary>
/// Furnace contract
/// </summary>
public class FurnaceService : IFurnaceService
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	private readonly FurnaceLogic mLogic = new FurnaceLogic();

	/// <summary>
	/// Gets unique id for client
	/// </summary>
	/// <returns>unique client id</returns>
	public int GetUniqueId() 
	{
		return mLogic.GetUniqueId();
	}
	/// <summary>
	/// Gets current furnace state
	/// </summary>
	/// <returns>Furnace state</returns>
	public Services.FurnaceState GetFurnaceState()
	{
		return mLogic.GetFurnaceState();
	}
	/// <summary>
	/// Main function that melts glass (increase heat or load glass)
	/// </summary>
	/// <param name="client">client information (heater or loader)</param>
	/// <returns>Result of success or failure</returns>
	public CycleAttemptResult MeltingGlass(ClientDesc client)
	{
		return mLogic.MeltingGlass(client);
	}
}