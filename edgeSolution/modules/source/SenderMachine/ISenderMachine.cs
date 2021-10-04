
namespace source
{
    using System;
    using System.Threading.Tasks;

    public interface ISenderMachine
    {
        public void Reset(SenderMachineConfigData config);
        
        public Task SendMessagesAsync();
    }
}
