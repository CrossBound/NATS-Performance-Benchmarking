using NATS.Client;
using System;
using System.Threading;

namespace NATS_WorkQueue.Consumer
{
    public class Program
    {
        private static bool _running = true;
        private static IConnection _connection;
        private static ISyncSubscription _subscription;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting consumer");
            try
            {
                Console.CancelKeyPress += (sender, args) =>
                {
                    Console.WriteLine();
                    Console.WriteLine("Cancelling...");
                    _running = false;
                    if (_subscription != null)
                    {
                        _subscription.Drain(500);
                        _subscription.Dispose();
                        _subscription = null;
                        Console.WriteLine("Subscription disposed");
                    }

                    if (_connection != null)
                    {
                        _connection.Drain(1000);
                        _connection.Close();
                        _connection.Dispose();
                        _connection = null;
                        Console.WriteLine("Connection disposed");
                        Thread.Sleep(100);
                    }
                };

                string url = $"nats://{args[0]}:{args[1]}";
                Console.Title = $"Consumer - {url}";

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = url;
                options.NoEcho = true;
                options.Pedantic = false;
                options.Verbose = false;

                _connection = new ConnectionFactory().CreateConnection(options);
                _subscription = _connection.SubscribeSync("queue");

                Console.WriteLine("Consuming");
                while (_running)
                {
                    var message = _subscription.NextMessage();
                    
                    _connection.Publish(message.Reply, message.Data);
                }
            }
            catch (Exception error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(error.ToString());
                Console.ResetColor();
            }
        }
    }
}
