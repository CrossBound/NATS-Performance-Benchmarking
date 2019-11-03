using NATS.Client;
using System;
using System.Diagnostics;
using System.Threading;

namespace NATS_WorkQueue.Publisher
{
    public static class Program
    {
        private static bool _running = true;
        private static IConnection _connection;
        private static ISyncSubscription _subscription;

        // CREDIT Contributors @ https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch.frequency?view=netcore-3.0
        public static readonly long NanosecondsPerStopWatchTick = 1_000_000_000L / Stopwatch.Frequency;

        // CREDIT Contributors @ https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch.frequency?view=netcore-3.0
        public static double GetMillisecondsFromStopWatchTicks(long Ticks)
        {
            var elapsedNS = Ticks * NanosecondsPerStopWatchTick;
            return (elapsedNS / 1_000_000D);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting publisher");

            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                _running = false;
            };

            try
            {
                string url = $"nats://{args[0]}:{args[1]}";
                Console.Title = $"Producer - {url}";
                
                // give consumer time to get subscribed
                Thread.Sleep(3000); 

                var options = ConnectionFactory.GetDefaultOptions();
                options.Url = url;
                options.NoEcho = true;
                options.Pedantic = false;
                options.Verbose = false;

                string inbox = Guid.NewGuid().ToString("N");
                _connection = new ConnectionFactory().CreateConnection(options);
                _subscription = _connection.SubscribeSync(inbox);

                var payload = new byte[16];
                ulong counter = 0;
                long startTime, endTime;
                double msTaken;
                Console.WriteLine("Publishing");
                while (_running)
                {
                    counter++;
                    try
                    {
                        startTime = Stopwatch.GetTimestamp();
                        BitConverter.GetBytes(counter).CopyTo(payload, 0);
                        BitConverter.GetBytes(startTime).CopyTo(payload, 8);

                        _connection.Publish("queue", inbox, payload);
                        _connection.Flush(2000);
                        var response = _subscription.NextMessage(2000);

                        endTime = Stopwatch.GetTimestamp();
                        msTaken = GetMillisecondsFromStopWatchTicks(endTime - startTime);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Published in {msTaken:N3} ms");
                    }
                    catch (Exception error)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connection Error: ({_connection.LastError?.GetType()?.Name ?? "n/a"}) {_connection.LastError?.Message ?? "n/a"}");
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Local Error: ({error.GetType().Name}) {error.Message}");

                        var inner = error.InnerException;
                        var indention = "   ";
                        while(inner != null)
                        {
                            Console.WriteLine($"{indention} Inner Error: ({inner.GetType().Name}) {inner.Message}");
                            indention += "   ";
                            inner = inner.InnerException;
                        }

                        Console.ResetColor();
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (Exception error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(error.ToString());
                Console.ResetColor();
            }
            finally
            {
                _subscription?.Dispose();
                _connection?.Dispose();
            }

#if DEBUG
            Console.Write("Press any key to exit: ");
            Console.ReadKey();
#endif
        }
    }
}
