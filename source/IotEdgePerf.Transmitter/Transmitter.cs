namespace IotEdgePerf.Transmitter
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Text;
    
    using System.Collections.Generic;
    using Newtonsoft.Json;

    using Serilog;

    using IotEdgePerf.Shared;
    using IotEdgePerf.Profiler;

    public delegate void SendMessageEvent(string message);
    public delegate void SendMessageBatchEvent(List<string> messageBatch);

    public partial class TransmitterLogic
    {
        public event SendMessageEvent SendMessage; // event
        public event SendMessageBatchEvent SendMessageBatch;

        TransmitterConfigData _config;

        Guid _runId;

        readonly AtomicBoolean _resetRequest = new AtomicBoolean(false);

        protected virtual void OnSendMessage(string message) //protected virtual method
        {
            //if ProcessCompleted is not null then call delegate
            SendMessage?.Invoke(message); 
        }

        protected virtual void OnSendMessageBatch(List<string> messageBatch, string output) //protected virtual method
        {
            //if ProcessCompleted is not null then call delegate
            SendMessageBatch?.Invoke(messageBatch); 
        }

        public TransmitterLogic()
        {
            this._resetRequest.Set(false);
        }

        public void Start(Guid runId)
        {
            this._runId = runId;
            this._resetRequest.Set(true);
            this._config.enable=true; //enables the transmitter
        }

        public void ApplyConfiguration(TransmitterConfigData config)
        {
            this._config = config;
            Log.Information("New configuration applied:\n{0}", JsonConvert.SerializeObject(this._config, Formatting.Indented));
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

        public async Task LoopAsync()
        {
            var profiler = new Profiler();
            double cyclePeriodMilliseconds;

            if (!this._config.enable) 
                return; // do nothing

            if (this._resetRequest)
            {
                Log.Information("reset executed.");
                this._resetRequest.Set(false);
            }

            // wait before starting
            Log.Information($"waiting {_config.waitBeforeStart} ms before starting...");
            Thread.Sleep(this._config.waitBeforeStart);
            Log.Information($"transmission started.");

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

                    var messageBatch = new List<string>();

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
                        messageBatch.Add(mergedMessageString);
                    }

                    // sends the message
                    profiler.MessageTransmissionStart();
                    if (messageBatch.Count == 1)
                        this.SendMessage(messageBatch[0]);
                    else
                        this.SendMessageBatch(messageBatch);
                    profiler.MessageTransmissionCompleted();

                    // waits to achieve desired target rate
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