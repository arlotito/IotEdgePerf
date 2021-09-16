using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Timers;
using Prometheus;

// dotnet add package prometheus-net

namespace edgeBenchmark
{
    public class MeterChannel
    {
        public double rate, elapsed, delta;
        double currValue, prevValue, prevElapsed;
        int count;
        bool newValue;

        Stopwatch watch;

        public MeterChannel(Stopwatch stopwatch)
        {
            newValue=false;
            watch = stopwatch;
        }

        public void Set(double value)
        {
            currValue = value;
            count++;
            newValue=true;
        }

        public double Calculate()
        {
            if (newValue)
            {
                double now = watch.ElapsedMilliseconds;
                elapsed = now - prevElapsed;
                prevElapsed = now;
                
                delta = Math.Abs(currValue - prevValue);
                rate = delta / elapsed * 1000;
                prevValue = currValue;
            }
            
            return rate;
        } 

        public override string ToString()
        {
            return String.Format("{0:0.00},{1:0.00},{2:0.00}", elapsed, delta, rate);
        }

    }
    public class RateMeter
    {
        Stopwatch stopwatch;

        public Dictionary<string, MeterChannel> channels = new Dictionary<string, MeterChannel>();

        double prevValue = 0;

        System.Timers.Timer timer;

        string deviceId, instanceNumber, iothubHostname, moduleId;

        // private readonly Counter CustomCounterMetric =
        //     Metrics.CreateCounter(
        //         "sink_counter", 
        //         "Cumulative counter, increment only every 500 msec",  
        //         labelNames: new[] { "edge_device_id", "instance_id", "iothub_name", "module_id" } 
        //     );

        private readonly Gauge RateMetric = 
            Metrics.CreateGauge(
                "rate", 
                "Gauges can have any numeric value and change arbitrarily, random number every sec", 
                labelNames: new[] { "edge_device_id", "instance_id", "iothub_name", "module_id", "type", "input_index" }
            );
        
        // private readonly Summary CustomSummaryMetric = 
        //     Metrics.CreateSummary(
        //         "sink_summary", 
        //         "Summaries track the trends in events over time (10 minutes by default).", 
        //         labelNames: new[] { "edge_device_id", "instance_id", "iothub_name", "module_id" });

        public RateMeter(int period, string[] inputs)
        {
            timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler((s,e) => this.OnTimedEvent());
            timer.Interval = period;
            timer.Enabled = true;

            stopwatch = new Stopwatch();
            stopwatch.Start();

            deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            instanceNumber = Guid.NewGuid().ToString();
            iothubHostname = Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");
            moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");

            foreach (string inputName in inputs)
            {
                channels[inputName] = new MeterChannel(stopwatch);
                RateMetric.WithLabels(deviceId, instanceNumber, iothubHostname, moduleId, "individual", inputName).Set(0);
            }
        }

        public void Update(double value, string name)
        {
            channels[name].Set(value);
        }

        public void OnTimedEvent()
        {
            bool somethingToShow = false;
            
            String logString; 

            logString = string.Format("{0},prometheus,{1}", 
                DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.ffffffK"),
                deviceId
                );

            foreach (KeyValuePair<string, MeterChannel> ch in channels)
            {
                // Console.WriteLine("Key = {0}, Value = {1}", item.Key, item.Value);
                double rate = ch.Value.Calculate();
                
                //send only changes
                if (rate != prevValue)
                {
                    logString += string.Format("," + ch.Key + "," + ch.Value.ToString());
                    RateMetric.WithLabels(deviceId, instanceNumber, iothubHostname, moduleId, "individual", ch.Key).Set(rate);

                    somethingToShow = true;
                }
                
                prevValue = rate;
            }

            if (somethingToShow)
                Console.WriteLine(logString);
        }
    }
}