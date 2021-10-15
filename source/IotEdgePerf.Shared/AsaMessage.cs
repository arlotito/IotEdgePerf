namespace IotEdgePerf.Shared
{
    public class AsaMessage
    {
        // timestamp of the 5s bucket
        public string t;
        public string sessionId;
        public double burstCounter;
        public double sessionTimeElapsedMilliseconds;
        public double firstMessageEpoch;
        public double lastMessageEpoch;
        public double firstIotHubEpoch;
        public double lastIotHubEpoch;
        public double? sessionRollingRate;
        public double messageSequenceNumberInSession;
        public double asaTimingViolationsCounter;
        public double? minTransmissionDuration;
        public double? maxTransmissionDuration;
        public double? avgTransmissionDuration;
        public double? minCycleDuration;
        public double? maxCycleDuration;
        public double? avgCycleDuration;
        public double asaMessageCount;
        public double avgDeviceToHubLatency;
        public double minDeviceToHubLatency;
        public double maxDeviceToHubLatency;
        public double avgHubToAsaLatency;
        public double minHubToAsaLatency;
        public double maxHubToAsaLatency;
    }
}


