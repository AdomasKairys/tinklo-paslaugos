extern alias simplerpc;

namespace Servers;

using SimpleRpc.Serialization.Hyperion;
using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Client;
using Grpc.Core;
using NLog;

using Services;
using SimpleRpc = simplerpc::Services;



/// <summary>
/// Simple RPC Furnace contract
/// </summary>
public class FurnaceServiceAdapter : Services.Furnace.FurnaceBase
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	
	private readonly SimpleRpc.IFurnaceService? furnace;
    private readonly Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Constructor for simple rpc furnace service adapter, connects to gRPC server 
	/// </summary>
	public FurnaceServiceAdapter(){
		while(true){
			try{
				mLog.Info("Connecting to simple RPC server");
				var sc = new ServiceCollection();
				sc
					.AddSimpleRpcClient(
						"furnaceService", //must be same as on line 86
						new HttpClientTransportOptions
						{
							Url = "http://127.0.0.1:5000/simplerpc",
							Serializer = "HyperionMessageSerializer"
						}
					)
					.AddSimpleRpcHyperionSerializer();

				sc.AddSimpleRpcProxy<SimpleRpc.IFurnaceService>("furnaceService"); //must be same as on line 77

				var sp = sc.BuildServiceProvider();

				furnace = sp.GetService<SimpleRpc.IFurnaceService>();
				furnace!.GetFurnaceState();
				break;
			}
			catch(Exception e){
				mLog.Warn(e, "Failed to connect to simple RPC server, retrying");
				Thread.Sleep(2000);
			}
		}
		mLog.Info("Connected succesfully");
	}
	/// <summary>
	/// Get next unique ID from the server. Is used by cars to acquire client ID's.
	/// </summary>
	/// <param name="input">Not used.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Unique ID.</returns>
	public override Task<IntMsg> GetUniqueId(Empty input, ServerCallContext context)
	{
		var result = new IntMsg { Value = furnace!.GetUniqueId() };
		return Task.FromResult(result);
	}

	/// <summary>
	/// Get current light state.
	/// </summary>
	/// <param name="input">Not used.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Current light state.</returns>				
	public override Task<GetFurnaceStateOutput> GetFurnaceState(Empty input, ServerCallContext context)
	{
		var logicFurnaceState = furnace!.GetFurnaceState();
		var serviceFurnaceState = (Services.FurnaceState)logicFurnaceState; //this will only work properly if enumerations are by-value compatible

		var result = new GetFurnaceStateOutput { Value = serviceFurnaceState };
		return Task.FromResult(result);
	}

	/// <summary>
	/// Try melting glass
	/// </summary>
	/// <param name="input">Client descriptor.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Attemp result descriptor.</returns>
	public override Task<Services.CycleAttemptResult> MeltGlass(Services.ClientDesc input, ServerCallContext context)
	{
		//convert input to the format expected by logic
		var client = 
			new SimpleRpc.ClientDesc { 
				ClientId = input.ClientId,
				ClientType = (SimpleRpc.ClientType)input.ClientType,
				GeneratedValue = input.GeneratedValue
			};

		//
		var logicResult = furnace!.MeltGlass(client);

		//convert result to the format expected by gRPC
		var result = 
			new Services.CycleAttemptResult {
				IsSuccess = logicResult.IsSuccess,
				FailReason = logicResult.FailReason ?? "" //convert null to empty string, because gRPC can't handle null values
			};

		//
		return Task.FromResult(result);
	}

}
