using EasyMockLib.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EasyMockLib
{
    public class MockFileParser
    {
        private const string DevSoapRequestPattern = "^\\d{2}:\\d{2}:\\d{2}\\.\\d+\\s+(http\\S+)\\s+(\\w+)\\s+Request$";
        private const string DevSoapResponsePattern = "^\\d{2}:\\d{2}:\\d{2}\\.\\d+\\s+(http\\S+)\\s+(\\w+)\\s+Response$";
        private const string DevRestRequestPattern = "^\\d{2}:\\d{2}:\\d{2}\\.\\d+\\s+(\\w+)\\s+(http\\S+)\\s+Request$";
        private const string DevRestResponsePattern = "^\\d{2}:\\d{2}:\\d{2}\\.\\d+\\s+(\\w+)\\s+(http\\S+)\\s+Response$";
        private const string SoapEnvSingleLineStartPattern = "^<[a-zA-Z-]+:Envelope\\s+[^>]+>.*</[a-zA-Z-]+:Envelope>$";
        private const string SoapEnvStartPattern = "^<[a-zA-Z-]+:Envelope\\s+[^>]+>";
        private const string SoapEnvEndPattern = "</[a-zA-Z-]+:Envelope>$";
        private const string RestRequestBodyPattern = "^RequestBody:\\s+(.*)$";
        private const string RestResponseBodyPattern = "^ResponseBody:\\s+(.*)$";
        private const string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>";
        private readonly XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        private int lineNumber = 0;
        public MockFileParser()
        {
        }
        public MockFileNode Parse(string filePath)
        {
            MockFileNode root = new MockFileNode()
            {
                MockFile = filePath,
            };
            lineNumber = 0;
            List<Request> pendingRequests = new List<Request>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = ReadLine(reader)) != null)
                {
                    Match m = Regex.Match(line, DevSoapRequestPattern);
                    if (m.Success)
                    {
                        pendingRequests.Add(ProcessSoapRequest(reader, m, line));
                        continue;
                    }
                    m = Regex.Match(line, DevSoapResponsePattern);
                    if (m.Success && pendingRequests.Count > 0)
                    {
                        root.Nodes.Add(ProcessSoapResponse(reader, m, pendingRequests, line));
                        continue;
                    }
                    m = Regex.Match(line, DevRestRequestPattern);
                    if (m.Success)
                    {
                        // Found a REST request
                        pendingRequests.Add(ProcessRestRequest(reader, m));
                        continue;
                    }
                    m = Regex.Match(line, DevRestResponsePattern);
                    if (m.Success)
                    {
                        // Found a REST response
                        root.Nodes.Add(ProcessRestResponse(reader, m, pendingRequests));
                        continue;
                    }
                };
            }
            foreach (var request in pendingRequests)
            {
                root.Nodes.Add(new MockNode() { Request = request, Response = null });
            }
            return root;
        }

        private Request ProcessSoapRequest(StreamReader reader, Match m, string line)
        {
            Request req = new Request()
            {
                RequestType = ServiceType.SOAP,
                Url = m.Groups[1].Value,
                ServiceName = m.Groups[1].Value.Substring(m.Groups[1].Value.LastIndexOf('/') + 1),
            };
            req.RequestBody = ReadSoapBlock(reader, line);
            req.MethodName = GetSoapAction(req.RequestBody.Content);
            return req;
        }
        private string GetSoapAction(string soapEnv)
        {
            XDocument doc = XDocument.Parse(soapEnv);
            return Regex.Replace(doc.Descendants(soap + "Body").First().Elements().First().Name.LocalName, "Request$", "");
        }
        private MockNode ProcessSoapResponse(StreamReader reader, Match m, List<Request> pendingRequests, string line)
        {
            var request = pendingRequests.LastOrDefault(r => r.Url == m.Groups[1].Value && r.RequestType == ServiceType.SOAP);
            var response = new Response() { StatusCode = HttpStatusCode.OK, ResponseBody = new Body() };
            response.ResponseBody = ReadSoapBlock(reader, line);
            response.ResponseBody.Content = xml + "\r\n" + response.ResponseBody.Content;
            pendingRequests.Remove(request);
            return new MockNode() { Request = request, Response = response };
        }
        private Request ProcessRestRequest(StreamReader reader, Match m)
        {
            Request req = new Request()
            {
                RequestType = ServiceType.REST,
                Url = m.Groups[2].Value,
                MethodName = m.Groups[1].Value,
                ServiceName = (new Uri(m.Groups[2].Value)).AbsolutePath,
            };
            if (!req.MethodName.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                req.RequestBody = ReadRestRequestBlock(reader);
                req.RequestBody.StartLineNumber = req.RequestBody.EndLineNumber = lineNumber;
            }
            return req;
        }
        private MockNode ProcessRestResponse(StreamReader reader, Match m, List<Request> pendingRequests)
        {
            var request = pendingRequests.LastOrDefault(r => r.Url == m.Groups[2].Value && r.RequestType == ServiceType.REST);
            var response = ReadRestResponseBlock(reader);
            pendingRequests.Remove(request);
            return new MockNode() { Request = request, Response = response };
        }
        private Body ReadRestRequestBlock(StreamReader reader)
        {
            var body = new Body()
            {
                StartLineNumber = lineNumber,
                EndLineNumber = 0
            };
            string line;
            while ((line = ReadLine(reader)) != null && !Regex.IsMatch(line, RestRequestBodyPattern))
            {
            }
            if (line == null)
            {
                throw new Exception($"Cannot find REST RequestBody pattern {RestRequestBodyPattern}");
            }
            body.EndLineNumber = lineNumber;
            body.Content = ExtractRestBody(reader, ref line, RestRequestBodyPattern);
            body.ContentObject = JObject.Parse(body.Content);
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
                    m = Regex.Match(line, "^StatusCode:(\\w+)$");
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
                        StartLineNumber = lineNumber,
                        Content = ExtractRestBody(reader, ref line, RestResponseBodyPattern),
                    };
                    if (response.StatusCode == HttpStatusCode.Unused)
                    {
                        response.StatusCode = HttpStatusCode.OK;
                    }
                    response.ResponseBody.EndLineNumber = lineNumber;
                }
                if ((response.StatusCode != HttpStatusCode.Unused || response.ResponseBody != null) &&
                (line == null || Regex.IsMatch(line, "^\\s*$")))
                {
                    return response;
                }
            }
            throw new Exception($"Cannot find extract pattern {RestResponseBodyPattern}");
        }
        private string ExtractRestBody(StreamReader reader, ref string line, string pattern)
        {
            Match m = Regex.Match(line, pattern);
            if (m.Success)
            {
                if (m.Groups[1].Value.StartsWith("{") && m.Groups[1].Value.EndsWith("}"))
                {
                    // Single line JSON
                    var jobject = JObject.Parse(m.Groups[1].Value);
                    return jobject.ToString();
                }
                else if (m.Groups[1].Value.StartsWith("[") && m.Groups[1].Value.EndsWith("]"))
                {
                    // Single line JSON
                    var jarray = JArray.Parse(m.Groups[1].Value);
                    return jarray.ToString();
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
                                return jarray.ToString();
                            }
                            else if (sbJson[0] == '{')
                            {
                                JObject jobject = JObject.Parse(sbJson.ToString());
                                return jobject.ToString();
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
                            return json.ToString();
                        }
                        else if (json[0] == '{')
                        {
                            JObject jobject = JObject.Parse(json);
                            return jobject.ToString();
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
            var body = new Body()
            {
                StartLineNumber = 0,
                EndLineNumber = 0
            };
            while (line != null)
            {
                if (Regex.IsMatch(line, SoapEnvSingleLineStartPattern))
                {
                    body.StartLineNumber = lineNumber;
                    body.EndLineNumber = lineNumber;
                    var xml0 = XElement.Parse(line);
                    body.Content = line;
                    body.ContentObject = xml0.Descendants(soap + "Body").First().Elements().First();
                    return body;
                }
                else if (Regex.IsMatch(line, SoapEnvStartPattern))
                {
                    body.StartLineNumber = lineNumber;
                    break;
                }
                else
                {
                    line = ReadLine(reader);
                }
            }
            if (line == null)
            {
                throw new Exception($"Cannot find startPattern {SoapEnvStartPattern}");
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
                throw new Exception($"Cannot find endPattern {SoapEnvEndPattern}");
            }
            body.EndLineNumber = lineNumber;
            var xml = XElement.Parse(blocks.ToString());
            body.Content = blocks.ToString();
            body.ContentObject = xml.Descendants(soap + "Body").First().Elements().First();
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
