using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using IotEdgePerf.Shared;

namespace IotEdgePerf.Analysis
{
    partial class Analyzer
    {
        List<AsaMessage> _messagesList;
        TransmitterConfigData _config;
        string _csvFilename;
        string _customLabel;

        public Analyzer(TransmitterConfigData config, string csvFilename, string customLabel)
        {
            this._messagesList = new List<AsaMessage>();
            this._config = config;
            this._csvFilename = csvFilename;
            this._customLabel = customLabel;
        }

        public void Add(AsaMessage msg)
        {
            this._messagesList.Add(msg);
        }

        public void AnalyzeData()
        {
            //bool logBurstDetails = false;

            // order by asa timestamp
            _messagesList.OrderBy(msg => msg.t);

            // group by burstCounter
            var query = from item in _messagesList
                        group item by item.sessionId into sessionGroup
                        orderby sessionGroup.Key ascending
                        select sessionGroup;

            // perform analysis
            foreach (var sessionGroup in query)
            {
                AsaMessage firstAsaMessage = sessionGroup.First();
                AsaMessage lastAsaMessage = sessionGroup.Last();
                string sessionId = lastAsaMessage.sessionId;

                //source
                var firstMessageEpoch = firstAsaMessage.firstMessageEpoch;
                var lastMessageEpoch = lastAsaMessage.lastMessageEpoch;
                double sessionDeviceDuration = lastMessageEpoch - firstMessageEpoch;
                double? deviceRollingRate = lastAsaMessage.sessionRollingRate;
                double? asaTimingsViolationsCount = sessionGroup.Max(item => item.asaTimingViolationsCounter);
                double? avgTransmissionDuration = sessionGroup.Average(item => item.avgTransmissionDuration);
                double? minTransmissionDuration = sessionGroup.Min(item => item.minTransmissionDuration);
                double? maxTransmissionDuration = sessionGroup.Max(item => item.maxTransmissionDuration);
                double? avgCycleDuration = sessionGroup.Average(item => item.avgCycleDuration);
                double? minCycleDuration = sessionGroup.Min(item => item.minCycleDuration);
                double? maxCycleDuration = sessionGroup.Max(item => item.maxCycleDuration);
                
                double iotHubMessageCount = sessionGroup.Sum(item => item.asaMessageCount);
                var firstIotHubMessageEpoch = firstAsaMessage.firstIotHubEpoch;
                var lastIotHubMessageEpoch = lastAsaMessage.lastIotHubEpoch;
                double sessionIotHubDuration = lastIotHubMessageEpoch - firstIotHubMessageEpoch;
                
                //double asaEstimatedRate         = lastAsaMessage.messageSequenceNumberInSession / ;
                double asaEstimatedRateIotHub   = lastAsaMessage.messageSequenceNumberInSession / sessionIotHubDuration * 1000;
                //double asaEstimatedRateAsa      = lastAsaMessage.messageSequenceNumberInSession / sessiosIotHubDuration;
                

                double avgDeviceToHubLatency = sessionGroup.Average(item => item.avgDeviceToHubLatency);
                double minDeviceToHubLatency = sessionGroup.Min(item => item.minDeviceToHubLatency);
                double maxDeviceToHubLatency = sessionGroup.Max(item => item.maxDeviceToHubLatency);
                double avgHubToAsaLatency = sessionGroup.Average(item => item.avgHubToAsaLatency);
                double minHubToAsaLatency = sessionGroup.Min(item => item.minHubToAsaLatency);
                double maxHubToAsaLatency = sessionGroup.Max(item => item.maxHubToAsaLatency);

                //remove latency offset
                avgDeviceToHubLatency -= minDeviceToHubLatency;
                minDeviceToHubLatency -= minDeviceToHubLatency;
                maxDeviceToHubLatency -= minDeviceToHubLatency;

                Console.WriteLine("");
                Console.Write($"SOURCE (out)    -->     edgeHub     -->     (in) IoT HUB    /   latency (device to hub)");
                
                Console.WriteLine("");
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"{deviceRollingRate:0.00} [msg/s]");
                Console.SetCursorPosition(44, Console.CursorTop);
                Console.Write($"{asaEstimatedRateIotHub:0.00} [msg/s]");
                Console.SetCursorPosition(64, Console.CursorTop);
                Console.Write($"avg:{avgDeviceToHubLatency:0.00}, min:{minDeviceToHubLatency:0.00}, max:{maxDeviceToHubLatency:0.00} [ms]");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("");

                Console.WriteLine("");
                Console.WriteLine($"session ID:                             {lastAsaMessage.sessionId}");
                Console.WriteLine($"message count:                          {lastAsaMessage.messageSequenceNumberInSession} [msg]");
                Console.WriteLine($"msgSize:                                {_config.payloadLength} [bytes]");
                Console.WriteLine("");
                Console.WriteLine($"SOURCE:");
                Console.WriteLine($"    fist msg epoch:                         {firstMessageEpoch} [epoch ms]");
                Console.WriteLine($"    last msg epoch:                         {lastMessageEpoch} [epoch ms]");
                Console.WriteLine($"    session duration:                       {sessionDeviceDuration} [ms]");
                //Console.WriteLine($"    rate (calculated):              {avgRateSource:0.0} [msg/s]");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    rate at device egress:                  {deviceRollingRate:0.00} [msg/s]");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"    avg (min/max) transmission duration:    {avgTransmissionDuration:0.00} ({minTransmissionDuration:0.00}/{maxTransmissionDuration:0.00}) [ms]");
                Console.WriteLine($"    avg (min/max) cycle duration:           {avgCycleDuration:0.00} ({minCycleDuration:0.00}/{maxCycleDuration:0.00}) [ms]");
                Console.WriteLine($"    timing violations count:                {asaTimingsViolationsCount:0.00}");


                Console.WriteLine("");
                Console.WriteLine($"IOT HUB:");
                Console.WriteLine($"    message count:                          {iotHubMessageCount} [msg]");
                Console.WriteLine($"    fist msg epoch:                         {firstIotHubMessageEpoch} [epoch ms]");
                Console.WriteLine($"    last msg epoch:                         {lastIotHubMessageEpoch} [epoch ms]");
                Console.WriteLine($"    session duration:                       {sessionIotHubDuration:0.00} [ms]");
                //Console.WriteLine($"    rate (at IoT HUB ingress, count/5s)     {asaEstimatedRate:0.00} [msg/s]");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    rate (at IoT HUB ingress, ts):          {asaEstimatedRateIotHub:0.00} [msg/s]");
                Console.ForegroundColor = ConsoleColor.White;
                //Console.WriteLine($"    rate (at ASA ingress):                  {asaEstimatedRateAsa:0.00} [msg/s]"); 
                
                Console.WriteLine("");
                Console.WriteLine($"LATENCY:");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"    device to hub avg (min/max) latency:    {avgDeviceToHubLatency:0.00} ({minDeviceToHubLatency:0.00}/{maxDeviceToHubLatency:0.00}) [ms]");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"    hub to asa avg (min/max) latency:       {avgHubToAsaLatency:0.00} ({minHubToAsaLatency:0.00}/{maxHubToAsaLatency:0.00}) [ms]");


                /*
                Console.Write($"source/iothub: {avgRateSource:0.00}/{avgRateIotHub:0.00} [msg/s],");
                Console.Write($"{sourceThroughput:0.00}/{iotHubThroughput:0.00} [KB/s],");
                Console.Write($"latency (avg/min/max): {avgLatency:0.00}/{minLatency:0.00}/{maxLatency:0.00} [ms]");
                Console.WriteLine();


                // gets first and last IoT HUB enqueuement timestamp (unix epoch)
                var firstIotHubEpoch = sessionGroup.First().firstIotHubEpoch;
                    var lastIotHubEpoch = sessionGroup.Last().lastIotHubEpoch;
                    double burstDurationIotHub = (lastIotHubEpoch - firstIotHubEpoch) / 1000;
                    double avgRateIotHub = sessionGroup / burstDurationIotHub;

                    // stats on latency
                    double avgLatency = sessionGroup.Average(item => item.asaAvgLatency);
                    double minLatency = sessionGroup.Min(item => item.asaMinLatency);
                    double maxLatency = sessionGroup.Max(item => item.asaMaxLatency);

                    // throughput [KB/s]
                    double sourceThroughput = (avgRateSource * _config.payloadLength) / 1024;
                    double iotHubThroughput = (avgRateIotHub * _config.payloadLength) / 1024;

                    Console.Write($"#: {burstGroup.Key},");
                    Console.Write($"total: {burstLength},");
                    Console.Write($"msgSize: {_config.payloadLength},");
                    Console.Write($"source/iothub: {avgRateSource:0.00}/{avgRateIotHub:0.00} [msg/s],");
                    Console.Write($"{sourceThroughput:0.00}/{iotHubThroughput:0.00} [KB/s],");
                    Console.Write($"latency (avg/min/max): {avgLatency:0.00}/{minLatency:0.00}/{maxLatency:0.00} [ms]");
                    Console.WriteLine();

                    string csvRow = String.Format($"{sessionId},");
                    csvRow += String.Format($"{burstGroup.Key},");
                    csvRow += String.Format($"{burstLength},");
                    csvRow += String.Format($"{_config.payloadLength},");
                    csvRow += String.Format($"{avgRateSource:0.00},{avgRateIotHub:0.00},");
                    csvRow += String.Format($"{sourceThroughput:0.00},{iotHubThroughput:0.00},");
                    csvRow += String.Format($"{avgLatency:0.00},{minLatency:0.00},{maxLatency:0.00},");
                    csvRow += String.Format($"{this._customLabel}");
                    csvRow += String.Format($"\n");
                    //Console.WriteLine(csvRow);

                    File.AppendAllText(_csvFilename, csvRow);

                    if (logBurstDetails)
                    {
                        // detailed
                        Console.WriteLine(string.Format("Run ID: {0}", burstGroup.Last().runId));
                        Console.WriteLine(string.Format("Burst ID: {0}", burstGroup.Key));
                        Console.WriteLine($"Total messages: {burstLength}");

                        Console.WriteLine($"Source:");
                        Console.WriteLine($"    First message ts: {firstMessageEpochInSession.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                        Console.WriteLine($"    Last message ts: {lastMessageEpochInSession.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                        Console.WriteLine($"    Delta ts [s]: {burstDurationSource.TotalSeconds}");
                        Console.WriteLine($"    avg rate [msg/s]: {avgRateSource:0.00}");
                        Console.WriteLine("\n");
                        Console.WriteLine($"IoT HUB ingress:");
                        Console.WriteLine($"    First message ts: {firstIotHubEpoch}");
                        Console.WriteLine($"    Last message ts: {lastIotHubEpoch}");
                        Console.WriteLine($"    Delta ts [s]: {burstDurationIotHub:0.00}");
                        Console.WriteLine($"    avg rate ASA [msg/s]: {asaEstimatedRateIotHub:0.00}");
                        Console.WriteLine($"    avg rate [msg/s]: {avgRateIotHub:0.00}");
                        Console.WriteLine($"    avg (min/max) latency [ms]: {avgLatency:0.00} ({minLatency:0.00},{maxLatency:0.00})");
                        Console.WriteLine("\n------\n\n");
                    }
                */
            }



        }
    }
}
