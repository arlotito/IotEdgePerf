namespace Toolbox
{
    using System;
        
    public class StatsCalculator
    {
        public double min;
        public double max;
        public double avg;
        
        public int elementsNum;

        public StatsCalculator()
        {
            this.min = Double.MaxValue;
            this.max = Double.MinValue;
            this.avg = 0;
            this.elementsNum = 0;
        }

        public void Append(double value)
        {
            this.elementsNum++;

            if (value < this.min)
                this.min = value;
            
            if (value > this.max)
                this.max = value;

            this.avg = (this.avg * (this.elementsNum-1) + value) / this.elementsNum;
        }

        public override string ToString()
        {
            return String.Format($"{this.avg:0.00},{this.min:0.00},{this.max:0.00}");
        }
    }
}