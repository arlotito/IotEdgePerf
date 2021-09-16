namespace edgeBenchmark
{
    using System;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;

    using Prometheus;
  
    class Program
    {
        static int counter;
        static bool logMsg = false, logBody = false, echo = false;

        static int ratePeriod;

        static RateMeter rateMeter;
        static double now = 0;

        static string ModuleOutput = "output1";

        static string EnvDeviceId,EnvHub,inputsCsv;
        static string[] inputs;

        

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("MyHandler caught : " + e.Message);
            Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        }

        static async Task Main()
        {
            var server = new MetricServer(port: 9600);
            server.Start();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            logMsg = Boolean.Parse(Environment.GetEnvironmentVariable("LOG_MSG"));
            logBody = Boolean.Parse(Environment.GetEnvironmentVariable("LOG_BODY"));
            echo = Boolean.Parse(Environment.GetEnvironmentVariable("ECHO"));
            ratePeriod = Int32.Parse(Environment.GetEnvironmentVariable("PERIOD")); //ms
            inputsCsv = Environment.GetEnvironmentVariable("INPUT_CSV");

            inputs = inputsCsv.Trim().ToLower().Split(',');
            
            Console.WriteLine($"Log messages: {logMsg.ToString()}");
            Console.WriteLine($"Log body: {logBody.ToString()}");
            Console.WriteLine($"Period: {ratePeriod.ToString()} ms");
            Console.WriteLine($"Echo: {echo.ToString()}");
            Console.WriteLine($"Inputs: {String.Join(",", inputs)}");

            var moduleClient = await CreateModuleClient();

            EnvDeviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            EnvHub = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");
            Console.WriteLine($"Device id: '{EnvDeviceId}'");
            Console.WriteLine($"IoT HUB: '{EnvHub}'");

            Console.WriteLine("--------------------------------");
            Console.WriteLine("");
            
            // rate meter
            rateMeter = new RateMeter(ratePeriod, inputs);
            foreach (string input in inputs)
            {
                Console.WriteLine("adding input '{0}'...", input);
                await moduleClient.SetInputMessageHandlerAsync(input, (msg, ctx) => OnIncomingMessage(msg, moduleClient), null);
            }            

            // Wait until the app unloads
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        private static async Task<ModuleClient> CreateModuleClient()
        {
            MqttTransportSettings mqttSettings = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSettings };
            var moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await moduleClient.OpenAsync();
            return moduleClient;
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> OnIncomingMessage(Message message, ModuleClient moduleClient)
        {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            int counterValue = Interlocked.Increment(ref counter);
            
            // message.InputName
            rateMeter.Update(counterValue, message.InputName);

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            
            MessageDataPoint dataPoint = JsonConvert.DeserializeObject<MessageDataPoint>(messageString);

            long latencyMs = now - dataPoint.ts;

            if (logMsg)
            {
                Console.Write("{0},input={1},counter={2},latencyMs={3},sizeBytes={4}",
                    DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"),
                    message.InputName,
                    counterValue,
                    latencyMs,
                    messageBytes.Length);

                if (logBody)
                {
                    Console.Write(",Body={0}", messageString);
                }

                Console.WriteLine();
            }

            // echo message
            if (echo)
            {
                var echoMsg = new Message(Encoding.ASCII.GetBytes(messageString));
                await moduleClient.SendEventAsync(ModuleOutput, echoMsg);
            }
            
            return MessageResponse.Completed;
        }
    }
}
