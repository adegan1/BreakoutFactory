public interface IMachineStoredResourceReceiver
{
    string MachineStateId { get; }
    void SetMachineStateId(string machineStateId);
    void SetStoredResourceAmount(int resourceAmount);
}
