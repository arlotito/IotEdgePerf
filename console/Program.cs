

using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Azure.Messaging.EventHubs.Consumer;

using source;

namespace eh_consumer
{
    partial class Program
    {

        private static string EventHubConnectionString = "";
        private static string EventHubName = "";
        private static double TimeoutInterval;

        private static System.Timers.Timer timeout;

        private static bool ShowMsg;

        static List<AsaJob.Message> MessagesList = new List<AsaJob.Message>();

        private static string IotHubConnectionString = "";
        private static string DeviceId = "";

        private static SenderMachineService senderMachineService;
        private static SenderMachineConfigData senderMachineConfig;

        public static async Task Main(string[] args)
        {
            GetConfig(args);

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

            if (
                !String.IsNullOrEmpty(IotHubConnectionString)
                && !String.IsNullOrEmpty(DeviceId)
            )
            {
                // Create a ServiceClient to communicate with service-facing endpoint on your hub.
                senderMachineService = new SenderMachineService(IotHubConnectionString, DeviceId);

                // Apply config
                await senderMachineService.Start(Guid.NewGuid(), senderMachineConfig);
            }
          
            // listens to EH messages
            await ReceiveMessagesFromDeviceAsync(cts.Token);

            Console.WriteLine("\nCloud message reader finished.");
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

        // Asynchronously create a PartitionReceiver for a partition and then start
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(CancellationToken ct)
        {
            DateTime discardBefore = DateTime.Now;
            
            await using var consumer = new EventHubConsumerClient(
                    EventHubConsumerClient.DefaultConsumerGroupName,
                    EventHubConnectionString,
                    EventHubName);

            Console.WriteLine($"Discarding messages before {discardBefore.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}\n");

            Console.WriteLine("Listening for messages on all partitions.");
            Console.WriteLine($"Reading events (timeout={TimeoutInterval}ms)... ctrl-C to exit.\n");

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
