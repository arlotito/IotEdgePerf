namespace source
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    using Newtonsoft.Json;
    
    class Program
    {
        static ModuleClient IoTHubModuleClient;
        static string ModuleOutput = "output1";

        static string EnvDeviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
        static string EnvHub = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");

        

        static SenderMachine senderMachine;
        
        static TwinCollection twin;


        async static Task Main(string[] args)
        {
            Init().Wait();

            // Start reading and sending device telemetry
            Console.WriteLine("");
            Console.WriteLine("sending messages...");

            senderMachine.Reset(SenderMachineConfig.GetFromTwin(twin));
            
            while (true)
            {
                await senderMachine.SendMessagesAsync();
            }
            
        }

        static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object obj)
        {
            try
            {
                Console.WriteLine("Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                twin = desiredProperties;
            }

            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine("Error when receiving desired property: {0}", exception);
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error when receiving desired property: {0}", ex.Message);
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
            IoTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await IoTHubModuleClient.OpenAsync();

            // Read module twin
            var moduleTwin = await IoTHubModuleClient.GetTwinAsync();
            await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, IoTHubModuleClient);
            await IoTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // direct methods
            await IoTHubModuleClient.SetMethodHandlerAsync("Reset", OnResetDm, null);
            
            Console.WriteLine("IoT Hub module client initialized.");
            Console.WriteLine($"Device id: '{EnvDeviceId}'");
            Console.WriteLine($"IoT HUB: '{EnvHub}'");

            // 
            senderMachine = new SenderMachine(IoTHubModuleClient, ModuleOutput);
        }

        private static Task<MethodResponse> OnResetDm(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("Reset DM received");
            
            senderMachine.Reset(SenderMachineConfig.GetFromTwin(twin));
            
            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }
    }
}
