namespace transmitter
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    using Newtonsoft.Json;

    using Serilog;

    using IotEdgePerf.Transmitter;
            
    class Program
    {
        static ModuleClient _ioTHubModuleClient;
        static string _moduleOutput = "output1";

        static string _deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
        static string _iotHubHostname = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");

        

        static Transmitter _transmitter;
        
        static TwinCollection _twin;

        async static Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                //.WriteTo.File("logs/myapp.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Init().Wait();

            await _transmitter.RegisterDM();
            _transmitter.Start(Guid.NewGuid(), TransmitterConfig.GetFromTwin(_twin));

            while (true)
            {
                await _transmitter.SendMessagesAsync();
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

            // 
            _transmitter = new Transmitter(_ioTHubModuleClient, _moduleOutput);
        }
    }
}
