namespace UriNotifyPlugin;

public class NotifyGroup
{
    public int Index { get; set; }
    public string Mode { get; set; } = "text";
    public double MaskDuration { get; set; } = 5.0;   // 默认 5
    public string? MaskMsg { get; set; }
    public string? Msg { get; set; }
    public double? Duration { get; set; }
    public double? ScrollSpeed { get; set; }
    public int? Time { get; set; }
    public bool Speech { get; set; } = false;         // 默认 false
}