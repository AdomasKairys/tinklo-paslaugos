extern alias simplerpc;
namespace Clients;

using System;
using System.Text;
using System.Threading;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using NLog;

using SimpleRpc = simplerpc::Services;


/// <summary>
/// <para>RPC style wrapper for the service.</para>
/// <para>Static members are thread safe, instance members are not.</para>
/// </summary>
class FurnaceClient : SimpleRpc.IFurnaceService
{
	/// <summary>
	/// Name of the message exchange.
	/// </summary>
	private static readonly String ExchangeName = "T120B180.Furnace.Exchange";

	/// <summary>
	/// Name of the server queue.
	/// </summary>
	private static readonly String ServerQueueName = "T120B180.Furnace.FurnaceService";

	/// <summary>
	/// Prefix for the name of the client queue.
	/// </summary>
	private static readonly String ClientQueueNamePrefix = "T120B180.Furnace.FurnaceClient";


	/// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger log = LogManager.GetCurrentClassLogger();


	/// <summary>
	/// Service client ID.
	/// </summary>
	public String ClientId {get;}

	/// <summary>
	/// Name of the client queue.
	/// </summary>
	private String ClientQueueName {get;}


	/// <summary>
	/// Connection to RabbitMQ message broker.
	/// </summary>
	private IConnection rmqConn;

	/// <summary>
	/// Communications channel to RabbitMQ message broker.
	/// </summary>
	private IModel rmqChann;


	/// <summary>
	/// Constructor.
	/// </summary>
	public FurnaceClient()
	{
		//initialize properties
		ClientId = Guid.NewGuid().ToString();
		ClientQueueName = ClientQueueNamePrefix + ClientId;

		//log client ID for easier traceability
		log.Info($"Client ID is '{ClientId}'.");

		//connect to the RabbitMQ message broker
		var rmqConnFact = new ConnectionFactory();
		rmqConn = rmqConnFact.CreateConnection();

		//get channel, configure exchange and queue
		rmqChann = rmqConn.CreateModel();

		rmqChann.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Direct);
		rmqChann.QueueDeclare(queue: ClientQueueName, durable: false, exclusive: true, autoDelete: false, arguments: null);
		rmqChann.QueueBind(queue: ClientQueueName, exchange: ExchangeName, routingKey: ClientQueueName, arguments: null);

		//XXX: see https://www.rabbitmq.com/dotnet-api-guide.html#concurrency for threading issues
	}

	/// <summary>
	/// Generic method to call a remove operation on a server.
	/// </summary>
	/// <param name="methodName">Name of the method to call.</param>
	/// <param name="requestDataProvider">Request data provider. Can be null if no data is to be provided.</param>
	/// <param name="resultDataExtractor">Result extractor. If this is null, the result will not be awaited for.</param>
	/// <typeparam name="RESULT">Type of the result.</typeparam>
	/// <returns>Result of the call.</returns>
	private RESULT Call<RESULT>(
		string methodName,
		Func<String> requestDataProvider,
		Func<String, RESULT> resultDataExtractor
	) {
		//validate inputs
		if( methodName == null )
			throw new ArgumentException("Argument 'methodName' is null.");

		//declare result storage
		RESULT result = default;

		//declare stuff used to avoid result owerwriting and to signal when result is ready
		var isResultReady = false;
		var resultReadySignal = new AutoResetEvent(false);

		//create request
		var request =
			new SimpleRpc.RPCMessage()
			{
				Action = $"Call_{methodName}",
				Data = requestDataProvider != null ? requestDataProvider() : null
			};

		var requestProps = rmqChann.CreateBasicProperties();
		requestProps.CorrelationId = Guid.NewGuid().ToString();
		requestProps.ReplyTo = ClientQueueName;

		//result data extractor set? set-up receiver for response message
		string consumerTag = null;

		if( resultDataExtractor != null )
		{
			//ensure contents of variables set in main thread, are loadable by receiver thread
			Thread.MemoryBarrier();

			//create response message consumer
			var consumer = new EventingBasicConsumer(rmqChann);
			consumer.Received +=
				(channel, delivery) => {
					//ensure contents of variables set in main thread are loaded into this thread
					Thread.MemoryBarrier();

					//prevent owerwriting of result, check if the expected message is received
					if( !isResultReady && (delivery.BasicProperties.CorrelationId == requestProps.CorrelationId) )
					{
						var response = JsonConvert.DeserializeObject<SimpleRpc.RPCMessage>(Encoding.UTF8.GetString(delivery.Body.ToArray()));
						if( response.Action == $"Result_{methodName}" )
						{
							//extract the result
							result = resultDataExtractor(response.Data);

							//indicate result has been received, ensure it is loadable by main thread
							isResultReady = true;
							Thread.MemoryBarrier();

							//signal main thread that result has been received
							resultReadySignal.Set();
						}
						else
						{
							log.Info($"Unsupported type of RPC action '{request.Action}'. Ignoring the message.");
						}
					}
				};

			//attach message consumer to the response queue
			consumerTag = rmqChann.BasicConsume(ClientQueueName, true, consumer);
		}

		//send request
		rmqChann.BasicPublish(
			exchange : ExchangeName,
			routingKey : ServerQueueName,
			basicProperties : requestProps,
			body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
		);

		//result data extractor set? await for response message
		if( resultDataExtractor != null )
		{
			//wait for the result to be ready
			resultReadySignal.WaitOne();

			//ensure contents of variables set by the receiver are loaded into this thread
			Thread.MemoryBarrier();

			//detach message consumer from the response queue
			rmqChann.BasicCancel(consumerTag);

			//
			return result;		
		}

		//we did not wait for response, return default value of whatever is expected
		return default;
	}

	/// <summary>
	/// Get next unique ID from the server. Is used by cars to acquire client ID's.
	/// </summary>
	/// <returns>Unique ID.</returns>
	public int GetUniqueId()
	{
		var result =
			Call(
				nameof(GetUniqueId),
				null,
				(data) => JsonConvert.DeserializeAnonymousType(data, new {Value = 0}).Value
			);
		return result;
	}

	/// <summary>
	/// Get current furnace state.
	/// </summary>
	/// <returns>Current furnace state.</returns>				
	public SimpleRpc.FurnaceState GetFurnaceState() 
	{
		var result =
			Call(
				nameof(GetFurnaceState),
				null,
				(data) => JsonConvert.DeserializeAnonymousType(data, new {Value = SimpleRpc.FurnaceState.Pouring}).Value
			);
		return result;
	}

	/// <summary>
	/// Try melting the glass
	/// </summary>
	/// <param name="client">Client descriptor.</param>
	/// <returns>Cycle attempt descriptor.</returns>
	public SimpleRpc.CycleAttemptResult MeltGlass(SimpleRpc.ClientDesc client)
	{
		var result =
			Call(
				nameof(MeltGlass),
				() => JsonConvert.SerializeObject(client),
				(data) => JsonConvert.DeserializeObject<SimpleRpc.CycleAttemptResult>(data)
			);
		return result;
	}
}