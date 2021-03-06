
namespace IotEdgePerf.Profiler
{
    using System;
    using System.Diagnostics;
    using Serilog;

    public class Profiler
    {
        // 
        Stopwatch _sessionStopwatch; //measures the transmission duration
        Stopwatch _messageCycleStopwatch; //measures the transmission duration
        Stopwatch _transmissionStopwatch; // measures time during burst

        string  _sessionId;
        int     _burstSequenceNumberInSession;
        int     _messageSequenceNumberInSession;
        double  _sessionElapsedMilliseconds;
        double? _previousTransmissionDurationMilliseconds;
        double? _previousMessageCycleDurationMilliseconds;
        double  _messageTimestampMilliseconds;
        double? _rollingRate;
        int     _timingViolationsCounter;

        public Profiler(Guid sessionId)
        {
            this._sessionId = sessionId.ToString();
            _sessionStopwatch = new Stopwatch();
            _messageCycleStopwatch = new Stopwatch();
            _transmissionStopwatch = new Stopwatch();
            this._burstSequenceNumberInSession = 0;
            Log.Debug("profiler: session started with session id={0}", sessionId.ToString());
        }

        public void BurstStart()
        {
            this._messageSequenceNumberInSession = 0;
            this._timingViolationsCounter = 0;
            this._burstSequenceNumberInSession++;
            Log.Debug("profiler: burst #{0} started.", this._burstSequenceNumberInSession);
        }

        public void MessageCycleStart()
        {
            this._messageSequenceNumberInSession++;
            this._messageTimestampMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            if (_messageSequenceNumberInSession == 1)
            {
                // this is the 1st message of the session / burst
                this._sessionStopwatch.Restart();
                this._sessionElapsedMilliseconds = 0;
                this._previousMessageCycleDurationMilliseconds = null;
                this._rollingRate = null;
            }
            else
            {
                this._previousMessageCycleDurationMilliseconds = this._messageCycleStopwatch.Elapsed.TotalMilliseconds;
                this._sessionElapsedMilliseconds = GetSessionElapsedMilliseconds();
                this._rollingRate = ((this._messageSequenceNumberInSession-1) / this._sessionElapsedMilliseconds) * 1000;
            }

            this._messageCycleStopwatch.Restart();
            
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

        public void WaitToAchieveDesiredTransmissionInterval(double cyclePeriodTimeMilliseconds)
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
            Log.Information("profiler: session completed. seq: {0}, session elapsed: {1}, rolling rate: {2}", 
                    this._messageSequenceNumberInSession,
                    this._sessionElapsedMilliseconds,
                    this._rollingRate
            );
        }

        public PerfTelemetryMessage GetProfilingTelemetry()
        {
            var perfMessage = new PerfTelemetryMessage 
            {
                id = this._sessionId,                       
                bc = this._burstSequenceNumberInSession,    
                el = this._sessionElapsedMilliseconds,
                mc = this._messageSequenceNumberInSession,
                rt = this._rollingRate,
                ts = this._messageTimestampMilliseconds,
                pt = this._previousTransmissionDurationMilliseconds,
                pc = this._previousMessageCycleDurationMilliseconds,
                vc = this._timingViolationsCounter
            };

            return perfMessage;
        }
    }
}
