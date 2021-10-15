namespace IotEdgePerf.Transmitter.ConfigData
{
    public class TransmitterConfigData
    {
        public bool enable;
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
    }
}