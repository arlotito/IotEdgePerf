namespace IotEdgePerf.Shared
{
    using System;
    
    public class TransmitterStartDmPayload
    {
        public Guid runId;
        public TransmitterConfigData config;
    }

    public class TransmitterRestartDmPayload
    {
        public Guid runId;
    }
}