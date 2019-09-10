using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Configuration.Models;

namespace RpcClient
{
    public class Rpc
    {
        private static object syncCalculation = new object();

        private static object syncStop = new object();

        public static void Main()
        {
            while (true)
            {
                Menu();

                var command = Console.ReadLine();

                if (command.ToLower() == "send")
                {
                    var task = Task.Run(() => CallRpcClient(GetMessages()));
                }

                if (command.ToLower() == "exit")
                {
                    return;
                }

                if (command.ToLower().Contains("stop id="))
                {
                    if (int.TryParse(command.Substring(8), out int result))
                    {
                        var task = Task.Run(() => SendIdToStop(result));
                    }
                    else
                    {
                        Console.WriteLine("Id is not a number");
                    }
                }
            }
        }

        private static List<TaskMessage> GetMessages()
        {
            var messages = new List<TaskMessage>();

            for (int i = 1; i <= 100; i++)
            {
                messages.Add(new TaskMessage()
                {
                    Id = i,
                    Task = "CalculationPi",
                    Precision = i,
                    IsFirst = i == 1 ? true : false
                });
            }

            return messages;
        }

        private static void CallRpcClient(List<TaskMessage> messages)
        {
            bool lockTaken = Monitor.TryEnter(syncCalculation);

            if (lockTaken)
            {
                RpcClient rpcClient = new RpcClient();
                try
                {
                    foreach (var message in messages)
                    {
                        var resultMessage = rpcClient.Call(message);
                        Console.WriteLine($" [.] Got Id {resultMessage.Id} with result: {resultMessage.Result} (precision: {resultMessage.Precision})");
                    }

                    Console.WriteLine("All messages were Sent");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Process throw exception: ", e.Message);
                    rpcClient.Close();
                }
                finally
                {
                    rpcClient.Close();
                }
            }
            else
            {
                Console.WriteLine("Calculation is in progress, please wait or use stop id=");
            }
        }

        private static void SendIdToStop(int id)
        {
            bool lockTaken = Monitor.TryEnter(syncStop);

            if (lockTaken)
            {
                RpcClientManagement rpcClientManagement = new RpcClientManagement();

                rpcClientManagement.Call(id);

                rpcClientManagement.Close();
            }
            else
            {
                Console.WriteLine("Sending stop id task in progress, please wait");
            }
        }

        private static void Menu()
        {
            Console.WriteLine("");
            Console.WriteLine("----------");
            Console.WriteLine("Exit: exit");
            Console.WriteLine("Send 100 requests: send");
            Console.WriteLine("Stop by id: stop id=x");
            Console.WriteLine("Stop all: stop id=-1");
            Console.WriteLine("----------");
            Console.WriteLine("");
        }

    }
}
