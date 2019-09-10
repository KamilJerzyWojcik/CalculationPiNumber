using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Configuration;
using StackExchange.Redis;
using Newtonsoft.Json;
using System.Collections.Generic;
using Configuration.Models;
using System.Numerics;
using System.Linq;

namespace RPCServer
{
    public class RPCServerComputing
    {
        public static ApplicationConfiguration config = ApplicationConfiguration.Instance;
        public static IDatabase context = config.context;
        private static List<int> stopIds;

        public static void Main()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    #region 
                        // Configuration
                        channel.QueueDeclare(queue: config.QueueName, durable: false,
                                             exclusive: false, autoDelete: false, arguments: null);
                        channel.BasicQos(0, 1, false);
                        var consumer = new EventingBasicConsumer(channel);
                        string consumerTag = channel.BasicConsume(queue: config.QueueName,
                                                                  autoAck: false, consumer: consumer);

                        Console.WriteLine(" [x] Awaiting RPC requests message to calculate");
                    #endregion

                    consumer.Received += (model, ea) =>
                    {
                        BigInteger result = -1;
                        var body = ea.Body;
                        var message = GetMessage(body);

                        CleanMemoryIfFirst(message.IsFirst);

                        try
                        {
                            result = GetPiNumber(message);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(" [.] " + e.Message);
                        }
                        finally
                        {
                            var responseBytes = GetResponse(message, result);

                            SendResult(channel, ea, responseBytes);
                        }
                    };

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();
                }
            }
        }

        #region
            // Pi calculation
            private static BigInteger GetPiNumber(TaskMessage message)
            {
                stopIds = JsonConvert.DeserializeObject<List<int>>(context.StringGet(config.StopIdsKey));
                BigInteger result = -1;

                if (stopIds != null && (stopIds.Contains(-1) || stopIds.Contains(message.Id)))
                {
                    Console.WriteLine($" [.] Current calculating: Id: {message.Id}, calculation was stopped");
                }
                else
                {
                    Console.WriteLine($" [.] Current calculating: Id: {message.Id}, precision: {message.Precision}, please wait about 5 seconds");
                    result = CalculatePiQuick(7500, message.Precision);
                }

                return result;
            }

            private static BigInteger CalculatePiQuick(int n, int digits)
            {
                return 4 * (4 * ArcTan(5, n, digits) - ArcTan(239, n, digits));
            }

            private static BigInteger ArcTan(int oneOverX, int iterations, int digits)
            {
                var mag = BigInteger.Pow(10, digits);
                var sum1 = Enumerable.Range(0, iterations / 2).Select(i => i * 4 + 1).Aggregate(BigInteger.Zero, (sum, i) => sum += mag / (i * BigInteger.Pow(oneOverX, i)));
                var sum2 = Enumerable.Range(0, iterations / 2).Select(i => i * 4 + 3).Aggregate(BigInteger.Zero, (sum, i) => sum += mag / (i * BigInteger.Pow(oneOverX, i)));
                return sum1 - sum2;
            }

        #endregion

        #region
            // Receive message
            private static TaskMessage GetMessage(byte[] body)
            {
                var messageJson = Encoding.UTF8.GetString(body);
                var message = JsonConvert.DeserializeObject<TaskMessage>(messageJson);

                return message;
            }

            private static void CleanMemoryIfFirst(bool isFirst)
            {
                if (isFirst)
                {
                    context.StringSet(config.StopIdsKey, "");
                }
            }

        #endregion

        #region
            // Send response
            private static byte[] GetResponse(TaskMessage message, BigInteger result)
            {
                var response = new ResultMessage()
                {
                    Id = message.Id,
                    Task = message.Task,
                    Precision = message.Precision,
                    Result = result
                };

                var responseJson = JsonConvert.SerializeObject(response);

                var responseBytes = Encoding.UTF8.GetBytes(responseJson);

                return responseBytes;
            }

            private static void SendResult(IModel channel, BasicDeliverEventArgs ea, byte[] responseBytes)
            {
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                channel.BasicPublish(exchange: "", routingKey: props.ReplyTo,
                  basicProperties: replyProps, body: responseBytes);

                channel.BasicAck(deliveryTag: ea.DeliveryTag,
                  multiple: false);
            }

        #endregion
    }
}
