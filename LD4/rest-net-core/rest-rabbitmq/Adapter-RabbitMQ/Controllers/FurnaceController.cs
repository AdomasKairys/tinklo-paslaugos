extern alias srabbitmq;
namespace Servers;

using Microsoft.AspNetCore.Mvc;

using SRabbitMQ = srabbitmq::Services;


/// <summary>
/// Furnace contract
/// </summary>
[Route("/furnace")] [ApiController]
public class FurnaceController : ControllerBase
{
	/// <summary>
	/// Service logic. This is created in Server.StartServer() and received through DI in constructor.
	/// </summary>
	private readonly FurnaceServiceAdapterLogic mLogic;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="logic">Logic to use. This will get passed through DI.</param>
	public FurnaceController(FurnaceServiceAdapterLogic logic)
	{
		this.mLogic = logic;
	}

	/// <summary>
	/// Gets unique id for client
	/// </summary>
	/// <returns>unique client id</returns>
	[HttpGet("/getUniqueId")]
	public ActionResult<int> GetUniqueId() 
	{
		return mLogic.GetUniqueId();
	}
	/// <summary>
	/// Gets current furnace state
	/// </summary>
	/// <returns>Furnace state</returns>
	[HttpGet("/getFurnaceState")]
	public ActionResult<State> GetFurnaceState()
	{
		return mLogic.GetFurnaceState();
	}
	/// <summary>
	/// Main function that melts glass (increase heat or load glass)
	/// </summary>
	/// <param name="client">client information (heater or loader)</param>
	/// <returns>Result of success or failure</returns>
	[HttpPost("/meltGlass")]
	public ActionResult<SRabbitMQ.CycleAttemptResult> MeltGlass(SRabbitMQ.ClientDesc client)
	{
		return mLogic.MeltGlass(client);
	}
}