using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Azure.Messaging.EventHubs.Consumer;

using IotEdgePerf.Service;
using IotEdgePerf.Shared;
using IotEdgePerf.Analysis;
using IotEdgePerf.Transmitter.ConfigData;

namespace IotEdgePerf.ConsoleApp
{
    partial class Program
    {

        private static string _eventHubConnectionString = "";
        private static string _eventHubName = "";
        private static double _timeoutInterval;

        private static System.Timers.Timer _timeout;

        private static bool _showMsg = true;



        private static string _iotHubConnectionString = "";
        private static string _deviceId = "";

        private static IotEdgePerfService _iotEdgePerfService;
        private static TransmitterConfigData _transmitterConfigData;
        private static BurstAnalyzer _analyzer;

        private static string _csvFile;
        private static string _customLabel;

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
            SetTimeout(_timeoutInterval, cts);

            if (
                !String.IsNullOrEmpty(_iotHubConnectionString)
                && !String.IsNullOrEmpty(_deviceId)
            )
            {
                // Create a ServiceClient to communicate with service-facing endpoint on your hub.
                _iotEdgePerfService = new IotEdgePerfService(_iotHubConnectionString, _deviceId, "source");

                // Apply config
                await _iotEdgePerfService.Start(Guid.NewGuid(), _transmitterConfigData);
            }
            else
            {
                Console.WriteLine("ERROR: IotHubConnectionString and/or DeviceId are empty.");
                return;
            }

            // HostName=arturol76-s1-benchmark.a
            _analyzer = new BurstAnalyzer(
                _iotHubConnectionString.Split('.')[0].Replace("HostName=", ""),
                _deviceId,
                _transmitterConfigData,
                _csvFile,
                _customLabel
            ); ;

            // listens to EH messages
            await ReceiveMessagesFromDeviceAsync(cts.Token);

            Console.WriteLine("\nCloud message reader finished.");
        }

        private static void SetTimeout(double interval, CancellationTokenSource cts)
        {
            // Create a timer with a two second interval.
            _timeout = new System.Timers.Timer(interval);
            // Hook up the Elapsed event for the timer. 
            _timeout.Elapsed += (sender, args) => OnTimeout(cts);
            _timeout.AutoReset = false;
            _timeout.Enabled = true;
        }

        private static void OnTimeout(CancellationTokenSource cts)
        {
            Console.WriteLine("\nTimeout elapsed.");
            Console.WriteLine("Analysis may be incomplete.");
            cts.Cancel();

            _analyzer.DoAnalysis();
            return;

        }

        // Asynchronously create a PartitionReceiver for a partition and then start
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(CancellationToken ct)
        {
            DateTime discardBefore = DateTime.Now;

            await using var consumer = new EventHubConsumerClient(
                    EventHubConsumerClient.DefaultConsumerGroupName,
                    _eventHubConnectionString,
                    _eventHubName);

            //Console.WriteLine($"Discarding messages before {discardBefore.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}\n");

            Console.WriteLine("Listening for messages on all partitions.");
            Console.WriteLine($"Reading events (timeout={_timeoutInterval}ms)... ctrl-C to exit.\n");

            Console.WriteLine("");

            int expectedMessageCount = _transmitterConfigData.burstLength * _transmitterConfigData.burstNumber * _transmitterConfigData.burstNumber;
            
            try
            {
                //Console.WriteLine("timestamp,counter,total,messagesCount,asaEstimatedRate,asaAvgLatency,asaMinLatency,asaMaxLatency,statsAvgRate,statsMinRate,statsMaxRate");

                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(ct))
                {
                    //Console.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId}:");

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    string[] lines = data.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        try
                        {
                            var msg = JsonConvert.DeserializeObject<AsaMessage>(line);

                            //do not show old messages
                            DateTime t = DateTime.Parse(msg.t);

                            if (DateTime.Compare(t, discardBefore) > 0)
                            {
                                _timeout.Stop();
                                _timeout.Start();

                                // add for analysis
                                double count = _analyzer.AddMessage(msg);

                                if (_showMsg)
                                    Console.WriteLine($"Received: {line}");
                                else
                                {
                                    double percentage = (count / expectedMessageCount) * 100;
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine($"{percentage:000.0}% - {count}/{expectedMessageCount}");
                                }

                                if (count == expectedMessageCount)
                                {
                                    Console.WriteLine("\nAll expected messages have been received. Completed.\n \n");
                                    _analyzer.DoAnalysis();
                                    return;
                                }
                            }
                            else
                            {
                                // discarded
                            }
                        }

                        catch (Newtonsoft.Json.JsonReaderException e)
                        {
                            Console.WriteLine($"JsonReaderException error on: {data}");
                            Console.WriteLine($"{e}");
                            Console.WriteLine($"{e.Message}");
                        }

                        catch (Newtonsoft.Json.JsonSerializationException e)
                        {
                            Console.WriteLine($"JsonSerializationException error on: {data}");
                            Console.WriteLine($"{e}");
                            Console.WriteLine($"{e.Message}");
                        }
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
