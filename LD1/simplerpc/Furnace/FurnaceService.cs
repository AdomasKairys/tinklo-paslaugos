namespace Servers;

using Services;

/// <summary>
/// Service
/// </summary>
public class FurnaceService : IFurnaceService
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	private readonly FurnaceLogic mLogic = new FurnaceLogic();


	/// <summary>
	/// Get next unique ID from the server. Is used by cars to acquire client ID's.
	/// </summary>
	/// <returns>Unique ID.</returns>
	public int GetUniqueId() 
	{
		return mLogic.GetUniqueId();
	}

	/// <summary>
	/// Get current light state.
	/// </summary>
	/// <returns>Current light state.</returns>				
	public Services.FurnaceState GetFurnaceState()
	{
		return mLogic.GetFurnaceState();
	}

	/// <summary>
	/// Queue give car at the light. Will only succeed if light is red.
	/// </summary>
	/// <param name="client">Car to queue.</param>
	/// <returns>True on success, false on failure.</returns>
	public bool Queue(ClientDesc client) 
	{
		return mLogic.Queue(client);
	}

	/// <summary>
	/// Tell if car is first in line in queue.
	/// </summary>
	/// <param name="carId">ID of the car to check for.</param>
	/// <returns>True if car is first in line. False if not first in line or not in queue.</returns>
	public bool IsFirstInLine(int carId)
	{
		return mLogic.IsFirstInLine(carId);
	}

	/// <summary>
	/// Try passing the traffic light. If car is in queue, it will be removed from it.
	/// </summary>
	/// <param name="client">Car descriptor.</param>
	/// <returns>Pass result descriptor.</returns>
	public CycleAttemptResult FurnacePass(ClientDesc client)
	{
		return mLogic.FurnacePass(client);
	}
}