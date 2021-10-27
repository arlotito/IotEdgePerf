namespace IotEdgePerf.Transmitter
{
    using System.Threading.Tasks;
    using System.Collections.Generic;
    public interface ITransmitterTransport
    {
        Task SendMessageHandler(string message);
        Task SendMessageBatchHandler(List<string> message);
    }
}