namespace source
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    using System.Collections.Generic;
    using Newtonsoft.Json;
    
    using edgeBenchmark;

    public class SenderMachineDMStartRequest
    {
        public Guid runId;
        public SenderMachineConfigData config;
    }

    public class SenderMachineDMRestartRequest
    {
        public Guid runId;
    }

    public partial class SenderMachine : ISenderMachine
    {
        public async Task RegisterDM()
        {
            // direct methods
            await moduleClient.SetMethodHandlerAsync("Start", OnStartDm, this);
            await moduleClient.SetMethodHandlerAsync("Restart", OnRestartDm, this);
        }

        private static Task<MethodResponse> OnStartDm(MethodRequest methodRequest, object userContext)
        {
            SenderMachine senderMachine = (SenderMachine)userContext;
            
            Console.WriteLine($"{methodRequest.Name} was called.");
            Console.WriteLine($"{methodRequest.DataAsJson}");

            var request = JsonConvert.DeserializeObject<SenderMachineDMStartRequest>(methodRequest.DataAsJson);
            senderMachine.Start(request.runId, request.config);
            
            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private static Task<MethodResponse> OnRestartDm(MethodRequest methodRequest, object userContext)
        {
            SenderMachine senderMachine = (SenderMachine)userContext;
            
            Console.WriteLine($"{methodRequest.Name} was called.");
            Console.WriteLine($"{methodRequest.DataAsJson}");

            var request = JsonConvert.DeserializeObject<SenderMachineDMRestartRequest>(methodRequest.DataAsJson);
            senderMachine.Restart(request.runId);             
            
            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }
    }
}