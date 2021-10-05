namespace IoTEdgePerf.Transmitter
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Azure.Devices.Client;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    
    using IoTEdgePerf.Shared;
    using IoTEdgePerf.Tools;
    
    public partial class Transmitter : ITransmitter
    {
        StatsCalculator stats;

        string moduleOutput;

        bool ResetRequest = false;

        ModuleClient moduleClient;

        TransmitterConfigData config;

        Stopwatch transmissionStopwatch; //measures the transmission duration
        Stopwatch burstStopwatch; // measures time during burst
        Stopwatch runStopwatch;   // measures time during the entire run
        Guid runId;
        int burstMsgCnt;        // counts messages in a burst
        int burstCnt;           // counts bursts
        int totalMessageCount;  // all messages across all bursts 
        int runMsgTotal; // number of messages that will be sent in this run
        double waitBeforeNextMessage; 
                
        public Transmitter(ModuleClient moduleClient, string moduleOutput)
        {
            this.moduleClient=moduleClient;
            this.moduleOutput=moduleOutput;
        }

        public void Restart(Guid runId)
        {
            ResetRequest = true;
            this.runId=runId;
        }

        public void Start(Guid runId, TransmitterConfigData config)
        {
            this.config = config;
            this.Restart(runId);
        }

        private void MachineReset()
        {
            transmissionStopwatch = new Stopwatch(); //measures the transmission duration
            burstStopwatch = new Stopwatch(); // measures time during burst
            runStopwatch = new Stopwatch();

            stats = new StatsCalculator(200, 1);
            
            burstMsgCnt = 0;        // counts messages in a burst
            burstCnt = 0;           // counts bursts
            totalMessageCount = 0;  // all messages across all bursts 
            runMsgTotal = this.config.burstLength * this.config.burstNumber; // number of messages that will be sent in this run
            waitBeforeNextMessage = 0; //if 0, there won't be any delay

            // show current config
            Console.WriteLine(JsonConvert.SerializeObject(this.config, Formatting.Indented));

            // wait before starting
            Thread.Sleep(this.config.waitBeforeStart);
            Console.WriteLine($"\nWaiting {config.waitBeforeStart} ms before starting...");
            
            //
            Console.WriteLine($"Run ID: {runId.ToString()}");

            // calculate period from desired rate
            if (this.config.targetRate > 0)
                waitBeforeNextMessage = 1 / (double)this.config.targetRate * 1000;
            Console.WriteLine($"(calculated) wait between messages: {waitBeforeNextMessage} ms");

            Console.WriteLine("\n\n");

            if (this.config.burstLength == 0)
            {
                Console.WriteLine("Burst length is 0. Nothing to do.");
                return;
            }

            if (this.config.burstNumber == 0)
            {
                Console.WriteLine("Burst number is 0. Nothing to do.");
                return;
            }

            if (this.config.batchSize < 1)
            {
                Console.WriteLine("batchSize must be >= 1.");
                return;
            }

            runStopwatch.Restart();
            burstStopwatch.Restart(); // 1st burst starts here
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
        
        public async Task SendMessagesAsync()
        {
            if (ResetRequest)
            {
                MachineReset();
                ResetRequest = false;
            }

            if (!config.enable)
            {
                // do nothing
                return;
            }

            while (true)
            {
                var start = burstStopwatch.Elapsed.TotalMilliseconds; // for rate adjustment
                var waitUntil = start + waitBeforeNextMessage;
                var messageBatch = new List<Message>();

                if (ResetRequest)
                {
                    Console.WriteLine("Reset requested\n");
                    ResetRequest = false;
                    return;
                }

                if (burstMsgCnt == 0)
                    Console.WriteLine("{0},text,sending burst...", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"));

                for (int k = 0; k < this.config.batchSize; k++)
                {
                    // IMPORTANT: do not move this outside of the loop, otherwise you'll get an exception
                    var payload = new PerfMessage
                    {
                        ts = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                        runId = runId.ToString(),
                        
                        //burst
                        burstCounter = burstCnt + 1,
                        burstMsgCounter = burstMsgCnt + 1,
                        burstLength = this.config.burstLength,
                        burstElapsed = burstStopwatch.Elapsed.TotalMilliseconds,

                        //run
                        runMsgCounter = totalMessageCount + 1,
                        runMsgTotal = runMsgTotal,
                        runElapsed = runStopwatch.Elapsed.TotalMilliseconds,
                        
                        payload = RandomString(this.config.payloadLength)
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

                if (this.config.logMsg)
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
                    Console.WriteLine("");
                }

                if (burstMsgCnt >= this.config.burstLength)
                {
                    burstCnt++;

                    if (this.config.logBurst)
                    {
                        double rate = burstMsgCnt / burstElapsedMilliseconds * 1000;
                        
                        Console.Write($"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK")},");
                        Console.Write($"burst,");
                        Console.Write($"{burstCnt},");
                        Console.Write($"{burstMsgCnt},");
                        Console.Write($"{stats.average:0.000},");
                        Console.Write($"{stats.min:0.000},");
                        Console.Write($"{stats.max:0.000},");
                        Console.Write($"{burstElapsedMilliseconds:0.000},");
                        Console.Write($"{rate:0.000}");
                        Console.WriteLine("");
                    }

                    if (this.config.logHist)
                    {
                        stats.Print();
                    }

                    await Task.Delay(this.config.burstWait); //wait even if last burst

                    if (burstCnt < this.config.burstNumber)
                    {
                        // next burst
                        burstMsgCnt = 0;
                        burstStopwatch.Restart();
                    }
                    else
                    {
                        //completed - wait until a reset is requested
                        while (!ResetRequest)
                        {
                        }

                        Console.WriteLine("Reset requested\n");
                        return;
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