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
    using IotEdgePerf.Transmitter.ConfigData;

    public delegate void SendMessageEvent(string message);
    public delegate void SendMessageBatchEvent(List<string> messageBatch);
    public delegate object CreateMessage(int length);

    public partial class TransmitterLogic
    {
        public event SendMessageEvent SendMessageHandler; 
        public event SendMessageBatchEvent SendMessageBatchHandler;
        public event CreateMessage CreateMessageHandler;

        TransmitterConfigData _config;

        Guid _sessionId;

        readonly AtomicBoolean _resetRequest = new AtomicBoolean(false);

        public TransmitterLogic()
        {
            this._resetRequest.Set(false);
            _config = new TransmitterConfigData();
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

        public void Send()
        {
            double cyclePeriodMilliseconds;

            if (!this._config.enable)
            {
                // transmitter is disabled. Nothing to do.
                return; 
            }

            // clear the resetRequest flag
            // (the resetRequest flag is used to stop a transmission if in progress)
            if (this._resetRequest)
            {
                Log.Information("reset request received. nothing to do.");
                this._resetRequest.Set(false);
            }

            // wait before starting
            Log.Information($"waiting {_config.waitBeforeStart} ms before starting...");
            Thread.Sleep(this._config.waitBeforeStart);
            Log.Information($"transmission started.");

            // calculates the cycle time period to achieve desired target rate
            if (this._config.targetRate > 0)
                cyclePeriodMilliseconds = 1 / (double)this._config.targetRate * 1000;
            else
                cyclePeriodMilliseconds = 0;
            Log.Information($"(calculated) cycle period [ms]: {cyclePeriodMilliseconds}");

            // let's start the transmission
            Log.Information($"Session ID: {_sessionId}");
            var profiler = new Profiler(_sessionId);

            // BURST loop (in a session we have 'this._config.burstNumber' bursts)
            for (int burstIndex = 0; burstIndex < this._config.burstNumber; burstIndex++)
            {
                profiler.BurstStart();

                Log.Debug("Burst index: {0}", burstIndex);

                // MESSAGE loop (in a burst we have 'this._config.burstLength' messages)
                for (int messageIndex = 0; messageIndex < this._config.burstLength; messageIndex++)
                {
                    profiler.MessageCycleStart();

                    Log.Debug("Message index: {0}", messageIndex);

                    // stops in case a reset was requested
                    if (_resetRequest)
                    {
                        Log.Information("reset request received. stops transmission.");
                        this._resetRequest.Set(false); //clears the request
                        return; 
                    }

                    
                    // create single message or batch of messages
                    var messageBatch = new List<string>();
                    for (int k = 0; k < this._config.batchSize; k++)
                    {
                        // gets profiling data
                        PerfTelemetryMessage perfMessage = profiler.GetProfilingTelemetry();
                        Log.Debug("perfMessage: \n{0}", perfMessage.Json);

                        // requests for an application payload by raising the 'CreateMessageHandler' event
                        //The resulting length must be equal to 'this._config.payloadLength'
                        //                                                     {"      - p -                    ": - <perfMessage> -          , - <object> }
                        int deltaPayloadLength = this._config.payloadLength - (2 + perfMessage.KeyName.Length + 2 + perfMessage.Json.Length + 1 + 1); 
                        object applicationObject = this.CreateMessageHandler?.Invoke(deltaPayloadLength > 0 ? deltaPayloadLength : 0);

                        // adds the profiling telemetry data to the application message
                        string mergedMessageJson = perfMessage.AddTo(applicationObject);
                        Log.Debug("mergedMessage length: {0}", mergedMessageJson.Length);

                        // creates the message for the SendEventAsync / SendEventBatchAsync
                        messageBatch.Add(mergedMessageJson);
                    }

                    // sends the message
                    profiler.MessageTransmissionStart();
                    if (messageBatch.Count == 1)
                        this.SendMessageHandler?.Invoke(messageBatch[0]);
                    else
                        this.SendMessageBatchHandler?.Invoke(messageBatch);
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