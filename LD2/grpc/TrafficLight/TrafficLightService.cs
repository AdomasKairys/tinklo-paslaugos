namespace Servers;

using Grpc.Core;

using Services;


/// <summary>
/// Service
/// </summary>
public class TrafficLightService : Services.TrafficLight.TrafficLightBase
{
	//NOTE: instance-per-request service would need logic to be static or injected from a singleton instance
	private readonly TrafficLightLogic mLogic = new TrafficLightLogic();


	/// <summary>
	/// Get next unique ID from the server. Is used by cars to acquire client ID's.
	/// </summary>
	/// <param name="input">Not used.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Unique ID.</returns>
	public override Task<IntMsg> GetUniqueId(Empty input, ServerCallContext context)
	{
		var result = new IntMsg { Value = mLogic.GetUniqueId() };
		return Task.FromResult(result);
	}

	/// <summary>
	/// Get current light state.
	/// </summary>
	/// <param name="input">Not used.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Current light state.</returns>				
	public override Task<GetLightStateOutput> GetLightState(Empty input, ServerCallContext context)
	{
		var logicLightState = mLogic.GetLightState();
		var serviceLightState = (Services.LightState)logicLightState; //this will only work properly if enumerations are by-value compatible

		var result = new GetLightStateOutput { Value = serviceLightState };
		return Task.FromResult(result);
	}

	/// <summary>
	/// Queue give car at the light. Will only succeed if light is red.
	/// </summary>
	/// <param name="input">Car to queue.</param>
	/// <param name="context">Call context.</param>
	/// <returns>True on success, false on failure.</returns>
	public override Task<BoolMsg> Queue(Services.CarDesc input, ServerCallContext context)
	{
		//convert input to the format expected by logic
		var car = 
			new CarDesc { 
				CarId = input.CarId,
				CarNumber = input.CarNumber,
				DriverNameSurname = input.DriverNameSurname
			};

		//
		var logicResult = mLogic.Queue(car);

		//convert result to the format expected by gRPC
		var result = new BoolMsg { Value = logicResult };

		//
		return Task.FromResult(result);
	}

	/// <summary>
	/// Tell if car is first in line in queue.
	/// </summary>
	/// <param name="input">ID of the car to check for.</param>
	/// <param name="context">Call context.</param>
	/// <returns>True if car is first in line. False if not first in line or not in queue.</returns>
	public override Task<BoolMsg> IsFirstInLine(IntMsg input, ServerCallContext context)
	{
		var result = new BoolMsg { Value = mLogic.IsFirstInLine(input.Value) };
		return Task.FromResult(result);
	}

	/// <summary>
	/// Try passing the traffic light. If car is in queue, it will be removed from it.
	/// </summary>
	/// <param name="input">Car descriptor.</param>
	/// <param name="context">Call context.</param>
	/// <returns>Pass result descriptor.</returns>
	public override Task<Services.PassAttemptResult> Pass(Services.CarDesc input, ServerCallContext context)
	{
		//convert input to the format expected by logic
		var car = 
			new CarDesc { 
				CarId = input.CarId,
				CarNumber = input.CarNumber,
				DriverNameSurname = input.DriverNameSurname
			};

		//
		var logicResult = mLogic.Pass(car);

		//convert result to the format expected by gRPC
		var result = 
			new Services.PassAttemptResult {
				IsSuccess = logicResult.IsSuccess,
				CrashReason = logicResult.CrashReason ?? "" //convert null to empty string, because gRPC can't handle null values
			};

		//
		return Task.FromResult(result);
	}
}