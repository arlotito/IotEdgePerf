
namespace source
{
    using System;
    using System.Threading.Tasks;

    public interface ISenderMachine
    {
        public void Restart(Guid runId);
        public void Start(Guid runId, SenderMachineConfigData config);

        public Task RegisterDM();
        
        public Task SendMessagesAsync();
    }
}
