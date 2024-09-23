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

	/// <summary>
	/// Car queue.
	/// </summary>
	public List<int> CarQueue = new List<int>();
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
			if(mState.FurncState != Services.FurnaceState.Red )
			{
                mLog.Info("Queing denied, because light is not red.");
				return false;
			}

			//already in queue? deny
			if( mState.CarQueue.Exists(it => it == client.ClientId) )
			{
				mLog.Info("Queing denied, because car is already in queue.");
				return false;
			}

			//queue
			mState.CarQueue.Add(client.ClientId);
			mLog.Info("Queuing allowed.");

			//
			return true;
		}
	}

	/// <summary>
	/// Tell if car is first in line in queue.
	/// </summary>
	/// <param name="carId">ID of the car to check for.</param>
	/// <returns>True if car is first in line. False if not first in line or not in queue.</returns>
	public bool IsFirstInLine(int carId)
	{
		lock( mState.AccessLock )
		{
			//no queue entries? return false
			if( mState.CarQueue.Count == 0 )
				return false;

			//check if first in line
			return (mState.CarQueue[0] == carId);
		}
	}

	/// <summary>
	/// Try passing the traffic light. If car is in queue, it will be removed from it.
	/// </summary>
	/// <param name="car">Car descriptor.</param>
	/// <returns>Pass result descriptor.</returns>
	public CycleAttemptResult Pass(CarDesc car)
	{
		//prepare result descriptor
		var par = new CycleAttemptResult();

		lock( mState.AccessLock )
		{
			mLog.Info($"Car {car.CarId}, RegNr. {car.CarNumber}, Driver {car.DriverNameSurname}, is trying to pass.");

			//light is red? do not allow to pass
			if(mState.FurncState == Services.FurnaceState.Red )
			{
				//indicate car crashed
				par.IsSuccess = false;
				
				//set crash reason
				if(mState.CarQueue.Exists(it => it == car.CarId) )
				{
					if(mState.CarQueue[0] == car.CarId )
						par.FailReason = "tried to run a red light";
					else
						par.FailReason = "hit a car in front of it";

                    //remove car from queue
                    mState.CarQueue = mState.CarQueue.Where(it => it != car.CarId).ToList();
				}
				else
				{
					par.FailReason = "tried to run a red light";
				}
			}
			//light is green, allow to pass if not in queue or first in queue
			else
			{
				//car in queue?
				if(mState.CarQueue.Exists(it => it == car.CarId) )
				{
					//first in queue? allow to pass
					if(mState.CarQueue[0] == car.CarId )
					{
						par.IsSuccess = true;						
					}
					//not first in queue, crash
					else
					{
						par.IsSuccess = false;
						par.FailReason = "hit a car in front of it";
					}

                    //remove car from queue
                    mState.CarQueue = mState.CarQueue.Where(it => it != car.CarId).ToList();
				}
				//car not in queue
				{
					par.IsSuccess = true;
				}
			}

			//log result
			if( par.IsSuccess )
			{
				mLog.Info("Car has passed.");
			}
			else
			{
				mLog.Info($"Car has crashed because '{par.FailReason}'.");
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
                mState.FurncState =
                    mState.FurncState == Services.FurnaceState.Red 
					? Services.FurnaceState.Green 
					: Services.FurnaceState.Red;

				mLog.Info($"New light state is '{mState.FurncState}'.");
			}
		}
	}
}