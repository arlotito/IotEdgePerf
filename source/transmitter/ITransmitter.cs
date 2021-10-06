
namespace IoTEdgePerf.Transmitter
{
    using System;
    using System.Threading.Tasks;
    using IoTEdgePerf.Shared;

    public interface ITransmitter
    {
        public void Restart(Guid runId);
        public void Start(Guid runId,  TransmitterConfigData config);

        public Task RegisterDM();
        
        public Task SendMessagesAsync();
    }
}
