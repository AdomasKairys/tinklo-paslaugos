namespace Servers;

using NLog;

using Services;


/// <summary>
/// Traffic light state descritor.
/// </summary>
public class FurnaceState
{
	/// <summary>
	/// Access lock.
	/// </summary>
	public readonly object AccessLock = new object();

	/// <summary>
	/// Last unique ID value generated.
	/// </summary>
	public int LastUniqueId;

	/// <summary>
	/// Light state.
	/// </summary>
	public Services.FurnaceState FurncState;

	public int GlassMass = 0; //kilograms

	public double GlassTemperature = 0; //kelvin

	/// <summary>
	/// Car queue.
	/// </summary>
	public List<int> ClientQueue = new List<int>();
}


/// <summary>
/// <para>Traffic light logic.</para>
/// <para>Thread safe.</para>
/// </summary>
class FurnaceLogic
{
	/// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger mLog = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Background task thread.
	/// </summary>
	private Thread mBgTaskThread;

	/// <summary>
	/// State descriptor.
	/// </summary>
	private FurnaceState mState = new FurnaceState();
	

	/// <summary>
	/// Constructor.
	/// </summary>
	public FurnaceLogic()
	{
		//start the background task
		mBgTaskThread = new Thread(BackgroundTask);
		mBgTaskThread.Start();
	}

	/// <summary>
	/// Get next unique ID from the server. Is used by cars to acquire client ID's.
	/// </summary>
	/// <returns>Unique ID.</returns>
	public int GetUniqueId() 
	{
		lock( mState.AccessLock )
		{
			mState.LastUniqueId += 1;
			return mState.LastUniqueId;
		}
	}

	/// <summary>
	/// Get current light state.
	/// </summary>
	/// <returns>Current light state.</returns>				
	public Services.FurnaceState GetFurnaceState() 
	{
		lock( mState.AccessLock )
		{
			return mState.FurncState;
		}
	}

	/// <summary>
	/// Queue give car at the light. Will only succeed if light is red.
	/// </summary>
	/// <param name="client">Car to queue.</param>
	/// <returns>True on success, false on failure.</returns>
	public bool Queue(ClientDesc client)
	{
		lock( mState.AccessLock )
		{
			mLog.Info($"Client {client.ClientId}, Operator {client.ClientNameSurname}, is trying to queue.");

			//light not red? do not allow to queue
			if(mState.FurncState != Services.FurnaceState.Pouring )
			{
                mLog.Info("Queing denied, because light is not red.");
				return false;
			}

			//already in queue? deny
			if( mState.ClientQueue.Exists(it => it == client.ClientId) )
			{
				mLog.Info("Queing denied, because car is already in queue.");
				return false;
			}

			//queue
			mState.ClientQueue.Add(client.ClientId);
			mLog.Info("Queuing allowed.");

			//
			return true;
		}
	}

	/// <summary>
	/// Tell if car is first in line in queue.
	/// </summary>
	/// <param name="clientId">ID of the car to check for.</param>
	/// <returns>True if car is first in line. False if not first in line or not in queue.</returns>
	public bool IsFirstInLine(int clientId)
	{
		lock( mState.AccessLock )
		{
			//no queue entries? return false
			if( mState.ClientQueue.Count == 0 )
				return false;

			//check if first in line
			return mState.ClientQueue[0] == clientId;
		}
	}

	/// <summary>
	/// Try passing the traffic light. If car is in queue, it will be removed from it.
	/// </summary>
	/// <param name="client">Car descriptor.</param>
	/// <returns>Pass result descriptor.</returns>
	public CycleAttemptResult FurnacePass(ClientDesc client)
	{
		//prepare result descriptor
		var par = new CycleAttemptResult();

		lock( mState.AccessLock )
		{
			//mLog.Info($"Car {client.CarId}, RegNr. {client.CarNumber}, Driver {client.DriverNameSurname}, is trying to pass.");

			//light is red? do not allow to pass
			bool inQueue = mState.ClientQueue.Exists(it => it == client.ClientId);

			if(mState.FurncState == Services.FurnaceState.Pouring)
			{
				par.IsSuccess = false;

				if(!inQueue)
					par.FailReason = "furnace was pouring";
				else
				{
					par.FailReason =  mState.ClientQueue[0] == client.ClientId ? "furnace was pouring" : "not first in line";
					
                	mState.ClientQueue = mState.ClientQueue.Where(it => it != client.ClientId).ToList();
				}
			}
			//light is green, allow to pass if not in queue or first in queue
			else
			{
				//car in queue?
				if(inQueue && mState.ClientQueue[0] != client.ClientId)
				{
					par.IsSuccess = false;
					par.FailReason = "not first in line";
				}
				else
				{
					if(inQueue)
						mState.ClientQueue = mState.ClientQueue.Where(it => it != client.ClientId).ToList();

					par.IsSuccess = true;

					if(client.ClientType == ClientType.Loader)
					{
						int newMass = client.GeneratedValue + mState.GlassMass;
						mState.GlassTemperature = (GlassProperties.DEFAULT_TEMP*client.GeneratedValue + mState.GlassTemperature*mState.GlassMass)/newMass;
						mState.GlassMass = newMass;
					}
					else if(mState.GlassMass > 0)
					{
						mState.GlassTemperature += client.GeneratedValue/(mState.GlassMass*GlassProperties.SPECIFIC_HEAT_CAPACITY);
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
				mLog.Info( $"{client.ClientType} was succesfull.");
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
	/// Background task for the traffic light.
	/// </summary>
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
				mState.FurncState = mState.GlassTemperature >= GlassProperties.MELTING_TEMP ? Services.FurnaceState.Pouring : Services.FurnaceState.Melting;
				if(mState.FurncState == Services.FurnaceState.Pouring){
					mState.GlassMass = 0;
					mState.GlassTemperature = 0;
					mLog.Info($"Furnace is pouring molten glass, ammount {mState.GlassMass}.");
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