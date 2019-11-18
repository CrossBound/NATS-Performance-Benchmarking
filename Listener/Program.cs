using NATS.Client;
using System;

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
                string url = $"nats://{args[0]}:{args[1]}";
                Console.Title = $"Consumer - {url}";

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = url;
                options.NoEcho = true;
                options.Pedantic = false;
                options.Verbose = false;
                options.PingInterval = 10_000;

                options.AsyncErrorEventHandler += (sender, args) =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] AsyncErrorEventHandler fired [ConnectionID={args.Conn.ConnectedId ?? "n/a"}; ConnectionURL={args.Conn.ConnectedUrl ?? "n/a"};]");
                    Console.WriteLine($"    Error: {args.Error}");
                };

                options.ClosedEventHandler += (sender, args) =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ClosedEventHandler fired [ConnectionID={args.Conn.ConnectedId ?? "n/a"}; ConnectionURL={args.Conn.ConnectedUrl ?? "n/a"};]");
                };

                options.DisconnectedEventHandler += (sender, args) =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] DisconnectedEventHandler fired [ConnectionID={args.Conn.ConnectedId ?? "n/a"}; ConnectionURL={args.Conn.ConnectedUrl ?? "n/a"};]");
                };

                options.ReconnectedEventHandler += (sender, args) =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ReconnectedEventHandler fired [ConnectionID={args.Conn.ConnectedId ?? "n/a"}; ConnectionURL={args.Conn.ConnectedUrl ?? "n/a"};]");
                };

                options.ServerDiscoveredEventHandler += (sender, args) =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ServerDiscoveredEventHandler fired [ConnectionID={args.Conn.ConnectedId ?? "n/a"}; ConnectionURL={args.Conn.ConnectedUrl ?? "n/a"};]");
                };

                _connection = new ConnectionFactory().CreateConnection(options);
                _subscription = _connection.SubscribeSync("queue");

                Console.WriteLine("Consuming");
                while (_running)
                {
                    var message = _subscription.NextMessage();
                    
                    _connection.Publish(message.Reply, message.Data);
                    _connection.Flush();
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
