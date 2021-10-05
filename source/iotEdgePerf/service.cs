namespace IoTEdgePerf.Service
{
    using System;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    using Newtonsoft.Json;
    using Microsoft.Azure.Devices;
    using System.Threading.Tasks;
    using IoTEdgePerf.Shared;
    
    public class MonitorService
    {
        private ServiceClient serviceClient;
        private string deviceId;

        private string moduleId="source";
        
        public MonitorService(string connString, string deviceId)
        {
            this.deviceId = deviceId;
            
            // Create a ServiceClient to communicate with service-facing endpoint on your hub.
            serviceClient = ServiceClient.CreateFromConnectionString(connString);
            Console.WriteLine("Connected to IoT HUB");                
        }
        
        // Invoke the direct method on the device, passing the payload
        public async Task Start(Guid runId, TransmitterConfigData config)
        {
            Console.WriteLine($"Starting {this.deviceId}...");
            
            var methodInvocation = new CloudToDeviceMethod("Start")
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };

            var payload = new {
                runId=runId,
                config=config
            };
            
            methodInvocation.SetPayloadJson(JsonConvert.SerializeObject(payload));

            Console.WriteLine($"Run ID: {runId}");

            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await this.serviceClient.InvokeDeviceMethodAsync(this.deviceId, this.moduleId, methodInvocation);

            //Console.WriteLine($"\nResponse status: {response.Status}, payload: {response.GetPayloadAsJson()}");
        }

        public async Task Restart(Guid runId)
        {
            Console.WriteLine($"Re-starting {this.deviceId}...");
            
            var methodInvocation = new CloudToDeviceMethod("Restart")
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };
            
            var payload = new {
                runId=runId
            };
            
            methodInvocation.SetPayloadJson(JsonConvert.SerializeObject(payload));

            Console.WriteLine($"Run ID: {runId}");

            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await this.serviceClient.InvokeDeviceMethodAsync(this.deviceId, this.moduleId, methodInvocation);

            //Console.WriteLine($"\nResponse status: {response.Status}, payload: {response.GetPayloadAsJson()}");
        }
    }
}