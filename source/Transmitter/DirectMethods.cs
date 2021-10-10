namespace IotEdgePerf.Transmitter
{
    using System;
    using System.Threading.Tasks;
    using System.Text;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using IotEdgePerf.Shared;
    
    public partial class Transmitter : ITransmitter
    {
        public async Task RegisterDM()
        {
            // direct methods
            await _moduleClient.SetMethodHandlerAsync("Start", OnStartDm, this);
            await _moduleClient.SetMethodHandlerAsync("Restart", OnRestartDm, this);
        }

        private static Task<MethodResponse> OnStartDm(MethodRequest methodRequest, object userContext)
        {
            Transmitter senderMachine = (Transmitter)userContext;
            
            Console.WriteLine($"{methodRequest.Name} was called.");
            Console.WriteLine($"{methodRequest.DataAsJson}");

            var request = JsonConvert.DeserializeObject<TransmitterStartDmPayload>(methodRequest.DataAsJson);
            senderMachine.Start(request.runId, request.config);
            
            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private static Task<MethodResponse> OnRestartDm(MethodRequest methodRequest, object userContext)
        {
            Transmitter senderMachine = (Transmitter)userContext;
            
            Console.WriteLine($"{methodRequest.Name} was called.");
            Console.WriteLine($"{methodRequest.DataAsJson}");

            var request = JsonConvert.DeserializeObject<TransmitterRestartDmPayload>(methodRequest.DataAsJson);
            senderMachine.Restart(request.runId);             
            
            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }
    }
}