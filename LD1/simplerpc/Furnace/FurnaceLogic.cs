namespace Servers;

using NLog;

using Services;

public static class Constants
{
	public static class GlassProperties
	{
		public const int MELTING_TEMP = 1674; //Kelvin
		public const int SPECIFIC_HEAT_CAPACITY = 670; // J*(kg*K)^-1 
		public const int DEFAULT_TEMP = 298; //Kelvin
	}
	public static class FurnaceProperties
	{		
		public const int FURNACE_CAPACITY = 200; //kg
	}
}

public class FurnaceState
{
	
	public readonly object AccessLock = new object();

	public int LastUniqueId;

	public Services.FurnaceState FurncState;

	public int GlassMass = 0; //kilograms

	public double FurnaceEnergy = 0; //jules

	//private List<bool> ClientWork = new List<bool>(); //boolean list of clients that interacted with the server, index + 1 = clientId
}


class FurnaceLogic
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

	public int GetUniqueId() 
	{
		lock( mState.AccessLock )
		{
			mState.LastUniqueId += 1;
			return mState.LastUniqueId;
		}
	}
		
	public Services.FurnaceState GetFurnaceState() 
	{
		lock( mState.AccessLock )
		{
			return mState.FurncState;
		}
	}

	public CycleAttemptResult MeltingGlass(ClientDesc client)
	{
		//prepare result descriptor
		var par = new CycleAttemptResult();

		lock( mState.AccessLock )
		{
			mLog.Info($"{client.ClientType} {client.ClientId}, is trying to do work.");

			if(mState.FurncState == Services.FurnaceState.Pouring)
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
						mState.GlassMass = newMass;
					
					else
					{
						par.IsSuccess = false;
						par.FailReason = "furnace is full";
					}
				}
				else
				{
					mState.FurnaceEnergy += client.GeneratedValue;
				}
			}

			//log result
			if( par.IsSuccess )
			{
				//mState.ClientWork[client.ClientId-1] = true;
				mLog.Info( $"{client.ClientType} has succesfully " +
				$"{(client.ClientType == ClientType.Heater ? $"increased the thermal energy by {client.GeneratedValue} " 
				: $"added {client.GeneratedValue} ")}" +
				$" {(client.ClientType == ClientType.Heater ? " J" : " kg of glass")}.");
			}
			else 
			{
				mLog.Info($"{client.ClientType} has failed because '{par.FailReason}'.");
			}

			//
			return par;
		}
	}

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

			if(cycleCounter < 3){
				cycleCounter++;
				continue;
			}
			cycleCounter = 0;
			
			lock( mState.AccessLock )
			{
				mState.FurncState = (mState.FurnaceEnergy/(Constants.GlassProperties.SPECIFIC_HEAT_CAPACITY*mState.GlassMass))+Constants.GlassProperties.DEFAULT_TEMP >= Constants.GlassProperties.MELTING_TEMP ? Services.FurnaceState.Pouring : Services.FurnaceState.Melting;
				if(mState.FurncState == Services.FurnaceState.Pouring && mState.GlassMass > 0){
					mLog.Info($"Furnace is pouring molten glass, ammount {mState.GlassMass}.");
					mState.GlassMass = 0;
					mState.FurnaceEnergy = 0;
				}
				else
				{
					mLog.Info($"Furnace is currently melting glass, current glass temperature {mState.FurnaceEnergy}, ammount of glass in the furnace {mState.GlassMass}");
				}
			}
			if(mState.FurncState == Services.FurnaceState.Pouring)
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
				mState.FurncState = Services.FurnaceState.Melting;
			}
		}
	}
}