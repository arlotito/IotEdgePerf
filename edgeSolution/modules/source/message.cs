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

        // payload with random content
        public string payload;
    }
}