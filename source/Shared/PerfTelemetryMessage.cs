namespace IotEdgePerf.Shared
{
    using System;
    
    public class PerfTelemetryMessage
    {
        public string   sessionId;
        public double   sessionTimeElapsedMilliseconds;

        public double?  sessionRollingRate;

        public int      messageSequenceNumberInSession;

        public double   messageTimestampMilliseconds;
        public double?  previousTransmissionDurationMilliseconds;
        public double?  previousMessageCycleDurationMilliseconds;
        public int      timingViolationsCounter;
    }
}