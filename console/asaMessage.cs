namespace AsaJob
{
    public class Message
    {
        // timestamp of the 5s bucket
        public string t;
        
        public string runId;
        public string firstMsgTs;
        public string lastMsgTs;

        //last counter seen in the bucket
        public float counter;
        //last total seen in the bucket
        public float total;
        
        public int messagesCount;
        public int estimatedRate;
        public float avgLatency;
        public float minLatency;
        public float maxLatency;
    }
}