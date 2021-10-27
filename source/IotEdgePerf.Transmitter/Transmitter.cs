namespace IotEdgePerf.Transmitter
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    
    using System.Collections.Generic;
    using Newtonsoft.Json;

    using Serilog;

    using IotEdgePerf.Shared;
    using IotEdgePerf.Profiler;
        
    public partial class TransmitterLogic
    {
        TransmitterConfigData _config;

        Guid _sessionId;

        readonly AtomicBoolean _startRequest = new AtomicBoolean(false);
        readonly AtomicBoolean _stopRequest = new AtomicBoolean(false);
        private ITransmitterTransport _handlers; 
        private ITransmitterMessageProvider _messageProvider; 

        public TransmitterLogic(
            TransmitterConfigData       config,
            ITransmitterTransport       transportHandlers,
            ITransmitterMessageProvider messageProvider
            )
        {
            this._startRequest.Set(false);
            this.ApplyConfiguration(config);

            this._handlers=transportHandlers;
            this._messageProvider=messageProvider;
        }

        public void Restart(Guid runId)
        {
            Log.Information($"started with id={runId.ToString()}.");
            this._sessionId = runId;

            //stop the transmission (if any)
            Stop();

            //enables the transmitter
            this._startRequest.Set(true);
        }

        void Stop()
        {
            Log.Information("stop requested.");
            this._stopRequest.Set(true); //stops transmission
        }

        public void ApplyConfiguration(TransmitterConfigData config)
        {
            this._config = config;
            Log.Information("New configuration applied:\n{0}", JsonConvert.SerializeObject(this._config, Formatting.Indented));

            // if autostart, start transmission immediately, otherwise stop ongoing transmission if any
            if (this._config.autoStart == true)
                this.Restart(Guid.NewGuid());
            else
                this.Stop();
        }

        public async Task TransmitterLoopAsync()
        {
            double desiredTransmissionInterval;

            if (!this._startRequest)
                return; //no transmission requested yet. nothing to do
            else
                this._startRequest.Set(false);

            // clear any pending request
            if (this._stopRequest)
                this._stopRequest.Set(false);

            // transmitter is enabled. Let's start.

            // wait before starting
            Log.Information($"waiting {_config.waitBeforeStart} ms before starting...");
            Thread.Sleep(this._config.waitBeforeStart);
            
            Log.Information($"transmission started.");

            // calculates the cycle time period to achieve desired target rate
            if (this._config.targetRate > 0)
                desiredTransmissionInterval = 1 / (double)this._config.targetRate * 1000;
            else
                desiredTransmissionInterval = 0;
            Log.Information($"(calculated) cycle period [ms]: {desiredTransmissionInterval}");

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
                    if (this._stopRequest)
                    {
                        //stops transmission
                        Log.Information("stopped.");
                        return;
                    }
                    
                    profiler.MessageCycleStart();

                    Log.Debug("Message index: {0}", messageIndex);

                                        
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
                        object applicationObject = this._messageProvider.GetMessage(deltaPayloadLength > 0 ? deltaPayloadLength : 0);

                        // adds the profiling telemetry data to the application message
                        string mergedMessageJson = perfMessage.AddTo(applicationObject);
                        Log.Debug("mergedMessage length: {0}", mergedMessageJson.Length);

                        // creates the message for the SendEventAsync / SendEventBatchAsync
                        messageBatch.Add(mergedMessageJson);
                    }

                    // sends the message
                    profiler.MessageTransmissionStart();
                    if (messageBatch.Count == 1)
                        await this._handlers.SendMessageHandler(messageBatch[0]);
                    else
                        await this._handlers.SendMessageBatchHandler(messageBatch);
                    profiler.MessageTransmissionCompleted();

                    // waits to achieve desired target rate
                    profiler.WaitToAchieveDesiredTransmissionInterval(desiredTransmissionInterval);
                }

                // burst completed
                if (this._config.logBurst)
                {
                    profiler.ShowSessionSummary();
                }

                await Task.Delay(this._config.burstWait); //wait even if last burst
            }

            // transmission completed.
            return; 
        }

    }
}