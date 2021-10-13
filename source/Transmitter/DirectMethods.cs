namespace IotEdgePerf.Transmitter
{
    using System;
    using System.Threading.Tasks;
    using System.Text;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using IotEdgePerf.Shared;
    using Serilog;
    
    public partial class Transmitter
    {
        public async Task RegisterDM()
        {
            // direct methods
            await _moduleClient.SetMethodHandlerAsync("Start", OnStartDm, this);
        }

        private static Task<MethodResponse> OnStartDm(MethodRequest methodRequest, object userContext)
        {
            Transmitter transmitter = (Transmitter)userContext;
            
            Log.Information($"Direct Method '{methodRequest.Name}' was called.");
            Log.Debug($"{methodRequest.DataAsJson}");

            var request = JsonConvert.DeserializeObject<TransmitterStartDmPayload>(methodRequest.DataAsJson);

            transmitter.ApplyConfiguration(request.config);
            transmitter.Start(request.runId);
            
            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }
    }
}