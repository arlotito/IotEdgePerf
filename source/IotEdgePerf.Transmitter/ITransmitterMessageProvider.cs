namespace IotEdgePerf.Transmitter
{
    public interface ITransmitterMessageProvider
    {
        object GetMessage(int length);
    }

}