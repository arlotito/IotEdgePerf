using System;
using System.Threading;
using IotEdgePerf.Profiler.Test;

namespace ProfilerTest
{
    class TestObject
    {
        public string name;
        public string description;
        public string type;
    }
    class Program
    {
        static void Main(string[] args)
        {
            Test();
        }

        static void Test()
        {
            Console.WriteLine("Test.");

            var profiler = new Profiler();
            var testObject = new TestObject { description = "hi", name = "there", type = "!" };

            int targetRate = 1000;
            double periodMilliseconds = (1 / (double)targetRate) * 1000;
            Console.WriteLine($"Target rate:    {targetRate} msg/s");
            Console.WriteLine($"Cycle period:   {periodMilliseconds} ms");
            Console.WriteLine($"");

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
                    profiler.WaitToAchieveDesiredRate(periodMilliseconds);
                }

                Console.WriteLine("\n\n--------------------------------------");

                //do something else here....
                Thread.Sleep(1000);
            }
        }
    }

    
}
