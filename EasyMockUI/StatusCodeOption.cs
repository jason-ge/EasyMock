public class StatusCodeOption
{
    public int Code { get; set; }
    public string Name { get; set; }
    public string Display => $"{Code} - {Name}";
}