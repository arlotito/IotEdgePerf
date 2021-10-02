namespace edgeBenchmark
{
    using System;
    
    public class MessageDataPoint
    {
        // timestamp
        public long ts;
        // msg counter
        public int counter;
        
        // total msg to be sent
        public int total;

        // a guid for this run
        public string runId;

        // payload with random content
        public string payload;
    }
}