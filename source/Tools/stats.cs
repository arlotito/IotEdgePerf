namespace IoTEdgePerf.Tools
{
    using System;
    
    public class StatsCalculator
    {
        public double min;
        public double max;
        public double average;
        
        public int[] hist;
        
        public double binSize;
        public int binNum;

        public int count;

        
        public StatsCalculator(int binNum, double binSize)
        {
            this.binSize=binSize;
            this.binNum=binNum;
            this.hist = new int[binNum];

            this.min = Double.MaxValue;
            this.max = Double.MinValue;
            this.average = 0;
            this.count = 0;
        }

         public void Update(double value)
        {
            // histogram
            int bin = (int)Math.Truncate(value / this.binSize);
            
            // Console.WriteLine($"bin: {bin}");
            if (bin < this.hist.Length)
            {
                this.hist[bin]++;
                // Console.WriteLine($"bin value: {this.hist[bin]}");
            }

            this.count++;

            if (value < this.min)
                this.min = value;
            
            if (value > this.max)
                this.max = value;

            this.average = (this.average * (this.count-1) + value) / this.count;
        }

        public void Print()
        {
            Console.Write($"HIST (bin size={this.binSize}): ");
            Console.Write("{");
            for (int i=0; i<this.hist.Length; i++)
            {
                Console.Write($"{this.hist[i]}");
                if (i<(this.hist.Length-1))
                    Console.Write(",");
            }
            Console.Write("}");     
            Console.WriteLine("");
        }
    }
}