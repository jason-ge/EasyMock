using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EasyMockLib.Models;
using Newtonsoft.Json.Linq;

namespace EasyMockLib
{
    public class DevLogParser
    {
        private const string DevSoapRequestPattern = "^Request:\\s*(http\\S+)$";
        private const string DevSoapResponsePattern = "^Response:\\s*(http\\S+)$";

        private const string DevRestRequestPattern1  =  "^.*DevListener\\s+(\\w+)\\s+(http\\S+)\\s+Request$";
        private const string DevRestResponsePattern1 = "^.*DevListener\\s+(\\w+)\\s+(http\\S+)\\s+Response$";

        private const string DevRestRequestPattern2  = "^.*MessageHandler\\s+(\\w+)\\s+(http\\S+)\\s+Request$";
        private const string DevRestResponsePattern2 = "^.*MessageHandler\\s+(\\w+)\\s+(http\\S+)\\s+Response$";

        private const string SoapEnvSingleLineStartPattern = "^<[a-zA-Z-]+:Envelope\\s+[^>]+>.*</[a-zA-Z-]+:Envelope>$";
        private const string SoapEnvStartPattern = "^<[a-zA-Z-]+:Envelope\\s+[^>]+>";
        private const string SoapEnvEndPattern = "</[a-zA-Z-]+:Envelope>$";
        private const string RestRequestBodyPattern = "^RequestBody:\\s+(.*)$";
        private const string RestResponseBodyPattern = "^ResponseBody:\\s+(.*)$";
        private readonly XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";

        private int lineNumber = 0;

        public MockFileNode Parse(string filePath)
        {
            MockFileNode root = new MockFileNode()
            {
                MockFile = filePath,
            };
            lineNumber = 0;
            List<MockNode> pendingNodes = new List<MockNode>();
            Console.WriteLine($"Processing file {filePath}");

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = ReadLine(reader)) != null)
                {
                    Match m = Regex.Match(line, DevSoapRequestPattern);
                    if (m.Success)
                    {
                        pendingNodes.Add(ProcessSoapRequest(reader, m, line));
                        continue;
                    }
                    m = Regex.Match(line, DevSoapResponsePattern);
                    if (m.Success && pendingNodes.Count > 0)
                    {
                        ProcessSoapResponse(reader, m, pendingNodes, line);
                        continue;
                    }
                    m = Regex.Match(line, DevRestRequestPattern1);
                    if (m.Success)
                    {
                        // Found a REST request
                        pendingNodes.Add(ProcessRestRequest(reader, m));
                        continue;
                    }
                    m = Regex.Match(line, DevRestResponsePattern1);
                    if (m.Success)
                    {
                        // Found a REST response
                        ProcessRestResponse(reader, m, pendingNodes);
                        continue;
                    }
                    m = Regex.Match(line, DevRestRequestPattern2);
                    if (m.Success)
                    {
                        // Found a REST request
                        pendingNodes.Add(ProcessRestRequest(reader, m));
                        continue;
                    }
                    m = Regex.Match(line, DevRestResponsePattern2);
                    if (m.Success)
                    {
                        // Found a REST response
                        ProcessRestResponse(reader, m, pendingNodes);
                        continue;
                    }
                };
            }

            foreach (var node in pendingNodes)
            {
                node.Url = new Uri(node.Url).PathAndQuery; // Normalize URL to path
                if (node.Response == null)
                {
                    Console.WriteLine($"Pending request: {node.ServiceType} {node.Url} - {node.MethodName}");
                    continue;
                }
                root.Nodes.Add(node);
            }
            return root;
        }

        private MockNode ProcessSoapRequest(StreamReader reader, Match m, string line)
        {
            Request req = new Request()
            {
                RequestBody = ReadSoapBlock(reader, line),
            };
            MockNode node = new MockNode()
            {
                ServiceType = ServiceType.SOAP,
                MethodName = GetSoapMethodName(req.RequestBody.Content),
                Request = req,
                Url = m.Groups[1].Value,
                Description = ""
            };
            return node;
        }

        private string GetSoapMethodName(string soapEnv)
        {
            XDocument doc = XDocument.Parse(soapEnv);
            var header = doc.Descendants(soap + "Header").First();
            var action = header.Descendants().FirstOrDefault(n => n.Name.LocalName == "serviceFunc");
            if (action != null)
            {
                return action.Value;
            }
            action = header.Descendants().FirstOrDefault(n => n.Name.LocalName == "APIFunction");
            if (action != null)
            {
                return action.Value;
            }
            else
            {
                return Regex.Replace(doc.Descendants(soap + "Body").First().Elements().First().Name.LocalName, "Request$", "");
            }
        }

        private void ProcessSoapResponse(StreamReader reader, Match m, List<MockNode> pendingNodes, string line)
        {
            var node = pendingNodes.LastOrDefault(r => 
                r.Response == null &&
                r.Url.Equals(m.Groups[1].Value, StringComparison.OrdinalIgnoreCase) && 
                r.ServiceType == ServiceType.SOAP);
            if (node == null)
            {
                Console.WriteLine($"No pending request found for response {line}");
                return;
            }
            var response = new Response() { StatusCode = HttpStatusCode.OK, ResponseBody = new Body() };
            line = ReadLine(reader);
            Match m1 = Regex.Match(line, "^StatusCode:\\s*(\\w+)$");
            if (m1.Success)
            {
                response.StatusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), m1.Groups[1].Value);
                node.Response = response;
                return;
            }
            response.ResponseBody = ReadSoapBlock(reader, line);
            node.Response = response;
        }

        private MockNode ProcessRestRequest(StreamReader reader, Match m)
        {
            var httpMethodName = m.Groups[1].Value;
            Request req = new Request();
            if (!httpMethodName.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                req.RequestBody = ReadRestRequestBlock(reader);
            }

            MockNode node = new MockNode()
            {
                ServiceType = ServiceType.REST,
                MethodName = httpMethodName,
                Request = req,
                Url = m.Groups[2].Value,
                Description = ""
            };
            return node;
        }

        private void ProcessRestResponse(StreamReader reader, Match m, List<MockNode> pendingNodes)
        {
            var node = pendingNodes.LastOrDefault(r =>
                r.Response == null &&
                r.Url.Equals(m.Groups[2].Value, StringComparison.OrdinalIgnoreCase) &&
                r.ServiceType == ServiceType.REST);
            if (node == null)
            {
                Console.WriteLine($"No pending request found for response {m.Groups[0]}");
                return;
            }
            var response = ReadRestResponseBlock(reader);
            node.Response = response;
        }

        private Body ReadRestRequestBlock(StreamReader reader)
        {
            var body = new Body();
            string line;
            while ((line = ReadLine(reader)) != null && !Regex.IsMatch(line, RestRequestBodyPattern))
            {
            }
            if (line == null)
            {
                throw new Exception($"Cannot find REST RequestBody pattern {RestRequestBodyPattern}");
            }
            body.ContentObject = ExtractRestBody(reader, ref line, RestRequestBodyPattern);
            body.Content = body.ContentObject.ToString();

            return body;
        }

        private Response ReadRestResponseBlock(StreamReader reader)
        {
            string line;
            var response = new Response()
            {
                StatusCode = HttpStatusCode.Unused,
            };
            while ((line = ReadLine(reader)) != null)
            {
                Match m;
                if (response.StatusCode == HttpStatusCode.Unused)
                {
                    m = Regex.Match(line, "^StatusCode:\\s*(\\w+)$");
                    if (m.Success)
                    {
                        response.StatusCode = (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), m.Groups[1].Value);
                        continue;
                    }
                }
                m = Regex.Match(line, RestResponseBodyPattern);
                if (m.Success)
                {
                    response.ResponseBody = new Body
                    {
                        ContentObject = ExtractRestBody(reader, ref line, RestResponseBodyPattern),
                    };
                    response.ResponseBody.Content = response.ResponseBody.ContentObject.ToString();

                    if (response.StatusCode == HttpStatusCode.Unused)
                    {
                        response.StatusCode = HttpStatusCode.OK;
                    }
                }
                if ((response.StatusCode != HttpStatusCode.Unused || response.ResponseBody != null) &&
                    (line == null || Regex.IsMatch(line, "^\\s*$")))
                {
                    return response;
                }
            }
            throw new Exception($"Cannot find extract pattern {RestResponseBodyPattern}");
        }

        private object ExtractRestBody(StreamReader reader, ref string line, string pattern)
        {
            Match m = Regex.Match(line, pattern);
            if (m.Success)
            {
                if (m.Groups[1].Value.StartsWith("{") && m.Groups[1].Value.EndsWith("}"))
                {
                    // Single line JSON
                    var jobject = JObject.Parse(m.Groups[1].Value);
                    return jobject;
                }
                else if (m.Groups[1].Value.StartsWith("[") && m.Groups[1].Value.EndsWith("]"))
                {
                    // Single line JSON
                    var jarray = JArray.Parse(m.Groups[1].Value);
                    return jarray;
                }
                else if (m.Groups[1].Value.StartsWith("\"") && m.Groups[1].Value.EndsWith("\""))
                {
                    // Single line of string
                    return m.Groups[1].Value.Trim('"');
                }
                else
                {
                    StringBuilder sbJson = new StringBuilder(m.Groups[1].Value);
                    while ((line = ReadLine(reader)) != null)
                    {
                        if (Regex.IsMatch(line, "^\\s*$"))
                        {
                            if (sbJson[0] == '[')
                            {
                                JArray jarray = JArray.Parse(sbJson.ToString());
                                return jarray;
                            }
                            else if (sbJson[0] == '{')
                            {
                                JObject jobject = JObject.Parse(sbJson.ToString());
                                return jobject;
                            }
                            else
                            {
                                sbJson.Clear();
                                return sbJson.ToString();
                            }
                        }
                        sbJson.AppendLine(line);
                    }
                    var json = sbJson.ToString().Trim();
                    if (json.Length > 0)
                    {
                        if (json[0] == '[')
                        {
                            JArray jarray = JArray.Parse(json);
                            return json;
                        }
                        else if (json[0] == '{')
                        {
                            JObject jobject = JObject.Parse(json);
                            return jobject;
                        }
                        else
                        {
                            throw new Exception($"Invalid Json Object");
                        }
                    }
                    else
                    {
                        return json;
                    }
                }
            }
            else
            {
                throw new Exception($"Cannot match extract pattern {pattern}");
            }
        }

        private Body ReadSoapBlock(StreamReader reader, string line)
        {
            var searchStartLineNumber = lineNumber;
            var body = new Body();
            while (line != null)
            {
                if (Regex.IsMatch(line, SoapEnvSingleLineStartPattern))
                {
                    body.ContentObject = XElement.Parse(line); //xml0.Descendants(soap + "Body").First().Elements().First();
                    return body;
                }
                else if (Regex.IsMatch(line, SoapEnvStartPattern))
                {
                    break;
                }
                else
                {
                    line = ReadLine(reader);
                }
            }
            if (line == null)
            {
                throw new Exception($"Cannot find startPattern {SoapEnvStartPattern} since line {searchStartLineNumber}");
            }
            StringBuilder blocks = new StringBuilder(line);
            blocks.AppendLine();
            while (line != null && !Regex.IsMatch(line, SoapEnvEndPattern))
            {
                line = ReadLine(reader);
                blocks.AppendLine(line);
            }
            if (line == null)
            {
                throw new Exception($"Cannot find endPattern {SoapEnvEndPattern} since line {searchStartLineNumber}");
            }
            body.ContentObject = XElement.Parse(blocks.ToString());
            body.Content = body.ContentObject.ToString();
            return body;
        }

        private string ReadLine(StreamReader reader)
        {
            string line = reader.ReadLine();
            lineNumber++;
            return line;
        }
    }
}
