
namespace IoTEdgePerf.Profiler
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Newtonsoft.Json.Linq;

    public class PerfMessage2
    {
        public string   sessionId;
        public double   sessionTimeElapsedMilliseconds;

        public double   sessionRollingRate;

        public int      messageSequenceNumberInSession;

        public long     messageTimestampMilliseconds;
        public double   previousTransmissionDurationMilliseconds;
        public double   previousMessageCycleDurationMilliseconds;
    }

    public class Profiler
    {
        // 
        Stopwatch   _sessionStopwatch; //measures the transmission duration
        Stopwatch   _messageCycleStopwatch; //measures the transmission duration
        Stopwatch   _transmissionStopwatch; // measures time during burst
        
        string      _sessionId;
        int         _messageSequenceNumberInSession;
        double      _previousTransmissionDurationMilliseconds;
        double      _previousMessageCycleDurationMilliseconds;

        public Profiler()
        {
            _sessionStopwatch = new Stopwatch();
            _messageCycleStopwatch = new Stopwatch();
            _transmissionStopwatch = new Stopwatch();
        }
        
        public void SessionStart(Guid sessionId)
        {
            this._sessionId = sessionId.ToString();
            this._sessionStopwatch.Restart();

            this._messageSequenceNumberInSession = 0;
        }

        public void MessageCycleStart()
        {
            if (this._messageSequenceNumberInSession >= 1)
                this._previousMessageCycleDurationMilliseconds = this._messageCycleStopwatch.ElapsedMilliseconds;
            else
                this._previousMessageCycleDurationMilliseconds = 0;

            this._messageCycleStopwatch.Restart();
            this._messageSequenceNumberInSession++;
        }
        public void MessageTransmissionStart()
        {
            this._transmissionStopwatch.Restart();
        }
        public void MessageTransmissionCompleted()
        {
            this._transmissionStopwatch.Stop();
            this._previousTransmissionDurationMilliseconds = this._transmissionStopwatch.Elapsed.TotalMilliseconds;
        }

        public double GetPreviousMessageTransmissionDuration()
        {
            return this._previousTransmissionDurationMilliseconds;
        }

        public double GetPreviousMessageCycleDuration()
        {
            return this._previousMessageCycleDurationMilliseconds;
        }

        public double GetCurrentMessageCycleElapsed()
        {
            return this._messageCycleStopwatch.Elapsed.TotalMilliseconds;
        }

        public double GetSessionElapsedMilliseconds()
        {
            return this._sessionStopwatch.Elapsed.TotalMilliseconds;
        }

        public void MessageCycleWaitUntil(double period)
        {
            while (this.GetCurrentMessageCycleElapsed() < period)
            { }
        }

        public PerfMessage2 DoProfiling()
        {
            // elapsed since session start
            var sessionElapsed = GetSessionElapsedMilliseconds();

            // rolling rate calculated on the fly
            double rate;
            if (this._messageSequenceNumberInSession > 1)
                rate = this._messageSequenceNumberInSession / sessionElapsed;
            else
                rate = 0;

            var perfMessage = new PerfMessage2
            {
                sessionId = this._sessionId,
                sessionTimeElapsedMilliseconds = sessionElapsed,

                messageSequenceNumberInSession = this._messageSequenceNumberInSession,

                sessionRollingRate = rate,

                messageTimestampMilliseconds    = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                previousTransmissionDurationMilliseconds = GetPreviousMessageTransmissionDuration(),
                previousMessageCycleDurationMilliseconds = GetPreviousMessageCycleDuration()
            };

            return perfMessage;
        }

        static public string AddProfilingDataAndSerialize(object message, PerfMessage2 perfMessage)
        {
            var jObject = JObject.FromObject(message);
            jObject.Add(new JProperty("iotEdgePerf", JToken.FromObject(perfMessage)));
                        
            return jObject.ToString();
        }
    }

    static public class ProfilerTest
    {
        class TestObject
        {
            public string name;
            public string description;
            public string type;
        }

        static public void Test()
        {
            Console.WriteLine("Test.");

            var profiler = new Profiler();
            var testObject = new TestObject { description = "hi", name = "there", type = "!" };

            int targetRate = 4;
            double periodMilliseconds = (1 / (double)targetRate) * 1000;
            Console.WriteLine($"Target rate:    {targetRate} msg/s");
            Console.WriteLine($"Cycle period:   {periodMilliseconds} ms");

            for (int session = 0; session < 1; session++)
            {
                profiler.SessionStart(Guid.NewGuid());
                
                for (int message = 0; message < 4; message++)
                {
                    profiler.MessageCycleStart();

                    //do something here....
                    Thread.Sleep(20);

                    var perfMessage = profiler.DoProfiling();
                    var messageString = Profiler.AddProfilingDataAndSerialize(testObject, perfMessage);
                    Console.WriteLine(messageString);

                    profiler.MessageTransmissionStart();
                    Thread.Sleep(20);
                    profiler.MessageTransmissionCompleted();

                    //do something here....
                    Thread.Sleep(10);

                    //
                    profiler.MessageCycleWaitUntil(periodMilliseconds);
                }

                Console.WriteLine("\n\n--------------------------------------");

                //do something else here....
                Thread.Sleep(1000);
            }
        }
    }

}
