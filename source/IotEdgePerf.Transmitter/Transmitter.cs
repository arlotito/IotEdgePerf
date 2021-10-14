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
        public event SendMessageEvent SendMessageHandler; // event
        public event SendMessageBatchEvent SendMessageBatchHandler;

        TransmitterConfigData _config;

        Guid _sessionId;

        readonly AtomicBoolean _resetRequest = new AtomicBoolean(false);

        protected virtual void OnSendMessage(string message) //protected virtual method
        {
            //if ProcessCompleted is not null then call delegate
            SendMessageHandler?.Invoke(message); 
        }

        protected virtual void OnSendMessageBatch(List<string> messageBatch, string output) //protected virtual method
        {
            //if ProcessCompleted is not null then call delegate
            SendMessageBatchHandler?.Invoke(messageBatch); 
        }

        public TransmitterLogic()
        {
            this._resetRequest.Set(false);
        }

        public void Start(Guid runId)
        {
            this._sessionId = runId;
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

        public void Send()
        {
            double cyclePeriodMilliseconds;

            if (!this._config.enable) 
                return; // do nothing

            // clears the resetRequest flag if raised
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
            Log.Information($"Run ID: {_sessionId}");

            // calculates time period
            if (this._config.targetRate > 0)
                cyclePeriodMilliseconds = 1 / (double)this._config.targetRate * 1000;
            else
                cyclePeriodMilliseconds = 0;
            Log.Information($"(calculated) cycle period [ms]: {cyclePeriodMilliseconds}");

            var profiler = new Profiler(_sessionId);

            for (int burstIndex = 0; burstIndex < this._config.burstNumber; burstIndex++)
            {
                profiler.BurstStart();

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
                        PerfTelemetryMessage perfMessage = profiler.GetProfilingTelemetry();
                        Log.Debug("perfMessage: \n{0}", perfMessage.Json);

                        // create an application payload. The resulting length must be equal to 'this._config.payloadLength'
                        int minimumLength = (perfMessage.Json.Length + perfMessage.KeyName.Length + 3) + 15; //{"p":<perfMessage>,"payload":"<randomString>"}
                        int delta = (this._config.payloadLength > minimumLength) ? (this._config.payloadLength - minimumLength) : 0;
                        var applicationObject = new
                        {
                            payload = RandomString(delta)
                        };
                        
                        string mergedMessageJson = perfMessage.AddTo(applicationObject);
                        Log.Debug("mergedMessage length: {0}", mergedMessageJson.Length);

                        // creates the message for the SendEventAsync / SendEventBatchAsync
                        messageBatch.Add(mergedMessageJson);
                    }

                    // sends the message
                    profiler.MessageTransmissionStart();
                    if (messageBatch.Count == 1)
                        this.SendMessageHandler(messageBatch[0]);
                    else
                        this.SendMessageBatchHandler(messageBatch);
                    profiler.MessageTransmissionCompleted();

                    // waits to achieve desired target rate
                    profiler.WaitToAchieveDesiredRate(cyclePeriodMilliseconds);
                }

                // burst completed
                if (this._config.logBurst)
                {
                    profiler.ShowSessionSummary();
                }

                Task.Delay(this._config.burstWait); //wait even if last burst
            }

            // stops further transmissions.
            // A new transmission must be requested via twin or DM
            this._config.enable = false;
            return; //completed
        }

    }
}