namespace IotEdgePerf.Transmitter.Commands
{
    using System;
    using IotEdgePerf.Transmitter.ConfigData;
    
    public class TransmitterStartCommand
    {
        public Guid runId;
        public TransmitterConfigData config;
    }
}