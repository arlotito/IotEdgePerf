namespace IotEdgePerf.Transmitter.Edge
{
    using System;
    using System.Threading.Tasks;
    using System.Text;

    using System.Collections.Generic;

    using Newtonsoft.Json;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    
    using Serilog;
    using Serilog.Core;
    using Serilog.Events;

    using IotEdgePerf.Transmitter;
    using IotEdgePerf.Transmitter.Commands;
    
    class Program
    {
        static ModuleClient _ioTHubModuleClient;
        static string _moduleOutput = "output1";

        static string _deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
        static string _iotHubHostname = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");
        static LoggingLevelSwitch _logLevelSwitch = new LoggingLevelSwitch();

        static TransmitterLogic _transmitter;
        
        static TwinCollection _twin;

        static void GetLogLevelFromEnv()
        {
            var level = Environment.GetEnvironmentVariable("LOG_LEVEL");

            if (!String.IsNullOrEmpty(level))
            {
                switch (level)
                {
                    default:
                        _logLevelSwitch.MinimumLevel = LogEventLevel.Information;
                        break;

                    case "info":
                        _logLevelSwitch.MinimumLevel = LogEventLevel.Information;
                        break;

                    case "debug":
                        _logLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                        break;

                    case "error":
                        _logLevelSwitch.MinimumLevel = LogEventLevel.Debug;
                        break;
                }
            }
        }
        
        static void Main(string[] args)
        {
            GetLogLevelFromEnv();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_logLevelSwitch)
                .WriteTo.Console()
                //.WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            
            Init().Wait();
            
            while (true)
            {
                _transmitter.Send();
            }
        }

        static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object obj)
        {
            try
            {
                Log.Debug("Desired property change:\n{0}", JsonConvert.SerializeObject(desiredProperties));
                _twin = desiredProperties;
            }

            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Log.Error("Error when receiving desired property: {0}", exception);
                }
            }

            catch (Exception ex)
            {
                Log.Error("Error when receiving desired property: {0}", ex.Message);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            _ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await _ioTHubModuleClient.OpenAsync();

            // Read module twin
            var moduleTwin = await _ioTHubModuleClient.GetTwinAsync();
            await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, _ioTHubModuleClient);
            await _ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            Log.Information("IoT Hub module client initialized.");
            Log.Information($"Device id: '{_deviceId}'");
            Log.Information($"IoT HUB: '{_iotHubHostname}'");

            // creat an instance
            _transmitter = new TransmitterLogic();

            // events handlers
            _transmitter.SendMessageHandler         += SdkSendMessage;
            _transmitter.SendMessageBatchHandler    += null;
            _transmitter.CreateMessageHandler       += CreateMessage;

            // direct methods handler
            await _ioTHubModuleClient.SetMethodHandlerAsync("Start", OnStartDm, _transmitter);

            // applies initial configuration from twins
            _transmitter.ApplyConfiguration(TransmitterConfig.GetFromTwin(_twin));
        }

        private static Task<MethodResponse> OnStartDm(MethodRequest methodRequest, object userContext)
        {
            TransmitterLogic transmitter = (TransmitterLogic)userContext;
            
            Log.Information($"Direct Method '{methodRequest.Name}' was called.");
            Log.Debug($"{methodRequest.DataAsJson}");

            var request = JsonConvert.DeserializeObject<TransmitterStartCommand>(methodRequest.DataAsJson);

            transmitter.ApplyConfiguration(request.config);
            transmitter.Start(request.runId);
            
            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private static void SdkSendMessage(string message)
        {
            Message azIotMessage = new Message(Encoding.ASCII.GetBytes(message));
            _ioTHubModuleClient.SendEventAsync(_moduleOutput, azIotMessage).Wait();
        }

        private static void SdkSendMessageBatch(string[] messageBatch)
        {
            List<Message> azIotMessageBatch = new List<Message>();

            foreach (var message in messageBatch)
            {
                azIotMessageBatch.Add(new Message(Encoding.ASCII.GetBytes(message)));
            }

            _ioTHubModuleClient.SendEventBatchAsync(_moduleOutput, azIotMessageBatch).Wait();
        }

        private static object CreateMessage(int length)
        {
            var applicationObject = new
            {
                payload = RandomString(length)
            };

            return applicationObject;
        }

        /// <summary>
        /// Creates a string of specified length with random chars 
        /// (from "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") 
        /// </summary>
        /// <param name="length">Number of chars</param>
        /// <returns>the string</returns>
        private static String RandomString(int length)
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
    }
}
