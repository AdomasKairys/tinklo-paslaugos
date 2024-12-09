extern alias srabbitmq;

namespace Servers;

using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NLog;
using Newtonsoft.Json;

using Services;
using SRabbitMQ = srabbitmq::Services;
using Grpc.Net.Client;



/// <summary>
/// Simple RPC Furnace contract
/// </summary>
public class FurnaceServiceAdapter
{
	/// <summary>
	/// Name of the request exchange.
	/// </summary>
	private static readonly String ExchangeName = "T120B180.Furnace.Exchange";

	/// <summary>
	/// Name of the request queue.
	/// </summary>
	private static readonly String ServerQueueName = "T120B180.Furnace.FurnaceService";


	/// <summary>
	/// Logger for this class.
	/// </summary>
	private Logger log = LogManager.GetCurrentClassLogger();


	/// <summary>
	/// Connection to RabbitMQ message broker.
	/// </summary>
	private IConnection rmqConn;

	/// <summary>
	/// Communications channel to RabbitMQ message broker.
	/// </summary>
	private IModel rmqChann;

	/// <summary>
	/// Service logic.
	/// </summary>
	private readonly FurnaceClient? furnace;

	/// <summary>
	/// Constructor.
	/// </summary>
	public FurnaceServiceAdapter()
	{
		while(true){
			try{
				log.Info("Connecting to REST server");
				furnace = new FurnaceClient("http://127.0.0.1:5000", new HttpClient());
				furnace.GetFurnaceState(); // arbitrary call to check connection
				break;
			}
			catch(Exception e){
				log.Warn(e, "Failed to connect to REST server, retrying");
				Thread.Sleep(2000);
			}
		}
		log.Info("Connected succesfully");


		//connect to the RabbitMQ message broker
		var rmqConnFact = new ConnectionFactory();
		rmqConn = rmqConnFact.CreateConnection();

		//get channel, configure exchanges and request queue
		rmqChann = rmqConn.CreateModel();

		rmqChann.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Direct);
		rmqChann.QueueDeclare(queue: ServerQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
		rmqChann.QueueBind(queue: ServerQueueName, exchange: ExchangeName, routingKey: ServerQueueName, arguments: null);

		//connect to the queue as consumer
		//XXX: see https://www.rabbitmq.com/dotnet-api-guide.html#concurrency for threading issues
		var rmqConsumer = new EventingBasicConsumer(rmqChann);
		rmqConsumer.Received += (consumer, delivery) => OnMessageReceived(((EventingBasicConsumer)consumer).Model, delivery);
		rmqChann.BasicConsume(queue: ServerQueueName, autoAck: true, consumer : rmqConsumer);
	}

	/// <summary>
	/// Is invoked to process messages received.
	/// </summary>
	/// <param name="channel">Related communications channel.</param>
	/// <param name="msgIn">Message deliver data.</param>
	private void OnMessageReceived(IModel channel, BasicDeliverEventArgs msgIn)
	{
		try
		{
			//get call request
			var request =
				JsonConvert.DeserializeObject<SRabbitMQ.RPCMessage>(
					Encoding.UTF8.GetString(
						msgIn.Body.ToArray()
					)
				);

			//set response as undefined by default
			SRabbitMQ.RPCMessage response = null;

			//process the call
			switch( request.Action )
			{
				case $"Call_{nameof(furnace.GetUniqueId)}":
				{
					//make the call
					var result = furnace!.GetUniqueId();

					//create response
					response =
						new SRabbitMQ.RPCMessage() {
							Action = $"Result_{nameof(furnace.GetUniqueId)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}

				case $"Call_{nameof(furnace.GetFurnaceState)}":
				{
					//make the call
					var result = furnace!.GetFurnaceState();

					//create response
					response =
						new SRabbitMQ.RPCMessage() {
							Action = $"Result_{nameof(furnace.GetFurnaceState)}",
							Data = JsonConvert.SerializeObject(new {Value = result})
						};

					//
					break;
				}
				case $"Call_{nameof(furnace.MeltGlass)}":
				{
					//deserialize arguments
					var clientDesc = JsonConvert.DeserializeObject<SRabbitMQ.ClientDesc>(request.Data);
					
					var clientDescgRPC = new ClientDesc(){
						ClientId = clientDesc.ClientId,
						ClientType = (ClientType)clientDesc.ClientType,
						GeneratedValue = clientDesc.GeneratedValue
					};

					//make the call
					var result = furnace!.MeltGlass(clientDescgRPC);

					//create response
					response =
						new SRabbitMQ.RPCMessage() {
							Action = $"Result_{nameof(furnace.MeltGlass)}",
							Data = JsonConvert.SerializeObject(result)
						};

					//
					break;
				}

				default:
				{
					log.Info($"Unsupported type of RPC action '{request.Action}'. Ignoring the message.");
					break;
				}
			}

			//response is defined? send reply message
			if( response != null )
			{
				//prepare metadata for outgoing message
				var msgOutProps = channel.CreateBasicProperties();
				msgOutProps.CorrelationId = msgIn.BasicProperties.CorrelationId;

				//send reply message to the client queue
				channel.BasicPublish(
					exchange : ExchangeName,
					routingKey : msgIn.BasicProperties.ReplyTo,
					basicProperties : msgOutProps,
					body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response))
				);
			}
		}
		catch( Exception e )
		{
			log.Error(e, "Unhandled exception caught when processing a message. The message is now lost.");
		}
	}
}