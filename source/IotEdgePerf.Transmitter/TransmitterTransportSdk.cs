namespace IotEdgePerf.Transmitter.Transport
{
    using System.Text;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    using Microsoft.Azure.Devices.Client;

    public class Sdk : ITransmitterTransport
    {
        ModuleClient    _client;
        string          _output;

        public Sdk(ModuleClient client, string output)
        {
            this._client=client;
            this._output=output;
        }
        public async Task SendMessageHandler(string message)
        {
            Message azIotMessage = new Message(Encoding.ASCII.GetBytes(message));
            await _client.SendEventAsync(_output, azIotMessage).ConfigureAwait(false);
        }

        public async Task SendMessageBatchHandler(List<string> messageBatch)
        {
            List<Message> azIotMessageBatch = new List<Message>();

            foreach (var message in messageBatch)
            {
                azIotMessageBatch.Add(new Message(Encoding.ASCII.GetBytes(message)));
            }

            await _client.SendEventBatchAsync(_output, azIotMessageBatch).ConfigureAwait(false);
        }
    }
}