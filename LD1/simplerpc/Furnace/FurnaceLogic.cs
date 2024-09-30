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

	public double GlassTemperature = 0; //kelvin

	public List<int> HeaterQueue = new List<int>();

	public List<int> LoaderQueue = new List<int>();
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

	public bool Queue(ClientDesc client)
	{
		lock( mState.AccessLock )
		{
			mLog.Info($"{client.ClientType} {client.ClientId}, Operator {client.ClientNameSurname}, is trying to queue.");

			//light not red? do not allow to queue
			if(mState.FurncState != Services.FurnaceState.Pouring )
			{
                mLog.Info("Queing denied, because furnace is melting.");
				return false;
			}
			List<int> queue = client.ClientType == ClientType.Heater ? mState.HeaterQueue : mState.LoaderQueue;
			//already in queue? deny
			if( queue.Exists(it => it == client.ClientId) )
			{
				mLog.Info("Queing denied, because client is already in queue.");
				return false;
			}

			//queue
			queue.Add(client.ClientId);
			mLog.Info("Queuing allowed.");

			//
			return true;
		}
	}

	public bool IsFirstInLine(int clientId, ClientType clientType)
	{
		lock( mState.AccessLock )
		{
			List<int> queue = clientType == ClientType.Heater ? mState.HeaterQueue : mState.LoaderQueue;
			//no queue entries? return false
			if( queue.Count == 0 )
				return false;
			//check if first in line
			return queue[0] == clientId;
		}
	}

	public CycleAttemptResult FurnacePass(ClientDesc client)
	{
		//prepare result descriptor
		var par = new CycleAttemptResult();

		lock( mState.AccessLock )
		{
			mLog.Info($"{client.ClientType} {client.ClientId}, Operator {client.ClientNameSurname}, is trying to do work.");
			List<int> queue = client.ClientType == ClientType.Heater ? mState.HeaterQueue : mState.LoaderQueue;
			//light is red? do not allow to pass
			bool inQueue = queue.Exists(it => it == client.ClientId);

			if(mState.FurncState == Services.FurnaceState.Pouring)
			{
				par.IsSuccess = false;

				if(!inQueue)
					par.FailReason = "furnace was pouring";
				else
				{
					par.FailReason =  queue[0] == client.ClientId ? "furnace was pouring" : "not first in line";
					
                	//queue = queue.Where(it => it != client.ClientId).ToList();
				}
			}
			//light is green, allow to pass if not in queue or first in queue
			else
			{
				//car in queue?
				if(inQueue && queue[0] != client.ClientId)
				{
					par.IsSuccess = false;
					par.FailReason = "not first in line";
				}
				else
				{
					par.IsSuccess = true;

					if(client.ClientType == ClientType.Loader)
					{
						int newMass = client.GeneratedValue + mState.GlassMass;
						if(newMass <= Constants.FurnaceProperties.FURNACE_CAPACITY)
						{
							mState.GlassTemperature = (Constants.GlassProperties.DEFAULT_TEMP*client.GeneratedValue + mState.GlassTemperature*mState.GlassMass)/newMass;
							mState.GlassMass = newMass;
						}
						else
						{
							par.IsSuccess = false;
							par.FailReason = "furnace is full";
						}
					}
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
			}

			//log result
			if( par.IsSuccess )
			{
				if(inQueue)
					queue.Remove(client.ClientId);
				
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

	public void BackgroundTask()
	{
		//intialize random number generator
		var rnd = new Random();

		//
		while( true )
		{
			//sleep a while
			Thread.Sleep(500 + rnd.Next(1500));

			//switch the light
			lock( mState.AccessLock )
			{
				mState.FurncState = mState.GlassTemperature >= Constants.GlassProperties.MELTING_TEMP ? Services.FurnaceState.Pouring : Services.FurnaceState.Melting;
				if(mState.FurncState == Services.FurnaceState.Pouring){
					mLog.Info($"Furnace is pouring molten glass, ammount {mState.GlassMass}.");
					mState.GlassMass = 0;
					mState.GlassTemperature = 0;
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
				else
				{
					mLog.Info($"Furnace is currently melting glass, current glass temperature {mState.GlassTemperature}, ammount of glass in the furnace {mState.GlassMass}");
				}
			}
		}
	}
}