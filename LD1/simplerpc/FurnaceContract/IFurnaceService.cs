namespace Services;

/// <summary>
/// Client descriptor.
/// </summary>
public class ClientDesc
{
	/// <summary>
	/// Client ID.
	/// </summary>
	public int ClientId { get; set; }

	public ClientType ClientType {get; set;}

	public int GeneratedValue {get; set;}
}

/// <summary>
/// Client types
/// </summary>
public enum ClientType : int
{
	Heater,
	Loader
}

/// <summary>
/// Descriptor of loading and heating cycle atempt result
/// </summary>
public class CycleAttemptResult
{
	/// <summary>
	/// Indicates if loading/heating attempt has succeeded.
	/// </summary>
	public bool IsSuccess { get; set; }

	/// <summary>
	/// If loading/heating attempt has failed, indicates reason.
	/// </summary>
	public string FailReason { get; set; }
}


/// <summary>
/// Furnace state.
/// </summary>
public enum FurnaceState : int
{
	Melting,
	Pouring
}


/// <summary>
/// Service contract.
/// </summary>
public interface IFurnaceService
{
	/// <summary>
	/// Get next unique ID from the server. Is used by loaders and heaters to acquire client ID's.
	/// </summary>
	/// <returns>Unique ID.</returns>
	int GetUniqueId();

	/// <summary>
	/// Get current furnace state.
	/// </summary>
	/// <returns>Current furnace state.</returns>				
	FurnaceState GetFurnaceState();

	/// <summary>
	/// Try passing the traffic light. If car is in queue, it will be removed from it.
	/// </summary>
	/// <param name="car">Car descriptor.</param>
	/// <returns>Pass result descriptor.</returns>
	CycleAttemptResult MeltingGlass(ClientDesc client);
}
