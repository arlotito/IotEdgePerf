

using System;
using System.Collections.Generic;
using System.Text;

using System.Timers;

using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Toolbox;

using Azure.Messaging.EventHubs.Consumer;

using CommandLine;


namespace eh_consumer
{
    class Program
    {
        
        private static string EventHubConnectionString = "";
        private static string EventHubName = "";
        private static double TimeoutInterval;

        private static System.Timers.Timer timeout;

        private static void GetConfig(string[] args)
        {
            Parameters _parameters = new Parameters();

            // Parse application parameters
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    _parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            EventHubName = Environment.GetEnvironmentVariable("EH_NAME");
            if (!string.IsNullOrEmpty(_parameters.EventHubName))
            {
                EventHubName = _parameters.EventHubName;         
            }

            EventHubConnectionString = Environment.GetEnvironmentVariable("EH_CONN_STRING");
            if (!string.IsNullOrEmpty(_parameters.EventHubConnectionString))
            {
                EventHubConnectionString = _parameters.EventHubConnectionString;         
            }

            // check if EH info is provided
            if (string.IsNullOrWhiteSpace(EventHubConnectionString)
                || string.IsNullOrWhiteSpace(EventHubName))
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }
            
            double.TryParse(_parameters.Timeout, out TimeoutInterval);
        }
        
        public static async Task Main(string[] args)
        {
            GetConfig(args);            

            Console.WriteLine("Reading events... Ctrl-C to exit.\n");

            // Set up a way for the user to gracefully shutdown
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\n\nExiting...");
            };

            // start timeout
            SetTimeout(TimeoutInterval, cts);

            // Run the sample
            await ReceiveMessagesFromDeviceAsync(cts.Token);

            Console.WriteLine("Cloud message reader finished.");
        }

        private static void SetTimeout(double interval, CancellationTokenSource cts)
        {
            // Create a timer with a two second interval.
            timeout = new System.Timers.Timer(interval);
            // Hook up the Elapsed event for the timer. 
            timeout.Elapsed += (sender, args) => OnTimeout(cts);
            timeout.AutoReset = false;
            timeout.Enabled = true;
        }

        private static void OnTimeout(CancellationTokenSource cts)
        {
            Console.WriteLine("Timeout elapsed.");
            cts.Cancel();
        }

        // Asynchronously create a PartitionReceiver for a partition and then start
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(CancellationToken ct)
        {
            await using var consumer = new EventHubConsumerClient(
                    EventHubConsumerClient.DefaultConsumerGroupName,
                    EventHubConnectionString,
                    EventHubName);

            Console.WriteLine("Listening for messages on all partitions.");

            bool logRaw = false;
            
            try
            {
                StatsCalculator statsRate = new StatsCalculator();
                StatsCalculator statsLatency = new StatsCalculator();

                DateTime startFromTime = DateTime.Now;
                DateTime firstMessageDT = new DateTime();
                DateTime lastMessageDT = new DateTime();
                
                //Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"));

                int count = 0;

                Console.WriteLine("timestamp,counter,total,messagesCount,asaEstimatedRate,asaAvgLatency,asaMinLatency,asaMaxLatency,statsAvgRate,statsMinRate,statsMaxRate");

                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(ct))
                {
                    //Console.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId}:");

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    
                    if (logRaw)
                        Console.WriteLine(data);

                    var msg = JsonConvert.DeserializeObject<AsaJob.Message>(data);

                    //DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")

                    //do not show old messages
                    DateTime t = DateTime.Parse(msg.t);

                    if (t >= startFromTime)
                    {
                        if (count == 0)
                        {
                            firstMessageDT = DateTime.Parse(msg.firstMsgTs);
                            //Console.WriteLine($"** First message ts: {firstMessageDT.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                        }

                        count++;
                        timeout.Stop();
                        timeout.Start();
                        
                        statsRate.Append(msg.estimatedRate);
                        statsLatency.Append(msg.avgLatency);

                        Console.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}", 
                            t.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"),
                            msg.counter, msg.total, msg.messagesCount,
                            msg.estimatedRate, 
                            msg.avgLatency, msg.minLatency, msg.avgLatency,
                            statsRate.avg, statsRate.min, statsRate.max);

                        //Console.WriteLine($"First message ts: {DateTime.Parse(msg.firstMsgTs).ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                        //Console.WriteLine($"Last message ts: {DateTime.Parse(msg.lastMsgTs).ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                    }

                    // test completed: stop listening if all messages has been received
                    if ( (msg.counter == msg.total) && (statsRate.elementsNum > 0) )
                    {
                        timeout.Stop();

                        lastMessageDT = DateTime.Parse(msg.lastMsgTs);
                        TimeSpan delta = lastMessageDT - firstMessageDT;
                        double averageRate = msg.total / delta.TotalSeconds;

                        Console.WriteLine("\n------\nTest completed. All messages received.\n");

                        Console.WriteLine($"Run ID: {msg.runId}");
                        Console.WriteLine($"First message ts: {firstMessageDT.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                        Console.WriteLine($"Last message ts: {lastMessageDT.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                        Console.WriteLine($"Delta ts [s]: {delta.TotalSeconds}");
                        Console.WriteLine($"Total messages: {msg.total}");
                        Console.WriteLine($"Avg rate [msg/s]: {averageRate:0.00}");
                        //Console.WriteLine($"Avg (min/max) rate [msg/s]: {statsRate.avg:0.00} ({statsRate.min:0.00},{statsRate.max:0.00})");
                        Console.WriteLine($"Avg (min/max) latency [ms]: {statsLatency.avg:0.00} ({statsLatency.min:0.00},{statsLatency.max:0.00})");
                        return;
                    }
                }
            }

            catch (TaskCanceledException)
            {
                // This is expected when the token is signaled; it should not be considered an
                // error in this scenario.
                return;
            }
        }

        private static void PrintProperties(KeyValuePair<string, object> prop)
        {
            string propValue = prop.Value is DateTime
                ? ((DateTime)prop.Value).ToString("O") // using a built-in date format here that includes milliseconds
                : prop.Value.ToString();

            Console.WriteLine($"\t\t{prop.Key}: {propValue}");
        }
    }
}
