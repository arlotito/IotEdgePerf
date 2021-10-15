﻿using IotEdgePerf.Shared;
using IotEdgePerf.Transmitter.ConfigData;
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
            Console.WriteLine($"session ID:     {SessionId}");
            Console.WriteLine($"IoT HUB:        {iotHubHostname}");
            Console.WriteLine($"device ID:      {deviceId}");
            Console.WriteLine($"burst counter:  {BurstCounter}");
            Console.WriteLine($"message count:  {DeviceMessageCount} [msg]");
            Console.WriteLine($"message size:   {Config.payloadLength} [bytes]");
            Console.WriteLine($"raget rate:     {Config.targetRate} [msg/s]");
            Console.WriteLine("");
            Console.WriteLine($"                                SOURCE (out)  =>    (in) IoT HUB");
            Console.WriteLine($"                                --------------------------------");
            Console.WriteLine($"single msg tx duration [ms]:    {DeviceAvgTransmissionDuration:0.00} ({DeviceMinTransmissionDuration:0.00}/{DeviceMaxTransmissionDuration:0.00})");
            Console.WriteLine($"single msg cycle duration [ms]: {DeviceAvgCycleDuration:0.00} ({DeviceMinCycleDuration:0.00}/{DeviceMaxCycleDuration:0.00})");
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
            csvRow += String.Format($"DeviceAvgTransmissionDuration,DeviceMinTransmissionDuration,DeviceMaxTransmissionDuration,");
            csvRow += String.Format($"CustomLabel");
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

        public int AddMessage(AsaMessage msg)
        {
            this._messagesList.Add(msg);
            this._receivedMessageCount += (int)msg.asaMessageCount;

            return this._receivedMessageCount;
        }

        public void DoAnalysis()
        {
            // order by asa timestamp
            _messagesList.OrderBy(msg => msg.t);

            // group by burstCounter
            var query = from item in _messagesList
                        group item by (item.sessionId, item.burstCounter) into sessionGroup
                        orderby sessionGroup.Key ascending
                        select sessionGroup;

            // if needed, creates file with header
            if (!File.Exists(this._csvFilename))
                File.AppendAllText(_csvFilename, new BurstAnalysisData().GetCsvHeader());

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

                // print to screen
                burstAnalysisData.Show();

                // saves to CSV file
                File.AppendAllText(_csvFilename, burstAnalysisData.ToCsvString());
            }


            Console.WriteLine($"\nOutput written to {_csvFilename}");
        }
    }
}

    
        
