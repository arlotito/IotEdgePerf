namespace IotEdgePerf.Transmitter.Edge
{
    using System;
    using Microsoft.Azure.Devices.Shared; // For TwinCollection
    using Newtonsoft.Json;
    using IotEdgePerf.Shared;
    using Serilog;
    
    public static class TransmitterConfig
    {
        static string GetEnv(string name, string description)
        {
            string value = Environment.GetEnvironmentVariable(name);

            if (value == null)
            {
                Log.Error("The required environment variable '{0}' was not found!", name);
                throw new System.ArgumentNullException();
            }
            else
            {
                Log.Information($"{name}, {description}: {value}");
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
    }
}