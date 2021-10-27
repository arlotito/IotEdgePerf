namespace IotEdgePerf.Transmitter.Edge
{
    using System;
    using System.Threading.Tasks;
    using System.Text;

    using Newtonsoft.Json;

    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    
    using Serilog;
    using Serilog.Core;
    using Serilog.Events;

    using IotEdgePerf.Transmitter;
    using IotEdgePerf.Shared;

    class Program
    {
        
        static LoggingLevelSwitch _logLevelSwitch = new LoggingLevelSwitch();

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
        
        static async Task Main(string[] args)
        {
            GetLogLevelFromEnv();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_logLevelSwitch)
                .WriteTo.Console()
                //.WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            
            TransmitterLogic transmitter = await Init();
            
            while (true)
            {
                await transmitter.TransmitterLoopAsync();
            }
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task<TransmitterLogic> Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };
            ModuleClient ioTHubModuleClient;
            string moduleOutput = "output1";

            // Open a connection to the Edge runtime
            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();

            Log.Information("IoT Hub module client initialized.");
            Log.Information($"Device id: '{Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID")}'");
            Log.Information($"IoT HUB: '{Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME")}'");

            // applies initial configuration from twins
            var moduleTwin = await ioTHubModuleClient.GetTwinAsync();
            
            TransmitterLogic transmitter = new TransmitterLogic(
                TransmitterConfigData.GetFromObject(moduleTwin.Properties.Desired["config"]),
                new Transport.Sdk(ioTHubModuleClient, moduleOutput),
                new MessageProvider.RandomMessage()
            );

            // twin and dm handlers
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, transmitter);
            await ioTHubModuleClient.SetMethodHandlerAsync("Start", OnStartDm, transmitter);

            return transmitter;
        }

        private static Task<MethodResponse> OnStartDm(MethodRequest methodRequest, object userContext)
        {
            TransmitterLogic transmitter = (TransmitterLogic)userContext;

            Log.Information($"Direct Method '{methodRequest.Name}' was called.");
            Log.Debug($"{methodRequest.DataAsJson}");

            var request = JsonConvert.DeserializeObject<TransmitterStartCommand>(methodRequest.DataAsJson);

            transmitter.ApplyConfiguration(request.config);
            transmitter.Restart(request.runId);

            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Log.Debug("Desired property change:\n{0}", JsonConvert.SerializeObject(desiredProperties));
                TransmitterLogic transmitter = (TransmitterLogic)userContext;
                transmitter.ApplyConfiguration(TransmitterConfigData.GetFromObject(desiredProperties["config"]));
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
    }
}
