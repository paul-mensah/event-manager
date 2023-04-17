namespace EventManager.Core.Options;

public class EmailConfiguration
{
    public string From { get; set; }
    public string Password { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
}