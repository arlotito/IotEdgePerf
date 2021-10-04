namespace AsaJob
{
    public class Message
    {
        // timestamp of the 5s bucket
        public string t;
        
        public string runId;
        public int burstCounter;
        public string firstMsgTs;
        public string lastMsgTs;
        public double firstIotHubEpoch;
        public double lastIotHubEpoch;

        //last counter seen in the bucket
        public float asaRunMsgTotal;
        //last total seen in the bucket
        public float asaRunMsgCounter;
        public double asaRunElapsed;
        public float asaBurstLength;
        public float asaBurstMsgCounter;
        public double asaBurstElapsed;
        
        public int asaMsgCount;
        public float asaEstimatedRate;
        public float asaEstimatedRateIotHub;
        public float asaEstimatedRateAsa;
        public float asaAvgLatency;
        public float asaMinLatency;
        public float asaMaxLatency;
    }
}


