using EasyMockLib.Models;
using System.Net;

namespace EasyMock.UI;

public class RequestResponsePair
{
    public string Title
    {
        get
        {
            return $"{Url} - {Method}: {StatusCode} {ResponseTimeInMs}ms";
        }
    }

    public long ResponseTimeInMs { get; set; }

    public bool IsErrorStatusCode
    {
        get
        {
            return (int)StatusCode < 200 || (int)StatusCode >= 300;
        }
    }
    public string Url { get; set; }
    public string Method { get; set; }
    public ServiceType ServiceType { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public MockTreeNode? MockNodeSource { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}