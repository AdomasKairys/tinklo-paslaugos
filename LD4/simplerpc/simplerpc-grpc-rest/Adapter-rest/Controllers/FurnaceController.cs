namespace Servers;

using Microsoft.AspNetCore.Mvc;

using Services;

/// <summary>
/// Furnace contract
/// </summary>
[Route("/furnace")] [ApiController]
public class FurnaceController : ControllerBase
{
	/// <summary>
	/// Service adapter. This is created in Server.StartServer() and received through DI in constructor.
	/// </summary>
	private readonly FurnaceServiceAdapter mAdapter;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="adapter">adapter to use. This will get passed through DI.</param>
	public FurnaceController(FurnaceServiceAdapter adapter)
	{
		this.mAdapter = adapter;
	}

	/// <summary>
	/// Gets unique id for client
	/// </summary>
	/// <returns>unique client id</returns>
	[HttpGet("/getUniqueId")]
	public ActionResult<int> GetUniqueId() 
	{
		return mAdapter.GetUniqueId();
	}
	/// <summary>
	/// Gets current furnace state
	/// </summary>
	/// <returns>Furnace state</returns>
	[HttpGet("/getFurnaceState")]
	public ActionResult<State> GetFurnaceState()
	{
		return mAdapter.GetFurnaceState();
	}
	/// <summary>
	/// Main function that melts glass (increase heat or load glass)
	/// </summary>
	/// <param name="client">client information (heater or loader)</param>
	/// <returns>Result of success or failure</returns>
	[HttpPost("/meltGlass")]
	public ActionResult<CycleAttemptResult> MeltGlass(ClientDesc client)
	{
		return mAdapter.MeltGlass(client);
	}
}