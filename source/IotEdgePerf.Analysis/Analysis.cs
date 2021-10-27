using IotEdgePerf.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IotEdgePerf.Analysis
{
    public class BurstAnalysisData
    {
        public string CustomLabel;
        public string SessionId;
        public double BurstCounter;

        public TransmitterConfigData Config;

        public string iotHubHostname;
        public string deviceId;

        // device
        public double DeviceFirstMessageEpoch;
        public double DeviceLastMessageEpoch;
        public double DeviceSessionDuration;
        public double DeviceMessageCount;
        public double? DeviceTimingsViolationsCount;
        public double? DeviceAvgTransmissionDuration;
        public double? DeviceMinTransmissionDuration;
        public double? DeviceMaxTransmissionDuration;
        public double? DeviceAvgCycleDuration;
        public double? DeviceMinCycleDuration;
        public double? DeviceMaxCycleDuration;
        public double? DeviceEgressRate;
        public double DeviceEgressThroughputKBs;

        // iot hub
        public double IotHubMessageCount;
        public double IotHubFirstMessageEpoch;
        public double IotHubLastMessageEpoch;
        public double IotHubSessionDuration;
        public double DeviceToIotHubAvgLatency;
        public double DeviceToIotHubMinLatency;
        public double DeviceToIotHubMaxLatency;
        public double? IotHubIngressRate;
        public double IotHubIngressThroughputKBs;


        // asa
        public double IotHubToAsaAvgLatency;
        public double IotHubToAsaMinLatency;
        public double IotHubToAsaMaxLatency;

        public void Show()
        {
            var sessionStart = DateTimeOffset.FromUnixTimeMilliseconds((long)DeviceFirstMessageEpoch).DateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK");
            var sessionEnd = DateTimeOffset.FromUnixTimeMilliseconds((long)IotHubLastMessageEpoch).DateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK");
            
            Console.WriteLine("---------------------------------------");
            Console.WriteLine($"IoT HUB:        {iotHubHostname}");
            Console.WriteLine($"device ID:      {deviceId}");
            Console.WriteLine($"session ID:     {SessionId}");
            Console.WriteLine($"session start:  {sessionStart}  (1st msg sent by device)");
            Console.WriteLine($"session end:    {sessionEnd}  (last msg received by IoT HUB)");
            Console.WriteLine($"burst counter:  {BurstCounter} of {Config.burstNumber}");
            Console.WriteLine($"message size:   {Config.payloadLength} [bytes]");
            Console.WriteLine("");
            
            const int colWidth = 10;
            string str;

            Console.Write($"{"",20}");
            Console.Write($"{"",5}");
            str = String.Format($"{"desired",colWidth} / {"SOURCE",colWidth} ==> {"IoT HUB",-colWidth}");
            Console.WriteLine($"{str}");
            
            

            Console.Write($"{"rate:",20}");
            Console.Write($"{"",5}");
            str = String.Format($"{Config.targetRate,colWidth:0} / {(double)DeviceEgressRate,colWidth:0.0} ==> {(double)IotHubIngressRate,-colWidth:0.0} [msg/s]");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{str}");
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Write($"{"throughput:",20}");
            Console.Write($"{"",5}");
            str = String.Format($"{Config.targetRate*Config.payloadLength/1024,colWidth:0.0} / {(double)DeviceEgressThroughputKBs,colWidth:0.0} ==> {(double)IotHubIngressThroughputKBs,-colWidth:0.0} [KB/s]");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{str}");
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Write($"{"message count:",20}");
            Console.Write($"{"",5}");
            Console.ForegroundColor = (IotHubMessageCount < Config.burstLength) ? ConsoleColor.Red : ConsoleColor.Gray;
            str = String.Format($"{Config.burstLength,colWidth:0} / {DeviceMessageCount,colWidth} ==> {IotHubMessageCount,-colWidth} [msg]");
            Console.WriteLine($"{str}");
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Write($"{"session duration:",20}");
            Console.Write($"{"",5}");
            str = String.Format($"{(double)Config.burstLength/(double)Config.targetRate,colWidth:0.000} / {(double)DeviceSessionDuration/1000,colWidth:0.000} ==> {(double)IotHubSessionDuration/1000,-colWidth:0.000} [s]");
            Console.WriteLine($"{str}");
            
            const int columnWidth = 35;

            Console.WriteLine("");
            Console.Write($"{"MAX device-to-HUB latency:",-columnWidth}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{DeviceToIotHubMaxLatency:0.00} [ms] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"(min:{DeviceToIotHubMinLatency:0.00}/avg:{DeviceToIotHubAvgLatency:0.00})");
            

            Console.Write($"{"MAX HUB-to-ASA latency:",-columnWidth}");
            Console.WriteLine($"{IotHubToAsaMaxLatency:0.00} [ms] (min:{IotHubToAsaMinLatency:0.00}/avg:{IotHubToAsaAvgLatency:0.00})");
            //PrintRow("fist msg epoch [ms]:", "", (double)DeviceFirstMessageEpoch, (double)IotHubFirstMessageEpoch);
            //PrintRow("last msg epoch [ms]:", "", (double)DeviceLastMessageEpoch, (double)IotHubLastMessageEpoch);
            
            Console.WriteLine("");
            
            Console.Write($"{"avg SendEventAsync() duration:", -columnWidth}");
            Console.WriteLine($"{DeviceAvgTransmissionDuration:0.00} [ms] (min:{DeviceMinTransmissionDuration:0.00}/max:{DeviceMaxTransmissionDuration:0.00})");
            
            Console.Write($"{"avg single msg cycle duration", -columnWidth}");
            Console.WriteLine($"{DeviceAvgCycleDuration:0.00} [ms] (min:{DeviceMinCycleDuration:0.00}/max:{DeviceMaxCycleDuration:0.00}) vs. a desired of {1/(double)Config.targetRate*1000:0.00} [ms]");
            
            Console.Write($"{"timing violations count:", -columnWidth}");
            Console.WriteLine($"{DeviceTimingsViolationsCount:0} out of {DeviceMessageCount} messages sent");
           
            Console.WriteLine("");

        }

        public string ToCsvString()
        {
            string csvRow = "";
            csvRow += String.Format($"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK")},");
            csvRow += String.Format($"{iotHubHostname},");
            csvRow += String.Format($"{deviceId},");
            csvRow += String.Format($"{SessionId},");
            csvRow += String.Format($"{BurstCounter},");
            csvRow += String.Format($"{Config.payloadLength},");
            csvRow += String.Format($"{Config.burstLength},");
            csvRow += String.Format($"{Config.targetRate},");

            csvRow += String.Format($"{DeviceEgressRate:0.00},");
            csvRow += String.Format($"{IotHubIngressRate:0.00},");
            csvRow += String.Format($"{DeviceEgressThroughputKBs:0.00},");
            csvRow += String.Format($"{IotHubIngressThroughputKBs:0.00},");
            csvRow += String.Format($"{DeviceToIotHubAvgLatency:0.00},{DeviceToIotHubMinLatency:0.00},{DeviceToIotHubMaxLatency:0.00},");
            csvRow += String.Format($"{DeviceAvgTransmissionDuration:0.00},{DeviceMinTransmissionDuration:0.00},{DeviceMaxTransmissionDuration:0.00},");
            csvRow += String.Format($"{CustomLabel}");

            csvRow += String.Format($"\n");

            return csvRow;
        }

        public string GetCsvHeader()
        {
            string csvRow = "";
            csvRow += String.Format($"ts,");
            csvRow += String.Format($"iotHubHostname,");
            csvRow += String.Format($"deviceId,");
            csvRow += String.Format($"SessionId,");
            csvRow += String.Format($"BurstCounter,");
            csvRow += String.Format($"Config.payloadLength,");
            csvRow += String.Format($"Config.burstLength,");
            csvRow += String.Format($"Config.targetRate,");

            csvRow += String.Format($"DeviceEgressRate,");
            csvRow += String.Format($"IotHubIngressRate,");
            csvRow += String.Format($"DeviceEgressThroughputKBs,");
            csvRow += String.Format($"IotHubIngressThroughputKBs,");
            csvRow += String.Format($"DeviceToIotHubAvgLatency,DeviceToIotHubMinLatency,DeviceToIotHubMaxLatency,");
            csvRow += String.Format($"DeviceAvgTransmissionDuration,DeviceMinTransmissionDuration,DeviceMaxTransmissionDuration");

            int cnt=1;
            foreach (var field in CustomLabel.Split(','))
            {
                csvRow += String.Format($",CustomLabel{cnt++}");    
            }
            
            csvRow += String.Format($"\n");

            return csvRow;
        }

    }

    partial class BurstAnalyzer
    {
        List<AsaMessage> _messagesList;
        TransmitterConfigData _config;
        string _iotHubHostname;
        string _deviceId;
        string _csvFilename;
        string _customLabel;
        int _receivedMessageCount;
        
 
        public BurstAnalyzer(
            string iotHubHostname,
            string deviceId,
            TransmitterConfigData config, string csvFilename, string customLabel)
        {
            this._messagesList = new List<AsaMessage>();
            this._config = config;
            this._csvFilename = csvFilename;
            this._customLabel = customLabel;
            this._deviceId = deviceId;
            this._iotHubHostname = iotHubHostname;
            this._receivedMessageCount = 0;
            
        }

        public int? AddMessage(AsaMessage msg, string sessionId)
        {
            bool add = true;
            int? count = null;
            
            if (!String.IsNullOrEmpty(sessionId))
            {
                if (!msg.sessionId.Equals(sessionId))
                {
                    add = false;
                }
            }
            
            if (add)
            {
                this._messagesList.Add(msg);
                this._receivedMessageCount += (int)msg.asaMessageCount;
                count = this._receivedMessageCount;
            }
                        
            return count;
        }

        public void DoAnalysis(string sessionId)
        {
            // order by asa timestamp
            _messagesList.OrderBy(msg => msg.t);

            // group by burstCounter
            var query = from item in _messagesList
                        where item.sessionId == sessionId
                        group item by (item.sessionId, item.burstCounter) into sessionGroup
                        orderby sessionGroup.Key ascending
                        select sessionGroup;
            
            // perform analysis
            foreach (var sessionGroup in query)
            {
                AsaMessage firstDeviceMessage = sessionGroup.OrderBy(item => item.firstMessageEpoch).First();
                AsaMessage lastDeviceMessage = sessionGroup.OrderBy(item => item.lastMessageEpoch).Last();
                AsaMessage firstIotHubMessage = sessionGroup.OrderBy(item => item.firstIotHubEpoch).First();
                AsaMessage lastIotHubMessage = sessionGroup.OrderBy(item => item.lastIotHubEpoch).Last();

                var burstAnalysisData = new BurstAnalysisData
                {
                    CustomLabel = this._customLabel,
                    Config = this._config,

                    iotHubHostname = this._iotHubHostname,
                    deviceId = this._deviceId,

                    SessionId = lastDeviceMessage.sessionId,
                    BurstCounter = lastDeviceMessage.burstCounter,

                    // device
                    DeviceFirstMessageEpoch = firstDeviceMessage.firstMessageEpoch,
                    DeviceLastMessageEpoch = lastDeviceMessage.lastMessageEpoch,
                    DeviceSessionDuration = lastDeviceMessage.lastMessageEpoch - firstDeviceMessage.firstMessageEpoch,

                    DeviceMessageCount = lastDeviceMessage.messageSequenceNumberInSession,

                    DeviceEgressRate = lastDeviceMessage.sessionRollingRate,
                    DeviceTimingsViolationsCount = lastDeviceMessage.asaTimingViolationsCounter,

                    DeviceAvgTransmissionDuration = sessionGroup.Average(item => item.avgTransmissionDuration),
                    DeviceMinTransmissionDuration = sessionGroup.Min(item => item.minTransmissionDuration),
                    DeviceMaxTransmissionDuration = sessionGroup.Max(item => item.maxTransmissionDuration),

                    DeviceAvgCycleDuration = sessionGroup.Average(item => item.avgCycleDuration),
                    DeviceMinCycleDuration = sessionGroup.Min(item => item.minCycleDuration),
                    DeviceMaxCycleDuration = sessionGroup.Max(item => item.maxCycleDuration),

                    // iot hub
                    IotHubMessageCount = sessionGroup.Sum(item => item.asaMessageCount),

                    IotHubFirstMessageEpoch = firstIotHubMessage.firstIotHubEpoch,
                    IotHubLastMessageEpoch = lastIotHubMessage.lastIotHubEpoch,
                    IotHubSessionDuration = lastIotHubMessage.lastIotHubEpoch - firstIotHubMessage.firstIotHubEpoch,

                    IotHubIngressRate = sessionGroup.Sum(item => item.asaMessageCount) / (lastIotHubMessage.lastIotHubEpoch - firstIotHubMessage.firstIotHubEpoch) * 1000,

                    DeviceToIotHubAvgLatency = sessionGroup.Average(item => item.avgDeviceToHubLatency),
                    DeviceToIotHubMinLatency = sessionGroup.Min(item => item.minDeviceToHubLatency),
                    DeviceToIotHubMaxLatency = sessionGroup.Max(item => item.maxDeviceToHubLatency),

                    IotHubToAsaAvgLatency = sessionGroup.Average(item => item.avgHubToAsaLatency),
                    IotHubToAsaMinLatency = sessionGroup.Min(item => item.minHubToAsaLatency),
                    IotHubToAsaMaxLatency = sessionGroup.Max(item => item.maxHubToAsaLatency)
                };

                // throughput
                if (burstAnalysisData.DeviceEgressRate != null)
                    burstAnalysisData.DeviceEgressThroughputKBs = (double)burstAnalysisData.DeviceEgressRate * burstAnalysisData.Config.payloadLength / 1024;
                else
                    burstAnalysisData.DeviceEgressRate = 0;

                if (burstAnalysisData.IotHubIngressRate != null)
                    burstAnalysisData.IotHubIngressThroughputKBs = (double)burstAnalysisData.IotHubIngressRate * burstAnalysisData.Config.payloadLength / 1024;
                else
                    burstAnalysisData.IotHubIngressThroughputKBs = 0;

                // removes offset
                burstAnalysisData.DeviceToIotHubAvgLatency -= burstAnalysisData.DeviceToIotHubMinLatency;
                burstAnalysisData.DeviceToIotHubMinLatency -= burstAnalysisData.DeviceToIotHubMinLatency;
                burstAnalysisData.DeviceToIotHubMaxLatency -= burstAnalysisData.DeviceToIotHubMinLatency;

                burstAnalysisData.IotHubToAsaAvgLatency -= burstAnalysisData.IotHubToAsaMinLatency;
                burstAnalysisData.IotHubToAsaMinLatency -= burstAnalysisData.IotHubToAsaMinLatency;
                burstAnalysisData.IotHubToAsaMaxLatency -= burstAnalysisData.IotHubToAsaMinLatency;

                // print to screen
                burstAnalysisData.Show();

                // saves to CSV file
                if (!String.IsNullOrEmpty(this._csvFilename))
                {
                    if (!File.Exists(this._csvFilename))
                        File.AppendAllText(_csvFilename, burstAnalysisData.GetCsvHeader());
                    
                    File.AppendAllText(_csvFilename, burstAnalysisData.ToCsvString());
                }
            }

            if (!String.IsNullOrEmpty(_csvFilename))
                Console.WriteLine($"\nOutput written to {_csvFilename}");
        }
    }
}

    
        

