namespace source
{
    using System;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Newtonsoft.Json;

    using System.Diagnostics;

    using Prometheus;
    using edgeBenchmark;

    public class SourceOptions
    {
        public int burstLength;
        public int burstWait;
        public int burstNumber;
        public int targetRate;
        public int payloadLength;
        public bool logMsg;
        public bool logBurst;
        public bool logHist;

        public int waitBeforeStart;

        public int rateCalcPeriod;

        string GetEnv(string name, string description)
        {
            string value = Environment.GetEnvironmentVariable(name);

            if (value == null)
            {
                Console.WriteLine("The required environment variable '{0}' was not found!", name);
                throw new System.ArgumentNullException();
            }
            else
            {
                Console.WriteLine($"{name}, {description}: {value}");
                return value;
            }

        }

        public void GetFromEnv()
        {
            Console.WriteLine("");
            Console.WriteLine("Settings from ENV variables:");
            burstLength = Int32.Parse(GetEnv("BURST_LENGTH", "Burst length [msg]"));
            burstWait = Int32.Parse(GetEnv("BURST_WAIT", "Delay between bursts [ms]"));
            burstNumber = Int32.Parse(GetEnv("BURST_NUMBER", "Burst number [burst]"));
            targetRate = Int32.Parse(GetEnv("TARGET_RATE", "Target rate [msg/s]"));
            payloadLength = Int32.Parse(GetEnv("MESSAGE_PAYLOAD_LENGTH", "Message payload length [bytes]"));
            logMsg = Boolean.Parse(GetEnv("LOG_MSG", "Log each message"));
            logBurst = Boolean.Parse(GetEnv("LOG_BURST", "Log stats of each burst"));
            logHist = Boolean.Parse(GetEnv("LOG_HIST", "Log histograms"));

            waitBeforeStart = Int32.Parse(GetEnv("START_WAIT", "Wait before starting [ms]"));
            rateCalcPeriod = Int32.Parse(GetEnv("RATE_CALC_PERIOD", "Calculation period of rate [ms]")); //ms
        }
    }

    class Program
    {
        static ModuleClient IoTHubModuleClient;
        static string ModuleOutput = "output1";

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
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

        static private async Task SendMessages(
            ModuleClient moduleClient,
            SourceOptions options,
            RateMeter rateMeter,
            StatsCalculator stats,
            string EnvDeviceId)
        {
            Stopwatch stopwatch = new Stopwatch();
            Stopwatch burstStopwatch = new Stopwatch();

            int msgCnt = 0;
            int burstCnt = 0;

            double waitBeforeNextMessage = 0; //if 0, there won't be any delay
            if (options.targetRate > 0)
                waitBeforeNextMessage = 1 / (double)options.targetRate * 1000;
            Console.WriteLine($"(calculated) wait between messages: {waitBeforeNextMessage} ms");

            Console.WriteLine();
            Console.WriteLine();

            burstStopwatch.Restart();

            if (options.burstLength == 0)
            {
                Console.WriteLine("Burst length is 0. Nothing to do.");
                return;
            }

            if (options.burstNumber == 0)
            {
                Console.WriteLine("Burst number is 0. Nothing to do.");
                return;
            }

            while (true)
            {
                var start = burstStopwatch.Elapsed.TotalMilliseconds; // for rate adjustment
                var waitUntil = start + waitBeforeNextMessage;

                if (msgCnt == 0)
                    Console.WriteLine("{0},text,sending burst...", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"));

                // IMPORTANT: do not move this outside of the loop, otherwise you'll get an exception
                var payload = new MessageDataPoint
                {
                    ts = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                    counter = msgCnt + 1,
                    total = options.burstLength,
                    payload = RandomString(options.payloadLength)
                };
                string messageString = "";
                messageString = JsonConvert.SerializeObject(payload);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // An IoT hub can filter on these properties without access to the message body.  
                //message.Properties.Add("<key>", value);

                // Send the telemetry message
                stopwatch.Restart();
                //Console.WriteLine($"sending packet: {counter}, inc/burst: {packet}/{burst}, size: {size}, delay ms before next: {interpacket_interval_ms}, delay ms vs expected: {delay_ms}...");
                await moduleClient.SendEventAsync(ModuleOutput, message);
                msgCnt++;
                stopwatch.Stop();

                double elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds; //this has the same precision as ElapsedTicks
                stats.Update(elapsedMilliseconds);

                double burstElapsedMilliseconds = burstStopwatch.Elapsed.TotalMilliseconds;

                rateMeter.Update(msgCnt, "output1");

                if (options.logMsg)
                {
                    Console.WriteLine("{0},message,{1},{2},{3:0.000},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000}",
                        DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"),
                            EnvDeviceId,
                            stats.count, elapsedMilliseconds, stats.average, stats.min, stats.max, burstElapsedMilliseconds, rateMeter.channels["output1"].rate);
                }

                if (msgCnt >= options.burstLength)
                {
                    burstCnt++;

                    if (options.logBurst)
                    {
                        Console.WriteLine("{0},burst,{1},{2},{3},{4:0.000},{5:0.000},{6:0.000},{7:0.000},{8:0.000}",
                            DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"),
                            EnvDeviceId,
                            burstCnt, msgCnt, stats.average, stats.min, stats.max, burstElapsedMilliseconds, rateMeter.channels["output1"].rate);
                    }

                    if (options.logHist)
                    {
                        stats.Print();
                    }

                    await Task.Delay(options.burstWait); //wait even if last burst

                    if (burstCnt < options.burstNumber)
                    {
                        // next burst
                        msgCnt = 0;
                        burstStopwatch.Restart();
                    }
                    else
                    {
                        //completed
                        return;
                    }

                }
                else //wait time between one message and the next
                {
                    while (burstStopwatch.Elapsed.TotalMilliseconds < waitUntil)
                    {
                        //wait
                    }
                }

            }
        }

        /// <summary>
        /// Creates a string of specified length with random chars 
        /// (from "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") 
        /// </summary>
        /// <param name="length">Number of chars</param>
        /// <returns>the string</returns>
        static String RandomString(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            SourceOptions config = new SourceOptions();

            // meters and stats
            RateMeter rateMeter;
            StatsCalculator stats = new StatsCalculator(200, 1);    // 0 - 199, bin size = 1ms, drop initial 5 messages

            // init and start the Prometheus server
            var server = new MetricServer(port: 9600);
            server.Start();

            // Open a connection to the Edge runtime
            IoTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await IoTHubModuleClient.OpenAsync();
            string EnvDeviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            string EnvHub = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");


            Console.WriteLine("IoT Hub module client initialized.");
            Console.WriteLine($"Device id: '{EnvDeviceId}'");
            Console.WriteLine($"IoT HUB: '{EnvHub}'");

            // get settings from ENV
            config.GetFromEnv();

            // wait before starting
            Console.WriteLine("");
            Console.WriteLine($"Waiting {config.waitBeforeStart} ms before starting...");
            Thread.Sleep(config.waitBeforeStart);

            // initialize and start the rate meter
            rateMeter = new RateMeter(config.rateCalcPeriod, new string[] { "output1" });

            // Start reading and sending device telemetry
            Console.WriteLine("");
            Console.WriteLine("sending messages...");

            await SendMessages(
                IoTHubModuleClient,
                config,
                rateMeter,
                stats,
                EnvDeviceId
            );
        }
    }
}
