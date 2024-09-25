namespace Services;


public static class GlassProperties
{
	public const int MELTING_TEMP = 1674; //Kelvin
	public const int SPECIFIC_HEAT_CAPACITY = 670; // J*(kg*K)^-1 

	public const int DEFAULT_TEMP = 298; //Kelvin
}

/// <summary>
/// Client descriptor.
/// </summary>
public class ClientDesc
{
	/// <summary>
	/// Client ID.
	/// </summary>
	public int ClientId { get; set; }

	/// <summary>
	/// Client name and surname.
	/// </summary>
	public string ClientNameSurname { get; set; }

	public ClientType ClientType {get; set;}

	public int GeneratedValue {get; set;}
}

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
	/// Queue give car at the light. Will only succeed if light is red.
	/// </summary>
	/// <param name="client">client to queue.</param>
	/// <returns>True on success, false on failure.</returns>
	bool Queue(ClientDesc client);

	/// <summary>
	/// Tell if car is first in line in queue.
	/// </summary>
	/// <param name="carId">ID of the car to check for.</param>
	/// <returns>True if car is first in line. False if not first in line or not in queue.</returns>
	bool IsFirstInLine(int clientId);

	/// <summary>
	/// Try passing the traffic light. If car is in queue, it will be removed from it.
	/// </summary>
	/// <param name="car">Car descriptor.</param>
	/// <returns>Pass result descriptor.</returns>
	CycleAttemptResult FurnacePass(ClientDesc client);
}
