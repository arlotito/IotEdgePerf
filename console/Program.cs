

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

        private static bool ShowMsg;

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
            ShowMsg=_parameters.ShowMsg;
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

            try
            {
                StatsCalculator statsRate = new StatsCalculator();
                StatsCalculator statsLatency = new StatsCalculator();

                DateTime startFromTime = DateTime.Now;
                DateTime firstMessageDtInBurst = new DateTime();
                DateTime lastMessageDtInBurst = new DateTime();

                double firstIotHubEpoch = 0, lastIotHubEpoch = 0;
                
                //Console.WriteLine(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"));

                int count = 0;

                Console.WriteLine("timestamp,counter,total,messagesCount,asaEstimatedRate,asaAvgLatency,asaMinLatency,asaMaxLatency,statsAvgRate,statsMinRate,statsMaxRate");

                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(ct))
                {
                    //Console.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId}:");

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    
                    if (ShowMsg)
                        Console.WriteLine(data);

                    try
                    {
                        var msg = JsonConvert.DeserializeObject<AsaJob.Message>(data);

                        //DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")

                        //do not show old messages
                        DateTime t = DateTime.Parse(msg.t);

                        if (t >= startFromTime)
                        {
                            if (count == 0)
                            {
                                firstMessageDtInBurst = DateTime.Parse(msg.firstMsgTs);
                                firstIotHubEpoch = msg.firstIotHubEpoch;
                                // Console.WriteLine($"** First message ts: {msg.firstMsgTs}");
                                // Console.WriteLine($"** First message iothub ts: {msg.firstIotHubEpoch}");
                            }

                            count++;
                            timeout.Stop();
                            timeout.Start();
                            
                            statsRate.Append(msg.asaEstimatedRate);
                            statsLatency.Append(msg.asaAvgLatency);

                            Console.WriteLine(
                                $"{t.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")},"
                                + $"{msg.asaRunMsgTotal},"
                                + $"{msg.asaRunMsgCounter}," 
                                + $"{msg.asaRunElapsed},"
                                + $"{msg.burstCounter}," 
                                + $"{msg.asaBurstLength}," 
                                + $"{msg.asaBurstMsgCounter},"
                                + $"{msg.asaRunElapsed},"
                                + $"{msg.asaAvgLatency:0.0},"
                                + $"{msg.asaMinLatency:0.0},"
                                + $"{msg.asaMaxLatency:0.0},"
                                + $"{msg.asaEstimatedRate:0.0},"
                                + $"{msg.asaEstimatedRateIotHub:0.0},"
                                + $"{msg.asaEstimatedRateAsa:0.0},"
                                + $"{statsRate.avg:0.0}," 
                                + $"{statsRate.min:0.0},"
                                + $"{statsRate.max:0.0}"
                            );

                            //Console.WriteLine($"First message ts: {DateTime.Parse(msg.firstMsgTs).ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                            //Console.WriteLine($"Last message ts: {DateTime.Parse(msg.lastMsgTs).ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                        }

                        // BURST completed
                        if ( (msg.asaBurstMsgCounter == msg.asaBurstLength) && (count > 0) )
                        {
                            lastMessageDtInBurst = DateTime.Parse(msg.lastMsgTs);
                            lastIotHubEpoch = msg.lastIotHubEpoch;

                            TimeSpan burstDurationSource = lastMessageDtInBurst - firstMessageDtInBurst;
                            double burstDurationIotHub = (lastIotHubEpoch - firstIotHubEpoch) / 1000;

                            double averageRateInBurstSource = msg.asaBurstLength / burstDurationSource.TotalSeconds;
                            double averageRateInBurstIotHub = msg.asaBurstLength / burstDurationIotHub;



                            Console.WriteLine("\n------\nBURST completed. All messages received.\n");

                            Console.WriteLine($"Run ID: {msg.runId}");
                            Console.WriteLine($"Burst counter: {msg.burstCounter}");
                            Console.WriteLine($"Total messages: {msg.asaBurstLength}");
                            
                            Console.WriteLine($"Source:");
                            Console.WriteLine($"    First message ts: {firstMessageDtInBurst.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                            Console.WriteLine($"    Last message ts: {lastMessageDtInBurst.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                            Console.WriteLine($"    Delta ts [s]: {burstDurationSource.TotalSeconds}");
                            Console.WriteLine($"    Total messages: {msg.asaBurstLength}");
                            Console.WriteLine($"    avg rate [msg/s]: {averageRateInBurstSource:0.00}");
                            
                            Console.WriteLine($"IoT HUB ingress:");
                            Console.WriteLine($"    First message ts: {firstIotHubEpoch}");
                            Console.WriteLine($"    Last message ts: {lastIotHubEpoch}");
                            Console.WriteLine($"    Delta ts [s]: {burstDurationIotHub:0.00}");
                            Console.WriteLine($"    Total messages: {msg.asaBurstLength}");
                            Console.WriteLine($"    avg rate ASA [msg/s]: {msg.asaEstimatedRateIotHub:0.00}");
                            Console.WriteLine($"    avg rate [msg/s]: {averageRateInBurstIotHub:0.00}");
                            Console.WriteLine($"    avg (min/max) latency [ms]: {statsLatency.avg:0.00} ({statsLatency.min:0.00},{statsLatency.max:0.00})");
                            
                            return;
                        }

                        // test completed: stop listening if all messages has been received
                        if ( (msg.asaRunMsgCounter == msg.asaRunMsgTotal) && (statsRate.elementsNum > 0) )
                        {
                            timeout.Stop();

                            Console.WriteLine("\n------\nTest completed. All bursts received.\n");

                            return;
                        }
                    }

                    catch (Newtonsoft.Json.JsonReaderException e)
                    {
                        //Console.WriteLine($"{e}");
                        //Console.WriteLine(data);
                        //Console.WriteLine($"{e.Message}");
                    }

                    catch (Newtonsoft.Json.JsonSerializationException e)
                    {
                        //Console.WriteLine($"{e}");
                        //Console.WriteLine(data);
                        //Console.WriteLine($"{e.Message}");
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
