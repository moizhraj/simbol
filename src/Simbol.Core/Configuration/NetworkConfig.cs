namespace Simbol.Core.Configuration;

public class NetworkConfig
{
    public string Interface { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 47808;
    public string BroadcastAddress { get; set; } = "255.255.255.255";

    public void Validate()
    {
        if (Port < 1 || Port > 65535)
            throw new InvalidOperationException($"Network port must be between 1 and 65535, got {Port}.");
    }
}
