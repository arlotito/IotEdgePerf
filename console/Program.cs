

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Azure.Messaging.EventHubs.Consumer;

using CommandLine;

using Microsoft.Azure.Devices;


namespace eh_consumer
{
    class Program
    {

        private static string EventHubConnectionString = "";
        private static string EventHubName = "";
        private static double TimeoutInterval;

        private static System.Timers.Timer timeout;

        private static bool ShowMsg;

        static List<AsaJob.Message> MessagesList = new List<AsaJob.Message>();

        private static ServiceClient serviceClient;
        private static string IotHubConnectionString = "";
        private static string DeviceId ="";

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

            IotHubConnectionString = Environment.GetEnvironmentVariable("IOT_CONN_STRING");
            if (!string.IsNullOrEmpty(_parameters.IotHubConnectionString))
            {
                IotHubConnectionString = _parameters.IotHubConnectionString;
            }

            // check if EH info is provided
            if (string.IsNullOrWhiteSpace(EventHubConnectionString)
                || string.IsNullOrWhiteSpace(EventHubName))
            {
                Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            double.TryParse(_parameters.Timeout, out TimeoutInterval);
            ShowMsg = _parameters.ShowMsg;
            DeviceId=_parameters.DeviceId;
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

            // Create a ServiceClient to communicate with service-facing endpoint on your hub.
            serviceClient = ServiceClient.CreateFromConnectionString(IotHubConnectionString);
            await InvokeReset(DeviceId);

            // listens to EH messages
            await ReceiveMessagesFromDeviceAsync(cts.Token);

            Console.WriteLine("\nCloud message reader finished.");
        }

        // Invoke the direct method on the device, passing the payload
        private static async Task InvokeReset(string deviceId)
        {
            var methodInvocation = new CloudToDeviceMethod("Reset")
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };
            //methodInvocation.SetPayloadJson("");

            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, "source", methodInvocation);

            Console.WriteLine($"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
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
            Console.WriteLine("\nTimeout elapsed.");
            cts.Cancel();

            Analyze();
        }

        private static void Analyze()
        {
            bool logBurstDetails = false;
            
            // order by asa timestamp
            MessagesList.OrderBy(msg => msg.t);

            // group by burstCounter
            var query = from item in MessagesList
                            group item by item.burstCounter into burstGroup
                            orderby burstGroup.Key ascending
                            select burstGroup;
            
            // perform analysis
            foreach (var burstGroup in query)
            {
                var burstLength = burstGroup.Last().asaBurstLength;
                
                // extracts measurement done by ASA
                double asaEstimatedRateIotHub = burstGroup.Last().asaEstimatedRateIotHub; 

                // gets first and last SOURCE timestamp
                var firstMessageDtInBurst = DateTime.Parse(burstGroup.First().firstMsgTs);
                var lastMessageDtInBurst = DateTime.Parse(burstGroup.Last().lastMsgTs);
                TimeSpan burstDurationSource = lastMessageDtInBurst - firstMessageDtInBurst;
                double averageRateInBurstSource = burstLength / burstDurationSource.TotalSeconds;

                // gets first and last IoT HUB enqueuement timestamp (unix epoch)
                var firstIotHubEpoch = burstGroup.First().firstIotHubEpoch;
                var lastIotHubEpoch = burstGroup.Last().lastIotHubEpoch;
                double burstDurationIotHub = (lastIotHubEpoch - firstIotHubEpoch) / 1000;
                double averageRateInBurstIotHub = burstLength / burstDurationIotHub;

                // stats on latency
                double avgLatency = burstGroup.Average(item => item.asaAvgLatency);
                double minLatency = burstGroup.Min(item => item.asaAvgLatency);
                double maxLatency = burstGroup.Max(item => item.asaAvgLatency);

                Console.Write($"#: {burstGroup.Key},");
                Console.Write($"msg sent: {burstLength},");
                Console.Write($"source/iothub [msg/s]: {averageRateInBurstSource:0.00}/{averageRateInBurstIotHub:0.00},");
                Console.Write($"latency (avg/min/max) [ms]: {avgLatency:0.00}/{minLatency:0.00}/{maxLatency:0.00}");
                Console.WriteLine();

                if (logBurstDetails)
                {
                    // detailed
                    Console.WriteLine(string.Format("Run ID: {0}", burstGroup.Last().runId));
                    Console.WriteLine(string.Format("Burst ID: {0}", burstGroup.Key));
                    Console.WriteLine($"Total messages: {burstLength}");
                    
                    Console.WriteLine($"Source:");
                    Console.WriteLine($"    First message ts: {firstMessageDtInBurst.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                    Console.WriteLine($"    Last message ts: {lastMessageDtInBurst.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                    Console.WriteLine($"    Delta ts [s]: {burstDurationSource.TotalSeconds}");
                    Console.WriteLine($"    avg rate [msg/s]: {averageRateInBurstSource:0.00}");
                    Console.WriteLine("\n");
                    Console.WriteLine($"IoT HUB ingress:");
                    Console.WriteLine($"    First message ts: {firstIotHubEpoch}");
                    Console.WriteLine($"    Last message ts: {lastIotHubEpoch}");
                    Console.WriteLine($"    Delta ts [s]: {burstDurationIotHub:0.00}");
                    Console.WriteLine($"    avg rate ASA [msg/s]: {asaEstimatedRateIotHub:0.00}");
                    Console.WriteLine($"    avg rate [msg/s]: {averageRateInBurstIotHub:0.00}");
                    Console.WriteLine($"    avg (min/max) latency [ms]: {avgLatency:0.00} ({minLatency:0.00},{maxLatency:0.00})");
                    Console.WriteLine("\n------\n\n");
                }
                
            }
        }


        // Asynchronously create a PartitionReceiver for a partition and then start
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(CancellationToken ct)
        {
            DateTime discardBefore = DateTime.Now;
            
            await using var consumer = new EventHubConsumerClient(
                    EventHubConsumerClient.DefaultConsumerGroupName,
                    EventHubConnectionString,
                    EventHubName);

            Console.WriteLine("Listening for messages on all partitions.");
            Console.WriteLine($"Timeout: {TimeoutInterval} ms\n");
            Console.WriteLine($"Discarding messages before {discardBefore.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}\n");

            try
            {
                //Console.WriteLine("timestamp,counter,total,messagesCount,asaEstimatedRate,asaAvgLatency,asaMinLatency,asaMaxLatency,statsAvgRate,statsMinRate,statsMaxRate");

                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(ct))
                {
                    //Console.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId}:");

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());

                    try
                    {
                        var msg = JsonConvert.DeserializeObject<AsaJob.Message>(data);
                        
                        //do not show old messages
                        DateTime t = DateTime.Parse(msg.t);

                        if (DateTime.Compare(t, discardBefore) > 0)
                        {
                            timeout.Stop();
                            timeout.Start();

                            if (ShowMsg)
                                Console.WriteLine(data);
                            else
                                Console.Write(".");

                            // add for analysis
                            MessagesList.Add(msg);
                        }
                        else
                        {
                            // discarded
                        }
                    }

                    catch (Newtonsoft.Json.JsonReaderException)
                    {
                        //Console.WriteLine($"{e}");
                        //Console.WriteLine(data);
                        //Console.WriteLine($"{e.Message}");
                    }

                    catch (Newtonsoft.Json.JsonSerializationException)
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
