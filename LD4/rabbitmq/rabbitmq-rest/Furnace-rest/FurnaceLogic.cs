namespace Servers;

using NLog;


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
public enum State : int
{
	Melting,
	Pouring
}

/// <summary>
/// Cunstants for glass and furnace
/// </summary>
public static class Constants
{
	/// <summary>
	/// Glass constants
	/// </summary>
	public static class GlassProperties
	{
		public const int MELTING_TEMP = 1674; //Kelvin
		public const int SPECIFIC_HEAT_CAPACITY = 670; // J*(kg*K)^-1 
		public const int DEFAULT_TEMP = 298; //Kelvin
	}
	/// <summary>
	/// Furnace constants
	/// </summary>
	public static class FurnaceProperties
	{		
		public const int FURNACE_CAPACITY = 200; //kg
	}
}
/// <summary>
/// Furnace variables
/// </summary>
public class FurnaceState
{
	
	public readonly object AccessLock = new object();

	public int LastUniqueId;

	public State FurncState;

	public int GlassMass = 0; //kilograms

	public double GlassTemperature = 0; //kelvin

}

/// <summary>
/// Furnace logic
/// </summary>
public class FurnaceLogic
{
	private Logger mLog = LogManager.GetCurrentClassLogger();

	private Thread mBgTaskThread;

	private FurnaceState mState = new FurnaceState();
	
	public FurnaceLogic()
	{
		//start the background task
		mBgTaskThread = new Thread(BackgroundTask);
		mBgTaskThread.Start();
	}
	/// <summary>
	/// Generates unique id for client
	/// </summary>
	/// <returns>unique id</returns>
	public int GetUniqueId() 
	{
		lock( mState.AccessLock )
		{
			mState.LastUniqueId += 1;
			return mState.LastUniqueId;
		}
	}
	/// <summary>
	/// Gets furnace state
	/// </summary>
	/// <returns>furnace state</returns>
	public State GetFurnaceState() 
	{
		lock( mState.AccessLock )
		{
			return mState.FurncState;
		}
	}
	/// <summary>
	/// Function to melt glass inside the furnace (add head or glass)
	/// </summary>
	/// <param name="client">client data</param>
	/// <returns>result success or fail</returns>
	public CycleAttemptResult MeltGlass(ClientDesc client)
	{
		//prepare result descriptor
		var par = new CycleAttemptResult();

		lock( mState.AccessLock )
		{
			mLog.Info($"{client.ClientType} {client.ClientId}, is trying to do work.");

			if(mState.FurncState == State.Pouring)
			{
				par.IsSuccess = false;
				par.FailReason = "furnace is pouring";
				
			}
			else
			{
				par.IsSuccess = true;

				if(client.ClientType == ClientType.Loader)
				{
					int newMass = client.GeneratedValue + mState.GlassMass;
					if(newMass <= Constants.FurnaceProperties.FURNACE_CAPACITY)
					{
						//applied formula Q=cm(t2-t1)
						mState.GlassTemperature = (Constants.GlassProperties.DEFAULT_TEMP*client.GeneratedValue + mState.GlassTemperature*mState.GlassMass)/newMass;
						mState.GlassMass = newMass;
					}
					else
					{
						par.IsSuccess = false;
						par.FailReason = "furnace is full";
					}
				}
				//if there is glass inside the furnace, increase temp
				else if(mState.GlassMass > 0)
				{
					mState.GlassTemperature += client.GeneratedValue/(mState.GlassMass*Constants.GlassProperties.SPECIFIC_HEAT_CAPACITY); 
				}
				else
				{
					par.IsSuccess = false;
					par.FailReason = "no glass in the furnace";
				}
			}

			//log result
			if( par.IsSuccess )
			{
				mLog.Info( $"{client.ClientType} has succesfully " +
				$"{(client.ClientType == ClientType.Heater ? $"increased the heat by {client.GeneratedValue/(mState.GlassMass*Constants.GlassProperties.SPECIFIC_HEAT_CAPACITY)} " 
				: $"added {client.GeneratedValue} ")}" +
				$" {(client.ClientType == ClientType.Heater ? " K" : " kg of glass")}.");
			}
			else
			{
				mLog.Info($"{client.ClientType} has failed because '{par.FailReason}'.");
			}

			//
			return par;
		}
	}
	/// <summary>
	/// Inner furnace cycle that checks if the glass is melted and changes state
	/// </summary>
	public void BackgroundTask()
	{
		//intialize random number generator
		var rnd = new Random();
		int cycleCounter = 0;
		//
		while( true )
		{
			//sleep a while
			Thread.Sleep(500 + rnd.Next(1500));
			
			lock( mState.AccessLock )
			{
				if(mState.GlassTemperature >= Constants.GlassProperties.MELTING_TEMP) //check if is melting
					cycleCounter++;
				else
					cycleCounter = 0;
				if(cycleCounter == 3){ //after 3 melting cycles, pour
					mState.FurncState = State.Pouring;
					mLog.Info($"Furnace is pouring molten glass, ammount {mState.GlassMass}.");
					mState.GlassMass = 0;
					mState.GlassTemperature = 0;
					cycleCounter = 0;
				}
				else
				{
					mLog.Info($"Furnace is currently melting glass, current glass temperature {mState.GlassTemperature}, ammount of glass in the furnace {mState.GlassMass}");
				}
			}
			if(mState.FurncState == State.Pouring)
			{
				int num = 0;
				while (true)
				{
					//pours for five seconds
					num = (num % 5) + 1; 
					mLog.Info($"{num}...");
					// wait for 1 second before printing the next value

					Thread.Sleep(1000); 
					if (num == 5)
						break;//exit while loop
				}
			}
			lock(mState.AccessLock)
			{
				mState.FurncState = State.Melting;
			}
		}
	}
}