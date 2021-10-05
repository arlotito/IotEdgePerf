namespace IoTEdgePerf.Transmitter
{
    using System;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    using Newtonsoft.Json;
    using IoTEdgePerf.Shared;
    
    public static class TransmitterConfig
    {
        static string GetEnv(string name, string description)
        {
            string value = Environment.GetEnvironmentVariable(name);

            if (value == null)
            {
                Console.WriteLine("The required environment variable '{0}' was not found!", name);
                throw new System.ArgumentNullException();
            }
            else
            {
                Console.WriteLine($"{name}, {description}: {value}");
                return value;
            }

        }

        static public TransmitterConfigData GetFromTwin(TwinCollection desiredProperties)
        {
            var config = JsonConvert.DeserializeObject<TransmitterConfigData>(JsonConvert.SerializeObject(desiredProperties["sourceOptions"]));

            //Console.WriteLine("\nParsed configuration:");
            //Console.WriteLine(JsonConvert.SerializeObject(config, Formatting.Indented));

            return config;
        }
        static public TransmitterConfigData GetFromEnv()
        {
            TransmitterConfigData options = new TransmitterConfigData();

            Console.WriteLine("");
            Console.WriteLine("Getting the configuration from ENV variables:");
            options.burstLength = Int32.Parse(GetEnv("BURST_LENGTH", "Burst length [msg]"));
            options.burstWait = Int32.Parse(GetEnv("BURST_WAIT", "Delay between bursts [ms]"));
            options.burstNumber = Int32.Parse(GetEnv("BURST_NUMBER", "Burst number [burst]"));
            options.targetRate = Int32.Parse(GetEnv("TARGET_RATE", "Target rate [msg/s]"));
            options.payloadLength = Int32.Parse(GetEnv("MESSAGE_PAYLOAD_LENGTH", "Message payload length [bytes]"));
            options.batchSize = Int32.Parse(GetEnv("BATCH_SIZE", "Size of the batch sent using the SendEventBatchAsync'"));
            options.logMsg = Boolean.Parse(GetEnv("LOG_MSG", "Log each message"));
            options.logBurst = Boolean.Parse(GetEnv("LOG_BURST", "Log stats of each burst"));
            options.logHist = Boolean.Parse(GetEnv("LOG_HIST", "Log histograms"));

            options.waitBeforeStart = Int32.Parse(GetEnv("START_WAIT", "Wait before starting [ms]"));
            options.rateCalcPeriod = Int32.Parse(GetEnv("RATE_CALC_PERIOD", "Calculation period of rate [ms]")); //ms

            return options;
        }
    }
}