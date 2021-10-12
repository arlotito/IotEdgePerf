using IotEdgePerf.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class AnalysisData
{
    public string CustomLabel;
    public string SessionId;

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
        Console.WriteLine($"session ID:     {SessionId}");
        Console.WriteLine($"IoT HUB:        {iotHubHostname}");
        Console.WriteLine($"device ID:      {deviceId}");
        Console.WriteLine($"message count:  {DeviceMessageCount} [msg]");
        Console.WriteLine($"message size:   {Config.payloadLength} [bytes]");
        Console.WriteLine($"raget rate:     {Config.targetRate} [msg/s]");
        Console.WriteLine(""); 
        Console.WriteLine($"                                SOURCE (out)  =>    (in) IoT HUB");
        Console.WriteLine($"                                --------------------------------");
        Console.WriteLine($"single msg tx duration [ms]:    {DeviceAvgTransmissionDuration:0.00} ({DeviceMinTransmissionDuration:0.00}/{DeviceMaxTransmissionDuration:0.00})");
        Console.WriteLine($"single msg cycle duration [ms]: {DeviceAvgCycleDuration:0.00} ({DeviceMinCycleDuration:0.00}/{DeviceMaxCycleDuration:0.00}");
        Console.WriteLine($"timing violations count [msg]:  {DeviceTimingsViolationsCount:0.00}"); 
        Console.WriteLine($"message count [msg]:            {DeviceMessageCount,-20}{IotHubMessageCount,-20}");
        Console.WriteLine($"fist msg epoch [ms]:            {DeviceFirstMessageEpoch,-20}{IotHubFirstMessageEpoch,-20}");
        Console.WriteLine($"last msg epoch [ms]:            {DeviceLastMessageEpoch,-20}{IotHubLastMessageEpoch,-20}");
        Console.WriteLine($"session duration [ms]:          {DeviceSessionDuration,-20}{IotHubSessionDuration,-20}");
        Console.WriteLine($"                                --------------------------------");
        Console.Write($"rate [msg/s]:                   ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{DeviceEgressRate,-20:0.00}{IotHubIngressRate,-20:0.00}");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"throughput [KB/s]:              ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{DeviceEgressThroughputKBs,-20:0.00}{IotHubIngressThroughputKBs,-20:0.00}");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("------------------------------------------------------------------");
        Console.Write($"Device to HUB latency:          ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{DeviceToIotHubAvgLatency:0.00} ({DeviceToIotHubMinLatency:0.00}/{DeviceToIotHubMaxLatency:0.00})");
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"HUB to ASA latency:             {IotHubToAsaAvgLatency:0.00} ({IotHubToAsaMinLatency:0.00}/{IotHubToAsaMaxLatency:0.00})");
        Console.WriteLine("");
    }

    public string ToCsvString()
    {
        string csvRow = "";
        csvRow += String.Format($"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK")},");
        csvRow += String.Format($"{iotHubHostname},");
        csvRow += String.Format($"{deviceId},");
        csvRow += String.Format($"{SessionId},");
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
        csvRow += String.Format($"Config.payloadLength,");
        csvRow += String.Format($"Config.burstLength,");
        csvRow += String.Format($"Config.targetRate,");

        csvRow += String.Format($"DeviceEgressRate,");
        csvRow += String.Format($"IotHubIngressRate,");
        csvRow += String.Format($"DeviceEgressThroughputKBs,");
        csvRow += String.Format($"IotHubIngressThroughputKBs,");
        csvRow += String.Format($"DeviceToIotHubAvgLatency,DeviceToIotHubMinLatency,DeviceToIotHubMaxLatency,");
        csvRow += String.Format($"DeviceAvgTransmissionDuration,DeviceMinTransmissionDuration,DeviceMaxTransmissionDuration,");
        csvRow += String.Format($"CustomLabel");
        csvRow += String.Format($"\n");

        return csvRow;
    }
}

namespace IotEdgePerf.Analysis
{
    partial class Analyzer
    {
        List<AsaMessage> _messagesList;
        TransmitterConfigData _config;
        string _iotHubHostname;
        string _deviceId;
        string _csvFilename;
        string _customLabel;

        public Analyzer(
            string iotHubHostname,
            string deviceId,
            TransmitterConfigData config, string csvFilename, string customLabel)
        {
            this._messagesList = new List<AsaMessage>();
            this._config = config;
            this._csvFilename = csvFilename;
            this._customLabel = customLabel;
            this._deviceId= deviceId;
            this._iotHubHostname = iotHubHostname;
        }

        public void Add(AsaMessage msg)
        {
            this._messagesList.Add(msg);
        }

        public void DoAnalysis()
        {
            // order by asa timestamp
            _messagesList.OrderBy(msg => msg.t);

            // group by burstCounter
            var query = from item in _messagesList
                        group item by item.sessionId into sessionGroup
                        orderby sessionGroup.Key ascending
                        select sessionGroup;

            // if needed, creates file with header
            if (!File.Exists(this._csvFilename))
                File.AppendAllText(_csvFilename, new AnalysisData().GetCsvHeader());

            // perform analysis
            foreach (var sessionGroup in query)
            {
                AsaMessage firstAsaMessage = sessionGroup.First();
                AsaMessage lastAsaMessage = sessionGroup.Last();

                var analysisData = new AnalysisData
                {
                    CustomLabel = this._customLabel,
                    Config = this._config,

                    iotHubHostname= this._iotHubHostname,
                    deviceId=this._deviceId,

                    SessionId = lastAsaMessage.sessionId,

                    // device
                    DeviceFirstMessageEpoch = firstAsaMessage.firstMessageEpoch,
                    DeviceLastMessageEpoch = lastAsaMessage.lastMessageEpoch,
                    DeviceSessionDuration = lastAsaMessage.lastMessageEpoch - firstAsaMessage.firstMessageEpoch,

                    DeviceMessageCount = lastAsaMessage.messageSequenceNumberInSession,

                    DeviceEgressRate = lastAsaMessage.sessionRollingRate,
                    DeviceTimingsViolationsCount = sessionGroup.Max(item => item.asaTimingViolationsCounter),

                    DeviceAvgTransmissionDuration = sessionGroup.Average(item => item.avgTransmissionDuration),
                    DeviceMinTransmissionDuration = sessionGroup.Min(item => item.minTransmissionDuration),
                    DeviceMaxTransmissionDuration = sessionGroup.Max(item => item.maxTransmissionDuration),

                    DeviceAvgCycleDuration = sessionGroup.Average(item => item.avgCycleDuration),
                    DeviceMinCycleDuration = sessionGroup.Min(item => item.minCycleDuration),
                    DeviceMaxCycleDuration = sessionGroup.Max(item => item.maxCycleDuration),

                    // iot hub
                    IotHubMessageCount = sessionGroup.Sum(item => item.asaMessageCount),

                    IotHubFirstMessageEpoch = firstAsaMessage.firstIotHubEpoch,
                    IotHubLastMessageEpoch = lastAsaMessage.lastIotHubEpoch,
                    IotHubSessionDuration = lastAsaMessage.lastIotHubEpoch - firstAsaMessage.firstIotHubEpoch,

                    IotHubIngressRate = lastAsaMessage.messageSequenceNumberInSession / (lastAsaMessage.lastIotHubEpoch - firstAsaMessage.firstIotHubEpoch) * 1000,

                    DeviceToIotHubAvgLatency = sessionGroup.Average(item => item.avgDeviceToHubLatency),
                    DeviceToIotHubMinLatency = sessionGroup.Min(item => item.minDeviceToHubLatency),
                    DeviceToIotHubMaxLatency = sessionGroup.Max(item => item.maxDeviceToHubLatency),

                    IotHubToAsaAvgLatency = sessionGroup.Average(item => item.avgHubToAsaLatency),
                    IotHubToAsaMinLatency = sessionGroup.Min(item => item.minHubToAsaLatency),
                    IotHubToAsaMaxLatency = sessionGroup.Max(item => item.maxHubToAsaLatency)
                };

                // throughput
                if (analysisData.DeviceEgressRate != null)
                    analysisData.DeviceEgressThroughputKBs = (double)analysisData.DeviceEgressRate * analysisData.Config.payloadLength / 1024;
                else
                    analysisData.DeviceEgressRate = 0;

                if (analysisData.IotHubIngressRate != null)
                    analysisData.IotHubIngressThroughputKBs = (double)analysisData.IotHubIngressRate * analysisData.Config.payloadLength / 1024;
                else
                    analysisData.IotHubIngressThroughputKBs = 0;

                // removes offset
                analysisData.DeviceToIotHubAvgLatency -= analysisData.DeviceToIotHubMinLatency;
                analysisData.DeviceToIotHubMinLatency -= analysisData.DeviceToIotHubMinLatency;
                analysisData.DeviceToIotHubMaxLatency -= analysisData.DeviceToIotHubMinLatency;

                // print to screen
                analysisData.Show();

                // saves to CSV file
                File.AppendAllText(_csvFilename, analysisData.ToCsvString());
            }


            Console.WriteLine($"\nOutput written to {_csvFilename}");
        }
    }
}
