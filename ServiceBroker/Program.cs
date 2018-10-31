using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace ServiceBroker
{
    public class Program
    {
        private static readonly object _lock = new object();
        private static readonly ManualResetEvent _waitSignal = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.local.json", optional: true);

            var configuration = builder.Build();
            string connectionString = configuration.GetConnectionString("SbTest");

            var rabbitSettings = configuration.GetSection("rabbit");
            string rabbitHost = rabbitSettings.GetValue<string>("host");
            string rabbitUser = rabbitSettings.GetValue<string>("user");
            string rabbitPass = rabbitSettings.GetValue<string>("pass");

            int line;

            lock (_lock)
            {
                Console.WriteLine("Async processing test (consecutive):");

                line = Console.CursorTop;
            }

            using (var test = new AsyncProcessingTest(connectionString, connectionString, 15))
            {
                test.UpdateStatistics += (s, stats) => OnUpdateStatistics(line, stats);
                test.Finished += (s, e) =>
                {
                    Console.WriteLine("Items processed: " + e);
                    _waitSignal.Set();
                };

                _waitSignal.Reset();
                test.Start();
                _waitSignal.WaitOne();
            }

            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("Concurrent read/write test:");

                line = Console.CursorTop;
            }

            using (var test = new ConcurrentReadWriteTest(connectionString, connectionString, 5000))
            {
                test.UpdateStatistics += (s, stats) => OnUpdateStatistics(line, stats);
                test.Finished += (s, e) => _waitSignal.Set();

                _waitSignal.Reset();
                test.Start();
                _waitSignal.WaitOne();
            }

            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("Consectuive read/write test:");

                line = Console.CursorTop;
            }

            using (var test = new ConsecutiveReadWriteTest(connectionString, connectionString, 5000))
            {
                test.UpdateStatistics += (s, stats) => OnUpdateStatistics(line, stats);
                test.Finished += (s, e) => _waitSignal.Set();

                _waitSignal.Reset();
                test.Start();
                _waitSignal.WaitOne();
            }
            
            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("Multiple readers (3) test:");

                line = Console.CursorTop;
            }

            using (var test = new MultipleReadersTest(connectionString, connectionString, 3, 15000))
            {
                test.UpdateStatistics += (s, stats) => OnUpdateStatistics(line, stats);
                test.Finished += (s, e) => _waitSignal.Set();

                _waitSignal.Reset();
                test.Start();
                _waitSignal.WaitOne();
            }

            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("Multiple readers (5) test:");

                line = Console.CursorTop;
            }

            using (var test = new MultipleReadersTest(connectionString, connectionString, 5, 25000))
            {
                test.UpdateStatistics += (s, stats) => OnUpdateStatistics(line, stats);
                test.Finished += (s, e) => _waitSignal.Set();

                _waitSignal.Reset();
                test.Start();
                _waitSignal.WaitOne();
            }

            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("Multiple readers (10) test:");

                line = Console.CursorTop;
            }

            using (var test = new MultipleReadersTest(connectionString, connectionString, 10, 25000))
            {
                test.UpdateStatistics += (s, stats) => OnUpdateStatistics(line, stats);
                test.Finished += (s, e) => _waitSignal.Set();

                _waitSignal.Reset();
                test.Start();
                _waitSignal.WaitOne();
            }

            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("Detecting concurrency conflicts:");

                line = Console.CursorTop;
            }

            using (var test = new ConcurrencyProblemTest(connectionString, connectionString, 5, 5000))
            {
                test.UpdateStatistics += (s, stats) => OnUpdateStatistics(line, stats);
                test.Finished += (s, e) =>
                {
                    Console.WriteLine("Number of conflicts: " + e);
                    _waitSignal.Set();
                };

                _waitSignal.Reset();
                test.Start();
                _waitSignal.WaitOne();
            }

            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("Rabbit MQ test:");

                line = Console.CursorTop;
            }

            using (var test = new RabbitMqTest(rabbitHost, rabbitUser, rabbitPass))
            {
                var time = test.Execute(5000);
                Console.WriteLine($"RabbitMQ took {time.TotalSeconds.ToString("N0")} seconds to read 5000 messages.");
            }

            lock (_lock)
            {
                Console.WriteLine();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }
        }

        private static void OnUpdateStatistics(int line, string stats)
        {
            lock (_lock)
            {
                Console.SetCursorPosition(0, line);

                string emptyLine = new string(' ', Console.WindowWidth);
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine(emptyLine);
                }

                Console.SetCursorPosition(0, line);
                Console.WriteLine(stats);
            }
        }
    }
}
