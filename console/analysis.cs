

using System;
using System.Linq;

namespace eh_consumer
{
    partial class Program
    {

        private static void Analyze()
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
                
                // extracts measurement done by ASA
                double asaEstimatedRateIotHub = burstGroup.Last().asaEstimatedRateIotHub; 

                // gets first and last SOURCE timestamp
                var firstMessageDtInBurst = DateTime.Parse(burstGroup.First().firstMsgTs);
                var lastMessageDtInBurst = DateTime.Parse(burstGroup.Last().lastMsgTs);
                TimeSpan burstDurationSource = lastMessageDtInBurst - firstMessageDtInBurst;
                double averageRateInBurstSource = burstLength / burstDurationSource.TotalSeconds;

                // gets first and last IoT HUB enqueuement timestamp (unix epoch)
                var firstIotHubEpoch = burstGroup.First().firstIotHubEpoch;
                var lastIotHubEpoch = burstGroup.Last().lastIotHubEpoch;
                double burstDurationIotHub = (lastIotHubEpoch - firstIotHubEpoch) / 1000;
                double averageRateInBurstIotHub = burstLength / burstDurationIotHub;

                // stats on latency
                double avgLatency = burstGroup.Average(item => item.asaAvgLatency);
                double minLatency = burstGroup.Min(item => item.asaAvgLatency);
                double maxLatency = burstGroup.Max(item => item.asaAvgLatency);

                Console.Write($"#: {burstGroup.Key},");
                Console.Write($"msg sent: {burstLength},");
                Console.Write($"source/iothub [msg/s]: {averageRateInBurstSource:0.00}/{averageRateInBurstIotHub:0.00},");
                Console.Write($"latency (avg/min/max) [ms]: {avgLatency:0.00}/{minLatency:0.00}/{maxLatency:0.00}");
                Console.WriteLine();

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
                    Console.WriteLine($"    avg rate [msg/s]: {averageRateInBurstSource:0.00}");
                    Console.WriteLine("\n");
                    Console.WriteLine($"IoT HUB ingress:");
                    Console.WriteLine($"    First message ts: {firstIotHubEpoch}");
                    Console.WriteLine($"    Last message ts: {lastIotHubEpoch}");
                    Console.WriteLine($"    Delta ts [s]: {burstDurationIotHub:0.00}");
                    Console.WriteLine($"    avg rate ASA [msg/s]: {asaEstimatedRateIotHub:0.00}");
                    Console.WriteLine($"    avg rate [msg/s]: {averageRateInBurstIotHub:0.00}");
                    Console.WriteLine($"    avg (min/max) latency [ms]: {avgLatency:0.00} ({minLatency:0.00},{maxLatency:0.00})");
                    Console.WriteLine("\n------\n\n");
                }
                
            }
        }
    }
}
