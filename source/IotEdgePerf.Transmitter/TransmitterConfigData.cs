using Newtonsoft.Json;

namespace IotEdgePerf.Transmitter.ConfigData
{
    public class TransmitterConfigData
    {
        public bool autoStart;
        public int burstLength;
        public int burstWait;
        public int burstNumber;
        public int targetRate;
        public int payloadLength;

        public int batchSize;
        public bool logMsg;
        public bool logBurst;
        public bool logHist;

        public int waitBeforeStart;

        public int rateCalcPeriod;

        public static TransmitterConfigData GetFromJson(string configDataJson)
        {
            return JsonConvert.DeserializeObject<TransmitterConfigData>(configDataJson);
        }

        public static TransmitterConfigData GetFromObject(object obj)
        {
            return JsonConvert.DeserializeObject<TransmitterConfigData>(JsonConvert.SerializeObject(obj));
        }
    }
}