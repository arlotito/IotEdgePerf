
namespace IotEdgePerf.Profiler
{
    using System;
    using System.Threading;
    using System.Diagnostics;
    using Newtonsoft.Json.Linq;
    using IotEdgePerf.Shared;
    using Serilog;

    public class Profiler
    {
        // 
        Stopwatch _sessionStopwatch; //measures the transmission duration
        Stopwatch _messageCycleStopwatch; //measures the transmission duration
        Stopwatch _transmissionStopwatch; // measures time during burst

        string _sessionId;
        int     _messageSequenceNumberInSession;
        double  _sessionElapsedMilliseconds;
        double?  _previousTransmissionDurationMilliseconds;
        double?  _previousMessageCycleDurationMilliseconds;
        double _messageTimestampMilliseconds;
        double _rollingRate;
        int     _timingViolationsCounter;

        public Profiler()
        {
            _sessionStopwatch = new Stopwatch();
            _messageCycleStopwatch = new Stopwatch();
            _transmissionStopwatch = new Stopwatch();
        }

        public void SessionStart(Guid sessionId)
        {
            this._sessionId = sessionId.ToString();
            this._messageSequenceNumberInSession = 0;
            this._timingViolationsCounter = 0;
            Log.Debug("session started with id={0}", sessionId.ToString());
        }

        public void MessageCycleStart()
        {
            if (_messageSequenceNumberInSession == 0)
            {
                // this is the 1st message of the session
                this._sessionStopwatch.Restart();
                this._previousMessageCycleDurationMilliseconds = null;
                this._sessionElapsedMilliseconds = GetSessionElapsedMilliseconds();
            }
            else
            {
                this._previousMessageCycleDurationMilliseconds = this._messageCycleStopwatch.Elapsed.TotalMilliseconds;
                this._sessionElapsedMilliseconds = GetSessionElapsedMilliseconds();
            }

            this._messageCycleStopwatch.Restart();
            this._messageSequenceNumberInSession++;

            this._rollingRate = ((this._messageSequenceNumberInSession-1) / this._sessionElapsedMilliseconds) * 1000;

            this._messageTimestampMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            Log.Debug("profiler: cycle start. seq: {0}, ts: {1}, prev cycle duration: {2}, session elapsed: {3}, rolling rate: {4}", 
                    this._messageSequenceNumberInSession,
                    this._messageTimestampMilliseconds,
                    this._previousMessageCycleDurationMilliseconds,
                    this._sessionElapsedMilliseconds,
                    this._rollingRate
            );
        }
        public void MessageTransmissionStart()
        {
            this._transmissionStopwatch.Restart();
            Log.Debug("profiler: transmission started.");
        }
        public void MessageTransmissionCompleted()
        {
            this._transmissionStopwatch.Stop();
            this._previousTransmissionDurationMilliseconds = this._transmissionStopwatch.Elapsed.TotalMilliseconds;
            Log.Debug("profiler: transmission completed. transmission durarion: {0}", this._previousTransmissionDurationMilliseconds);
        }

        public double? GetPreviousMessageTransmissionDuration()
        {
            return this._previousTransmissionDurationMilliseconds;
        }

        public double? GetPreviousMessageCycleDuration()
        {
            return this._previousMessageCycleDurationMilliseconds;
        }

        public double GetCurrentMessageCycleElapsed()
        {
            return this._messageCycleStopwatch.Elapsed.TotalMilliseconds;
        }

        public double GetCurrentSessionElapsed()
        {
            return this._sessionStopwatch.Elapsed.TotalMilliseconds;
        }

        public double GetSessionElapsedMilliseconds()
        {
            return this._sessionStopwatch.Elapsed.TotalMilliseconds;
        }

        public void WaitToAchieveDesiredRate(double cyclePeriodTimeMilliseconds)
        {
            double waitUntil = cyclePeriodTimeMilliseconds * this._messageSequenceNumberInSession;
            
            double now = this.GetCurrentSessionElapsed();
            if (now > waitUntil)
            {
                // too late, nothing to wait for
                this._timingViolationsCounter++;
                Log.Debug("profiler: timing violation. violations count: {0}, actual time: {1}, expected time: {2}", 
                    this._timingViolationsCounter, now, waitUntil);
                return;
            }
            else
            {
                while (this.GetCurrentSessionElapsed() < waitUntil)
                { }
            }
        }

        public void ShowSessionSummary()
        {
            Log.Error("ShowSessionSummary: not implemented yet.");
        }

        public PerfTelemetryMessage DoProfiling()
        {
            var perfMessage = new PerfTelemetryMessage
            {
                sessionId = this._sessionId,
                sessionTimeElapsedMilliseconds = this._sessionElapsedMilliseconds,

                messageSequenceNumberInSession = this._messageSequenceNumberInSession,

                sessionRollingRate = this._rollingRate,

                messageTimestampMilliseconds    = this._messageTimestampMilliseconds,
                previousTransmissionDurationMilliseconds = GetPreviousMessageTransmissionDuration(),
                previousMessageCycleDurationMilliseconds = GetPreviousMessageCycleDuration(),

                timingViolationsCounter = this._timingViolationsCounter
            };

            return perfMessage;
        }

        static public string AddProfilingDataAndSerialize(object message, PerfTelemetryMessage perfMessage)
        {
            var jObject = JObject.FromObject(message);
            jObject.Add(new JProperty("iotEdgePerf", JToken.FromObject(perfMessage)));
                        
            return jObject.ToString();
        }
    }
}
