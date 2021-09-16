namespace RateMeter
{
    using System;

    //{"t":"2021-09-14T14:03:40.0000000Z","device_id":"standard-ds5-v2-edge-1-2-1631627945","estimatedRate":538,"avgLatency":40284.937174721192,"minLatency":38752,"maxLatency":41858} 
    public class Message
    {
        public string t;
        public string device_id;
        public int estimatedRate;
        public float avgLatency;
        public float minLatency;
        public float maxLatency;
    }
}