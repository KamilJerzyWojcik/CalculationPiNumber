using System;
using System.Text;
using System.Threading;
using Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RpcClient
{
    public class RpcClientManagement
    {
        private readonly IConnection connection;

        public readonly IModel channel;

        public readonly EventingBasicConsumer consumer;

        public static ApplicationConfiguration config => ApplicationConfiguration.Instance;

        public RpcClientManagement()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.QueueDeclare(queue: config.QueueManagment,
                                      durable: false,
                                      exclusive: false,
                                      autoDelete: false,
                                      arguments: null);
        }

        public void Call(int id)
        {
            string message = id.ToString();
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                                 routingKey: config.QueueManagment,
                                 basicProperties: null,
                                 body: body);

            Console.WriteLine($"[x] Sent stop id: {message}");
        }

        public void Close()
        {
            connection.Close();
        }
    }
}
