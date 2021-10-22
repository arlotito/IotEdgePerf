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

        public TransmitterConfigData()
        {
            enable=false;
            burstLength=1000;
            burstWait=5000;
            burstNumber=1;
            targetRate=1000;
            payloadLength=1024;
            batchSize=1;
            logMsg=false;
            logBurst=true;
            logHist=false;
            waitBeforeStart=0;
            rateCalcPeriod=5000;
        }
    }
}