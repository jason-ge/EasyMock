using EasyMockLib.Models;
using System.Collections.Specialized;
using System.Net;

namespace EasyMock.UI;

public class RequestResponsePair
{
    public string Title
    {
        get
        {
            return $"{Url} - {Method}: {StatusCode}";
        }
    }

    public string ResponseTitle
    {
        get
        {
            if (!string.IsNullOrEmpty(MockNodeSource?.Description))
            {
                return $"Response ({MockNodeSource.Description}) in {ResponseTimeInMs}ms:";
            }
            return $"Response in {ResponseTimeInMs}ms:";
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
    public string HeaderString
    {
        get
        {
            if (Headers == null || Headers.Count == 0)
            {
                return string.Empty;
            }
            return string.Join(Environment.NewLine, Headers.AllKeys.Select(k => $"{k}: {string.Join(",", Headers.GetValues(k) ?? [])}"));
        }
    }
    public string Url { get; set; }
    public string Method { get; set; }
    public ServiceType ServiceType { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public NameValueCollection Headers { get; set; }
    public MockNode? MockNodeSource { get; set; }
    public MockTreeNode? MockTreeNodeSource { get; set; }
    public HttpStatusCode StatusCode { get; set; }

    public bool CanReplayInQA { get; set; }
}