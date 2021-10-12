
namespace IotEdgePerf.Transmitter
{
    using System;
    using System.Threading.Tasks;
    using IotEdgePerf.Shared;

    public interface ITransmitter
    {
        public void Restart(Guid runId);
        public void Start(Guid runId,  TransmitterConfigData config);

        public Task RegisterDM();
        
        public Task SendMessagesAsync();
    }
}
