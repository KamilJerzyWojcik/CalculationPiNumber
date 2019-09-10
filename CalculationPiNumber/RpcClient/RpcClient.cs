using System;
using System.Text;
using Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Configuration.Models;
using System.Collections.Concurrent;

namespace RpcClient
{
    public class RpcClient
    {
        private readonly IConnection connection;

        public readonly IModel channel;

        private readonly string replyQueueName;

        public readonly EventingBasicConsumer consumer;

        private readonly IBasicProperties props;

        private readonly BlockingCollection<ResultMessage> respQueue = new BlockingCollection<ResultMessage>();

        public static ApplicationConfiguration config => ApplicationConfiguration.Instance;

        public RpcClient()
        {

            #region
                // Configuration
                var factory = new ConnectionFactory() { HostName = "localhost" };

                connection = factory.CreateConnection();
                channel = connection.CreateModel();

                replyQueueName = channel.QueueDeclare().QueueName;
                consumer = new EventingBasicConsumer(channel);

                props = channel.CreateBasicProperties();
                var correlationId = Guid.NewGuid().ToString();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyQueueName;
            #endregion

            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;

                var response = Encoding.UTF8.GetString(body);

                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    var resultMessage = JsonConvert.DeserializeObject<ResultMessage>(response);
                    respQueue.Add(resultMessage);
                }
            };
        }

        public ResultMessage Call(TaskMessage message)
        {
            var messageJson = JsonConvert.SerializeObject(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);
            channel.BasicPublish(
                exchange: "",
                routingKey: config.QueueName,
                basicProperties: props,
                body: messageBytes);

            channel.BasicConsume(
                consumer: consumer,
                queue: replyQueueName,
                autoAck: true);

            return respQueue.Take();
        }

        public void Close()
        {
            connection.Close();
        }
    }
}