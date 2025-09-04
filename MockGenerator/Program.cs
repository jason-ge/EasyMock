using EasyMockLib.Models;
using MockGenerator;
using System.Configuration;
using System.Text;

var baseUrl = "http://localhost:" + ConfigurationManager.AppSettings["BindingPort"];
var loader = new MockFileLoader();
List<MockFileNode> nodes = new List<MockFileNode>();
foreach(var file in Directory.GetFiles(ConfigurationManager.AppSettings["MockFileFolder"]))
{
    nodes.Add(loader.LoadMockFile(file));
}

var client = new HttpClient();
foreach (var fileNode in nodes)
{
    foreach(var mockNode in fileNode.Nodes)
    {
        HttpRequestMessage request = new HttpRequestMessage();
        if (mockNode.ServiceType == ServiceType.REST)
        {
            request.Method = HttpMethod.Parse(mockNode.MethodName);
            if (request.Method != HttpMethod.Get)
            {
                request.Content = new StringContent(mockNode.Request.RequestBody.Content, Encoding.UTF8, "application/json");
            }
        }
        else
        {
            request.Method = HttpMethod.Post;
            request.Content = new StringContent(mockNode.Request.RequestBody.Content, Encoding.UTF8, "text/xml");
        }
        request.RequestUri = new Uri(baseUrl + mockNode.Url);
        Console.WriteLine($"Send request to {request.RequestUri.AbsoluteUri}");
        if (request.Content != null)
        {
            Console.WriteLine($"RequestBody: {request.Content.ReadAsStringAsync().Result}");
        }
        var response = await client.SendAsync(request);
        Console.WriteLine($"\nResponse: StatusCode - {response.StatusCode}");
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}