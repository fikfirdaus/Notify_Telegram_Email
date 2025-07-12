public class AlertRequestTelegram
{
    public string IDTelegram { get; set; }
    public string ServerName { get; set; }
    public string IPAddress { get; set; }
    public string status { get; set; }
    public DateTime statusTime { get; set; }
    public string message { get; set; }
}

public class AlertRequestEmail
{
    public string ServerName { get; set; }
    public string IPAddress { get; set; }
    public string status { get; set; }
    public DateTime statusTime { get; set; }
    public string message { get; set; }
    public string EmailTo { get; set; }
    public string EmailCC { get; set; }
}
