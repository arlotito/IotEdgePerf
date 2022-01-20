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

using CommandLine;
using Microsoft.Azure.Devices;

namespace IotEdgePerf.ConsoleApp
{
    partial class Program
    {

        private static string _eventHubConnectionString = "";
        private static string _eventHubName = "";
        private static double _timeoutInterval;

        private static System.Timers.Timer _timeout;

        private static bool _showAsaMessage = true;



        private static string _iotHubConnectionString = "";
        private static string _deviceId = "";

        private static IotEdgePerfService _iotEdgePerfService;
        private static TransmitterConfigData _transmitterConfigData;
        private static BurstAnalyzer _analyzer;

        private static RegistryManager _registryManager;
        private static ServiceClient _serviceClient;

        private static Guid _sessionId;
        private static string _csvFile;
        private static string _customLabel;

        public static async Task Main(string[] args)
        {
            
            // Set up a way for the user to gracefully shutdown
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\n\nExiting...");
            };

            // init command line options and verbs
            
            await CommandLine.Parser.Default.ParseArguments<DeployOptions, RunOptions>(args)
                    .MapResult(
                        (DeployOptions opts) => DeployModules(opts, cts),
                        (RunOptions opts) => RunPerformanceTest(opts, cts),
                        errs => Task.FromResult(0)
                        );
            
            
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


        private static async Task DeployModules(DeployOptions opts, CancellationTokenSource ct)
        {
            _iotHubConnectionString = Environment.GetEnvironmentVariable("IOT_CONN_STRING");
            bool addingLogAnalytics = false;
            if (!string.IsNullOrEmpty(opts.IotHubConnectionString))
            {
                _iotHubConnectionString = opts.IotHubConnectionString;
            }

            _deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");
            if (!string.IsNullOrEmpty(opts.DeviceId))
            {
                _deviceId = opts.DeviceId;
            }

            if (!string.IsNullOrWhiteSpace(opts.LogAnalyticsWorkspaceId))
            {
                addingLogAnalytics = true; 
                //check additionally if the other related values are provided:
                if(string.IsNullOrWhiteSpace(opts.LogAnalyticsSharedKey) 
                    || string.IsNullOrWhiteSpace(opts.LogAnalyticsIoTResourceId))
                    {
                        Console.WriteLine($"ERROR: You have provided a Log Analytics Workspace ID but missing either IoT Hub resource id or Shared key.\nExiting...\n");
                        Environment.Exit(1);
                    }
                
            }
            
            _registryManager = RegistryManager.CreateFromConnectionString(_iotHubConnectionString);

            ConfigurationContent deploymentManifest = EdgeConfigurations.GetBaseConfigurationContent(
                opts.ImageUri,
                opts.MaxUpstreamBatchSize,
                opts.LogAnalyticsWorkspaceId,
                opts.LogAnalyticsSharedKey,
                opts.LogAnalyticsIoTResourceId,
                addingLogAnalytics
            );

            await _registryManager.ApplyConfigurationContentOnDeviceAsync(_deviceId, deploymentManifest);
            Console.WriteLine("Deploy Modules - Applied configuration (deployment manfiest applied)");

            //restart edgeAgent
            _serviceClient = ServiceClient.CreateFromConnectionString(_iotHubConnectionString);

            var deviceMethod = new CloudToDeviceMethod("RestartModule");
            deviceMethod.ResponseTimeout = TimeSpan.FromSeconds(30);

            var json = new 
            {
                schemaVersion = "1.0",
                id = "edgeHub"
            };
            deviceMethod.SetPayloadJson(JsonConvert.SerializeObject(json));

            try
            {
                var response = await _serviceClient.InvokeDeviceMethodAsync(_deviceId, "$edgeAgent", deviceMethod);
                Console.WriteLine($"Deploy Modules - Restart module: {response.Status}");
            }
            catch(System.Exception e)
            {
                Console.WriteLine($"Deploy Modules - could not restart edgeAgent: {e.Message}");
            }
            
            Console.WriteLine("\nFinished with deployment.");

        }

        private static async Task RunPerformanceTest(RunOptions opts, CancellationTokenSource ct)
        {
            //==== Initialize
            _eventHubName = Environment.GetEnvironmentVariable("EH_NAME");
            if (!string.IsNullOrEmpty(opts.EventHubName))
            {
                _eventHubName = opts.EventHubName;
            }

            _eventHubConnectionString = Environment.GetEnvironmentVariable("EH_CONN_STRING");
            if (!string.IsNullOrEmpty(opts.EventHubConnectionString))
            {
                _eventHubConnectionString = opts.EventHubConnectionString;
            }

            _iotHubConnectionString = Environment.GetEnvironmentVariable("IOT_CONN_STRING");
            if (!string.IsNullOrEmpty(opts.IotHubConnectionString))
            {
                _iotHubConnectionString = opts.IotHubConnectionString;
            }

            _deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");
            if (!string.IsNullOrEmpty(opts.DeviceId))
            {
                _deviceId = opts.DeviceId;
            }

            // check if EH info is provided
            if (string.IsNullOrWhiteSpace(_eventHubConnectionString))
            {
                Console.WriteLine($"ERROR: _eventHubConnectionString not found.\n\n");
                //Console.WriteLine(CommandLine.Text.HelpText..AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            if (string.IsNullOrWhiteSpace(_eventHubName))
            {
                Console.WriteLine($"ERROR: _eventHubName not found.\n\n");
                //Console.WriteLine(CommandLine.Text.HelpText.AutoBuild(result, null, null));
                Environment.Exit(1);
            }

            double.TryParse(opts.Timeout, out _timeoutInterval);
            _showAsaMessage = opts.ShowMsg;
            _csvFile = opts.csvOutputFile;
            _customLabel = opts.TestLabel;
            
            _transmitterConfigData = new TransmitterConfigData {
                autoStart = false,
                burstLength=opts.burstLength,
                burstWait=opts.burstWait,
                burstNumber=opts.burstNumber,
                targetRate=opts.targetRate,
                payloadLength=opts.payloadLength,
                batchSize=opts.batchSize,
                logMsg=false,
                logBurst=true,
                logHist=false,
                waitBeforeStart=opts.waitBeforeStart,
                rateCalcPeriod=5000
            };


            //===== RUN
            _sessionId = Guid.NewGuid();

            // start timeout
            SetTimeout(_timeoutInterval, ct);

            if (
                !String.IsNullOrEmpty(_iotHubConnectionString)
                && !String.IsNullOrEmpty(_deviceId)
            )
            {
                // Create a IotEdgePerfService
                _iotEdgePerfService = new IotEdgePerfService(
                    _iotHubConnectionString, 
                    _deviceId, 
                    "source"    //module name
                );

                // Apply config
                await _iotEdgePerfService.Start(_sessionId, _transmitterConfigData);
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
            await ReceiveMessagesFromDeviceAsync(ct.Token);

            Console.WriteLine("\nCloud message reader finished.");
        }

        private static void OnTimeout(CancellationTokenSource cts)
        {
            Console.WriteLine("\nTimeout elapsed.");
            Console.WriteLine("Analysis may be incomplete.");
            cts.Cancel();

            _analyzer.DoAnalysis(_sessionId.ToString());
            return;

        }

        // Asynchronously create a PartitionReceiver for a partition and then start
        // reading any messages sent from the simulated client.
        private static async Task ReceiveMessagesFromDeviceAsync(CancellationToken ct)
        {
            //DateTime discardBefore = DateTime.Now;

            int discarded = 0;

            await using var consumer = new EventHubConsumerClient(
                    EventHubConsumerClient.DefaultConsumerGroupName,
                    _eventHubConnectionString,
                    _eventHubName);

            Console.WriteLine("Listening for messages on all partitions.");
            Console.WriteLine($"Reading events (timeout={_timeoutInterval}ms)... ctrl-C to exit.\n");

            Console.WriteLine("");

            int expectedMessageCount = _transmitterConfigData.burstLength * _transmitterConfigData.burstNumber * _transmitterConfigData.burstNumber;

            try
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(false, null, ct)) //starts reading new events only
                {
                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    string[] lines = data.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        try
                        {
                            var msg = JsonConvert.DeserializeObject<AsaMessage>(line);

                            // message received. restart timeout
                            _timeout.Stop();
                            _timeout.Start();

                            // add message for later analysis
                            int? count = _analyzer.AddMessage(msg, _sessionId.ToString());

                            if (_showAsaMessage)
                                Console.WriteLine($"Received: {line}");
                            else
                            {
                                // show progress
                                if (count != null)
                                {
                                    double percentage = ((double)count / expectedMessageCount) * 100;
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine($"{percentage:000.0}% - {count}/{expectedMessageCount} - discarded: {discarded}");
                                }
                                else
                                {
                                    discarded++;

                                }
                            }

                            if (count == expectedMessageCount)
                            {
                                Console.WriteLine("\nAll expected messages have been received. Completed.\n \n");
                                _analyzer.DoAnalysis(_sessionId.ToString());
                                return;
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
