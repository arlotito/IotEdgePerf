namespace IotEdgePerf.Transmitter
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Azure.Devices.Client;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    using Serilog;

    using IotEdgePerf.Shared;
    using IotEdgePerf.Profiler;

    public partial class Transmitter : ITransmitter
    {
        ModuleClient _moduleClient;
        string _moduleOutput;

        TransmitterConfigData _config;

        Guid _runId;

        bool _resetRequest;

        public Transmitter(ModuleClient moduleClient, string moduleOutput)
        {
            this._moduleClient = moduleClient;
            this._moduleOutput = moduleOutput;
            this._resetRequest = false;
        }

        public void Restart(Guid runId)
        {
            this._runId = runId;
            _resetRequest = true;
        }

        public void Start(Guid runId, TransmitterConfigData config)
        {
            this._config = config;
            Log.Information("New configuration applied:\n{0}", JsonConvert.SerializeObject(this._config, Formatting.Indented));

            this.Restart(runId);
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
            var profiler = new Profiler();
            double cyclePeriodMilliseconds;

            if (!_config.enable) return; // do nothing

            if (_resetRequest)
            {
                Log.Information("reset executed.");
                _resetRequest = false;
            }

            // wait before starting
            Log.Information($"waiting {_config.waitBeforeStart} ms before starting...");
            Thread.Sleep(this._config.waitBeforeStart);

            // 
            Log.Information($"Run ID: {_runId}");

            // calculates time period
            if (this._config.targetRate > 0)
                cyclePeriodMilliseconds = 1 / (double)this._config.targetRate * 1000;
            else
                cyclePeriodMilliseconds = 0;
            Log.Information($"(calculated) cycle period [ms]: {cyclePeriodMilliseconds}");

            for (int burstIndex = 0; burstIndex < this._config.burstNumber; burstIndex++)
            {
                profiler.SessionStart(_runId);

                Log.Debug("Burst index: {0}", burstIndex);

                for (int messageIndex = 0; messageIndex < this._config.burstLength; messageIndex++)
                {
                    profiler.MessageCycleStart();

                    Log.Debug("Message index: {0}", messageIndex);

                    // checks if a reset has been requested

                    if (_resetRequest)
                    {
                        Log.Information("reset request received. stops transmission.");
                        return; // not complete
                    }

                    var messageBatch = new List<Message>();

                    // create single message or batch of messages
                    for (int k = 0; k < this._config.batchSize; k++)
                    {
                        // gets profiling data
                        var perfMessage = profiler.DoProfiling();
                        string perfMessageJson = JsonConvert.SerializeObject(perfMessage);
                        //Log.Debug("perfMessage: \n{0} \nlength: {1}", perfMessageJson, perfMessageJson.Length);

                        // create a sample payload
                        int bytesToBeAdded = this._config.payloadLength;
                        bytesToBeAdded -= (perfMessageJson.Length + 14); //"iotEdgePerf":
                        bytesToBeAdded -= 15; //{"payload":"",}
                        if (bytesToBeAdded < 0) bytesToBeAdded=0;
                        var applicationPayload = new
                        {
                            //"payload":
                            payload = RandomString(bytesToBeAdded)
                        };
                        string applicationPayloadJson = JsonConvert.SerializeObject(applicationPayload);
                        //Log.Debug("applicationPayload: \n{0} \nlength: {1}", applicationPayloadJson, applicationPayloadJson.Length);

                        //
                        var mergedMessageString = Profiler.AddProfilingDataAndSerialize(applicationPayloadJson, perfMessageJson);
                        //Log.Debug("mergedMessage: \n{0} \nlength: {1}", mergedMessageString, mergedMessageString.Length);

                        // creates the message for the SendEventAsync / SendEventBatchAsync
                        var message = new Message(Encoding.ASCII.GetBytes(mergedMessageString));
                        messageBatch.Add(message);
                    }

                    // sends the message
                    profiler.MessageTransmissionStart();
                    if (messageBatch.Count == 1)
                        await _moduleClient.SendEventAsync(this._moduleOutput, messageBatch[0]);
                    else
                        await _moduleClient.SendEventBatchAsync(this._moduleOutput, messageBatch);
                    profiler.MessageTransmissionCompleted();

                    // waitsto achieve desired target rate
                    profiler.WaitToAchieveDesiredRate(cyclePeriodMilliseconds);
                }

                // burst completed
                if (this._config.logBurst)
                {
                    profiler.ShowSessionSummary();
                }

                await Task.Delay(this._config.burstWait); //wait even if last burst
            }

            // stops further transmissions.
            // A new transmission must be requested via twin or DM
            this._config.enable = false;
            return; //completed
        }

    }
}