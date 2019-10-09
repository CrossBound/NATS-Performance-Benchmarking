using System;
using NATS.Client;
using System.Threading;

namespace NATS_WorkQueue.Publisher
{
    public static class Program
    {
        private static bool _running = true;
        private static IConnection _connection;
        private static ISyncSubscription _subscription;

        static void Main(string[] args)
        {
            Console.WriteLine("Starting publisher");
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
                Console.Title = $"Producer - {url}";

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = url;
                options.NoEcho = true;
                options.Pedantic = false;
                options.Verbose = false;

                _connection = new ConnectionFactory().CreateConnection(options);
                string inbox = _connection.NewInbox();
                _subscription = _connection.SubscribeSync(inbox);

                var payload = new byte[16];
                ulong counter = 0;
                DateTime startTime;
                Console.WriteLine("Publishing");
                while (_running)
                {
                    counter++;

                    startTime = DateTime.Now;
                    BitConverter.GetBytes(counter).CopyTo(payload, 0);
                    BitConverter.GetBytes(startTime.ToBinary()).CopyTo(payload, 8);

                    _connection.Publish("queue", inbox, payload);
                    var response = _subscription.NextMessage();
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
