namespace IotEdgePerf.Shared
{
    using System.Threading;

    public class AtomicBoolean
    {
        int underlying;

        public AtomicBoolean(bool value)
        {
            this.underlying = value ? 1 : 0;
        }

        public AtomicBoolean()
            : this(false)
        {
        }

        public static implicit operator bool(AtomicBoolean value) => value.Get();

        public bool Get() => Volatile.Read(ref this.underlying) != 0;

        public void Set(bool value) => Volatile.Write(ref this.underlying, value ? 1 : 0);

        public bool GetAndSet(bool value) => Interlocked.Exchange(ref this.underlying, value ? 1 : 0) != 0;

        public bool CompareAndSet(bool expected, bool result)
        {
            int e = expected ? 1 : 0;
            int r = result ? 1 : 0;
            return Interlocked.CompareExchange(ref this.underlying, r, e) == e;
        }
    }
}