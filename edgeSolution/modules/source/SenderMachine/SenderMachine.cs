namespace source
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    using System.Collections.Generic;
    using Newtonsoft.Json;
    
    using edgeBenchmark;

    public class SenderMachine
    {
        RateMeter rateMeter;

        StatsCalculator stats;

        string moduleOutput;

        bool doReset = false;

        ModuleClient moduleClient;
        
        public SenderMachine(ModuleClient moduleClient, string moduleOutput)
        {
            stats = new StatsCalculator(200, 1);
            this.moduleClient=moduleClient;
            this.moduleOutput=moduleOutput;
        }

        public void RequestReset()
        {
            doReset = true;
        }

        /// <summary>
        /// Creates a string of specified length with random chars 
        /// (from "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789") 
        /// </summary>
        /// <param name="length">Number of chars</param>
        /// <returns>the string</returns>
        String RandomString(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new String(stringChars);
        }
        
        public async Task SendMessages(SenderMachineConfigData config)
        {
            Stopwatch transmissionStopwatch = new Stopwatch(); //measures the transmission duration
            Stopwatch burstStopwatch = new Stopwatch(); // measures time during burst
            Stopwatch runStopwatch = new Stopwatch();   // measures time during the entire run
            Guid runId = Guid.NewGuid();

            doReset = false;

            int burstMsgCnt = 0;        // counts messages in a burst
            int burstCnt = 0;           // counts bursts
            int totalMessageCount = 0;  // all messages across all bursts 
            int runMsgTotal = config.burstLength * config.burstNumber; // number of messages that will be sent in this run

            // show current config
            Console.WriteLine(JsonConvert.SerializeObject(config, Formatting.Indented));

            // wait before starting
            Console.WriteLine("");
            Console.WriteLine($"Waiting {config.waitBeforeStart} ms before starting...");
            Thread.Sleep(config.waitBeforeStart);

            // initialize and start the rate meter
            rateMeter = new RateMeter(config.rateCalcPeriod, new string[] { this.moduleOutput });

            Console.WriteLine($"Run ID: {runId.ToString()}");

            // calculate period from desired rate
            double waitBeforeNextMessage = 0; //if 0, there won't be any delay
            if (config.targetRate > 0)
                waitBeforeNextMessage = 1 / (double)config.targetRate * 1000;
            Console.WriteLine($"(calculated) wait between messages: {waitBeforeNextMessage} ms");

            Console.WriteLine("\n\n");

            if (config.burstLength == 0)
            {
                Console.WriteLine("Burst length is 0. Nothing to do.");
                return;
            }

            if (config.burstNumber == 0)
            {
                Console.WriteLine("Burst number is 0. Nothing to do.");
                return;
            }

            if (config.batchSize < 1)
            {
                Console.WriteLine("batchSize must be >= 1.");
                return;
            }

            runStopwatch.Restart();
            burstStopwatch.Restart(); // 1st burst starts here

            while (true)
            {
                var start = burstStopwatch.Elapsed.TotalMilliseconds; // for rate adjustment
                var waitUntil = start + waitBeforeNextMessage;
                var messageBatch = new List<Message>();

                if (doReset)
                {
                    Console.WriteLine("Reset requested\n");
                    doReset = false;
                    return;
                }

                if (burstMsgCnt == 0)
                    Console.WriteLine("{0},text,sending burst...", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"));

                for (int k = 0; k < config.batchSize; k++)
                {
                    // IMPORTANT: do not move this outside of the loop, otherwise you'll get an exception
                    var payload = new MessageDataPoint
                    {
                        ts = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                        runId = runId.ToString(),
                        
                        //burst
                        burstCounter = burstCnt + 1,
                        burstMsgCounter = burstMsgCnt + 1,
                        burstLength = config.burstLength,
                        burstElapsed = burstStopwatch.Elapsed.TotalMilliseconds,

                        //run
                        runMsgCounter = totalMessageCount + 1,
                        runMsgTotal = runMsgTotal,
                        runElapsed = runStopwatch.Elapsed.TotalMilliseconds,
                        
                        payload = RandomString(config.payloadLength)
                    };
                    string messageString = "";
                    messageString = JsonConvert.SerializeObject(payload);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));

                    messageBatch.Add(message);

                    burstMsgCnt += 1;
                    totalMessageCount += 1;
                }

                // An IoT hub can filter on these properties without access to the message body.  
                //message.Properties.Add("<key>", value);

                // Send the telemetry message
                transmissionStopwatch.Restart();
                //Console.WriteLine($"sending packet: {counter}, inc/burst: {packet}/{burst}, size: {size}, delay ms before next: {interpacket_interval_ms}, delay ms vs expected: {delay_ms}...");

                if (messageBatch.Count == 1)
                    await moduleClient.SendEventAsync(this.moduleOutput, messageBatch[0]);
                else
                    await moduleClient.SendEventBatchAsync(this.moduleOutput, messageBatch);
                transmissionStopwatch.Stop();

                double elapsedMilliseconds = transmissionStopwatch.Elapsed.TotalMilliseconds; //this has the same precision as ElapsedTicks
                stats.Update(elapsedMilliseconds);

                double burstElapsedMilliseconds = burstStopwatch.Elapsed.TotalMilliseconds;

                rateMeter.Update(totalMessageCount, this.moduleOutput);

                if (config.logMsg)
                {
                    Console.Write($"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")},");
                    Console.Write($"message,");
                    Console.Write($"{burstCnt},");
                    Console.Write($"{burstMsgCnt},");
                    Console.Write($"{stats.count:0.000},");
                    Console.Write($"{elapsedMilliseconds:0.000},");
                    Console.Write($"{stats.average:0.000},");
                    Console.Write($"{stats.min:0.000},");
                    Console.Write($"{stats.max:0.000},");
                    Console.Write($"{burstElapsedMilliseconds:0.000},");
                    Console.Write($"{rateMeter.channels[this.moduleOutput].rate:0.000}");
                    Console.WriteLine("");
                }

                if (burstMsgCnt >= config.burstLength)
                {
                    burstCnt++;

                    if (config.logBurst)
                    {
                        Console.Write($"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")},");
                        Console.Write($"burst,");
                        Console.Write($"{burstCnt},");
                        Console.Write($"{burstMsgCnt},");
                        Console.Write($"{stats.average:0.000},");
                        Console.Write($"{stats.min:0.000},");
                        Console.Write($"{stats.max:0.000},");
                        Console.Write($"{burstElapsedMilliseconds:0.000},");
                        Console.Write($"{rateMeter.channels[this.moduleOutput].rate:0.000}");
                        Console.WriteLine("");
                    }

                    if (config.logHist)
                    {
                        stats.Print();
                    }

                    await Task.Delay(config.burstWait); //wait even if last burst

                    if (burstCnt < config.burstNumber)
                    {
                        // next burst
                        burstMsgCnt = 0;
                        burstStopwatch.Restart();
                    }
                    else
                    {
                        //completed
                        while (!doReset)
                        {
                        }

                        Console.WriteLine("Reset requested\n");
                    }

                }
                else //wait time between one message and the next
                {
                    while (burstStopwatch.Elapsed.TotalMilliseconds < waitUntil)
                    {
                        //wait
                    }
                }

            }
        }
        
    }
}