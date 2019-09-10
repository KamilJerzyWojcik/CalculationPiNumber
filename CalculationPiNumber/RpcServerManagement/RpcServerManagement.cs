using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Configuration;
using System.Collections.Generic;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RpcServerManagement
{
    class RpcServerManagement
    {
        public static ApplicationConfiguration config = ApplicationConfiguration.Instance;
        public static IDatabase context = config.context;

        public static void Main()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    #region
                        // Configuration
                        channel.QueueDeclare(queue: config.QueueManagment,
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                        var consumer = new EventingBasicConsumer(channel);

                        channel.BasicConsume(queue: config.QueueManagment,
                                             autoAck: true,
                                             consumer: consumer);

                        Console.WriteLine(" [x] Awaiting Wroker requests message to stop");
                    #endregion

                    consumer.Received += (model, ea) =>
                    {
                        var id = Encoding.UTF8.GetString(ea.Body);

                        if (int.TryParse(id, out int result))
                        {
                            var stopIds = GetIdsToStop(result);

                            var stopIdsJson = JsonConvert.SerializeObject(stopIds);

                            context.StringSet(config.StopIdsKey, stopIdsJson);

                            Console.WriteLine($" [x] Received stop id: {id}");
                        }
                        else
                        {
                            Console.WriteLine($" [x] Received wrong stop id");
                        }
                    };

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }

        private static List<int> GetIdsToStop(int result)
        {
            var storeIds = context.StringGet(config.StopIdsKey).ToString();
            var stopIds = new List<int>();

            if (string.IsNullOrEmpty(storeIds))
            {
                stopIds = new List<int>() { result };
            }
            else
            {
                stopIds = JsonConvert.DeserializeObject<List<int>>(storeIds);
                stopIds.Add(result);
            }

            return stopIds;
        }

    }
}
