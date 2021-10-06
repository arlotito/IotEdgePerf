using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using IoTEdgePerf.Shared;

namespace IoTEdgePerf.Analysis
{
    partial class Analyzer
    {
        public List<AsaMessage> MessagesList;
        public TransmitterConfigData config;

        public string csvFile;
        public string testLabel;

        public Analyzer(TransmitterConfigData config, string csvFile, string testLabel)
        {
            this.MessagesList = new List<AsaMessage>();
            this.config = config;
            this.csvFile = csvFile;
            this.testLabel = testLabel;
        }

        public void Add(AsaMessage msg)
        {
            this.MessagesList.Add(msg);
        }

        public void Do()
        {
            bool logBurstDetails = false;

            // order by asa timestamp
            MessagesList.OrderBy(msg => msg.t);

            // group by burstCounter
            var query = from item in MessagesList
                        group item by item.burstCounter into burstGroup
                        orderby burstGroup.Key ascending
                        select burstGroup;

            
                // perform analysis
                foreach (var burstGroup in query)
                {
                    var burstLength = burstGroup.Last().asaBurstLength;
                    var runId = burstGroup.Last().runId;

                    // extracts measurement done by ASA
                    double asaEstimatedRateIotHub = burstGroup.Last().asaEstimatedRateIotHub;

                    // gets first and last SOURCE timestamp
                    var firstMessageDtInBurst = DateTime.Parse(burstGroup.First().firstMsgTs);
                    var lastMessageDtInBurst = DateTime.Parse(burstGroup.Last().lastMsgTs);
                    TimeSpan burstDurationSource = lastMessageDtInBurst - firstMessageDtInBurst;
                    double avgRateSource = burstLength / burstDurationSource.TotalSeconds;

                    // gets first and last IoT HUB enqueuement timestamp (unix epoch)
                    var firstIotHubEpoch = burstGroup.First().firstIotHubEpoch;
                    var lastIotHubEpoch = burstGroup.Last().lastIotHubEpoch;
                    double burstDurationIotHub = (lastIotHubEpoch - firstIotHubEpoch) / 1000;
                    double avgRateIotHub = burstLength / burstDurationIotHub;

                    // stats on latency
                    double avgLatency = burstGroup.Average(item => item.asaAvgLatency);
                    double minLatency = burstGroup.Min(item => item.asaMinLatency);
                    double maxLatency = burstGroup.Max(item => item.asaMaxLatency);

                    // throughput [KB/s]
                    double sourceThroughput = (avgRateSource * config.payloadLength) / 1024;
                    double iotHubThroughput = (avgRateIotHub * config.payloadLength) / 1024;

                    Console.Write($"#: {burstGroup.Key},");
                    Console.Write($"total: {burstLength},");
                    Console.Write($"msgSize: {config.payloadLength},");
                    Console.Write($"source/iothub: {avgRateSource:0.00}/{avgRateIotHub:0.00} [msg/s],");
                    Console.Write($"{sourceThroughput:0.00}/{iotHubThroughput:0.00} [KB/s],");
                    Console.Write($"latency (avg/min/max): {avgLatency:0.00}/{minLatency:0.00}/{maxLatency:0.00} [ms]");
                    Console.WriteLine();

                    string csvRow = String.Format($"{runId},");
                    csvRow += String.Format($"{burstGroup.Key},");
                    csvRow += String.Format($"{burstLength},");
                    csvRow += String.Format($"{config.payloadLength},");
                    csvRow += String.Format($"{avgRateSource:0.00},{avgRateIotHub:0.00},");
                    csvRow += String.Format($"{sourceThroughput:0.00},{iotHubThroughput:0.00},");
                    csvRow += String.Format($"{avgLatency:0.00},{minLatency:0.00},{maxLatency:0.00},");
                    csvRow += String.Format($"{this.testLabel}");
                    csvRow += String.Format($"\n");
                    //Console.WriteLine(csvRow);

                    File.AppendAllText(csvFile, csvRow);

                    if (logBurstDetails)
                    {
                        // detailed
                        Console.WriteLine(string.Format("Run ID: {0}", burstGroup.Last().runId));
                        Console.WriteLine(string.Format("Burst ID: {0}", burstGroup.Key));
                        Console.WriteLine($"Total messages: {burstLength}");

                        Console.WriteLine($"Source:");
                        Console.WriteLine($"    First message ts: {firstMessageDtInBurst.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
                        Console.WriteLine($"    Last message ts: {lastMessageDtInBurst.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")}");
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

                }
            


        }
    }
}
