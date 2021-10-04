namespace edgeBenchmark
{
    using System;
    
    public class MessageDataPoint
    {
        // timestamp
        public long ts;

        public int runMsgTotal;
        public int runMsgCounter;
        public double runElapsed;

        public int burstCounter;
        // msg counter
        public int burstMsgCounter;
        
        // total msg to be sent
        public int burstLength;

        public double burstElapsed;

        // a guid for this run
        public string runId;

        // payload with random content
        public string payload;
    }
}