namespace IotEdgePerf.Shared
{
    public class AsaMessage
    {
        // timestamp of the 5s bucket
        public string t;
        public string sessionId;
        public double sessionTimeElapsedMilliseconds;
        public double firstMessageEpoch;
        public double lastMessageEpoch;
        public double firstIotHubEpoch;
        public double lastIotHubEpoch;
        public double minRollingRate;
        public double maxRollingRate;
        public double avgRollingRate;
        public float messageSequenceNumberInSession;
        public float asaTimingViolationsCounter;
        public float minTransmissionDuration;
        public float maxTransmissionDuration;
        public float avgTransmissionDuration;
        public float minCycleDuration;
        public float maxCycleDuration;
        public float avgCycleDuration;
        public float asaMessageCount;
        public float asaEstimatedRate;
        public float asaEstimatedRateIotHub;
        public float asaEstimatedRateAsa;
        public float avgDeviceToHubLatency;
        public float minDeviceToHubLatency;
        public float maxDeviceToHubLatency;
        public float avgHubToAsaLatency;
        public float minHubToAsaLatency;
        public float maxHubToAsaLatency;
    }
}


