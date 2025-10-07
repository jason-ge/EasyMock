using EasyMockLib.Models;
using System.Net;
using System.Text;

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
    public string? Headers { get; set; }
    public MockNode? MockNodeSource { get; set; }
    public MockTreeNode? MockTreeNodeSource { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}