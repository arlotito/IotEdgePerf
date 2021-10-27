namespace IotEdgePerf.Transmitter
{
    using System;
    using IotEdgePerf.Shared;
    
    public class TransmitterStartCommand
    {
        public Guid runId;
        public TransmitterConfigData config;
    }
}