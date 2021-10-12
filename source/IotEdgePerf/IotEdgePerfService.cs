namespace IotEdgePerf.Service
{
    using System;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    using Newtonsoft.Json;
    using Microsoft.Azure.Devices;
    using System.Threading.Tasks;
    using IotEdgePerf.Shared;
    
    public class IotEdgePerfService
    {
        private ServiceClient   _serviceClient;
        private string          _deviceId;
        private string          _transmitterModuleName;
        public string           _runId;
        
        public IotEdgePerfService(string connString, string deviceId, string transmitterModuleName)
        {
            this._deviceId = deviceId;
            this._transmitterModuleName = transmitterModuleName;
            
            // Create a ServiceClient to communicate with service-facing endpoint on your hub.
            _serviceClient = ServiceClient.CreateFromConnectionString(connString);
            Console.WriteLine("Connected to IoT HUB");                
        }
        
        // Invoke the direct method on the device, passing the payload
        public async Task Start(Guid runId, TransmitterConfigData config)
        {
            Console.WriteLine($"Invoking 'Start' dm on device '{this._deviceId}'...");
            
            this._runId = runId.ToString();

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
            var response = await this._serviceClient.InvokeDeviceMethodAsync(this._deviceId, this._transmitterModuleName, methodInvocation);

            //Console.WriteLine($"\nResponse status: {response.Status}, payload: {response.GetPayloadAsJson()}");
        }

        public async Task Restart(Guid runId)
        {
            Console.WriteLine($"Invoking 'Restart' dm on device '{this._deviceId}'...");
            
            this._runId = runId.ToString();

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
            var response = await this._serviceClient.InvokeDeviceMethodAsync(this._deviceId, this._transmitterModuleName, methodInvocation);

            //Console.WriteLine($"\nResponse status: {response.Status}, payload: {response.GetPayloadAsJson()}");
        }
    }
}