using EasyMockLib.Models;
using System.Windows.Controls;

namespace EasyMock.UI;

public class RequestResponsePair
{
    public string RequestSummary
    {
        get
        {
            return $"{Url} -  {Method}";
        }
    }
    public string ResponseSummary
    {
        get
        {
            return $"Response: StatusCode - {StatusCode}";
        }
    }

    public bool IsErrorStatusCode
    {
        get
        {
            return StatusCode < 200 || StatusCode >= 300;
        }
    }
    public string Url { get; set; }
    public string Method { get; set; }
    public ServiceType ServiceType { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public MockTreeNode? MockFileSource { get; set; }
    public MockTreeNode? MockNodeSource { get; set; }
    public int StatusCode { get; set; }
}