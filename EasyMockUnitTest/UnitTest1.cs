using EasyMockLib;
using EasyMockLib.Models;
using Newtonsoft.Json;
using System.Net;
using System.Reflection.PortableExecutable;

namespace EasyMockUnitTest
{
    public class Tests
    {
        private Dictionary<string, Dictionary<string, List<string>>> SOAP_SERVICE_MATCHING_ELEMENTS;
        private readonly string GETPROFILE_REQUEST = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">  
   <soap:Header>
         <version>1.0</version>
         <appName>EasyMock Demo</appName>             
         <hostName>Demo Workstation</hostName>
         <userId>tester</userId>
         <timeStamp>2023–01–20T14:22:07.425</timeStamp>
   </soap:Header>    
   <soap:Body>
      <GetProfileRequest>
          <profileType>Personal</profileType>
          <profileId>{0}</profileId>          
      </GetProfileRequest>
   </soap:Body>
</soap:Envelope>";
        [SetUp]
        public void Setup()
        {
            SOAP_SERVICE_MATCHING_ELEMENTS = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(File.ReadAllText("SoapServiceMatchingElements.json"));
        }

        [Test]
        public void Test1()
        {
            var parser = new MockFileParser();
            var fileNode1 = parser.Parse("MockFiles\\RestDemo.txt");
            var fileNode2 = parser.Parse("MockFiles\\SoapDemo.txt");
            var node1 = fileNode1.GetMock(ServiceType.REST, "Countries", "GET", null, SOAP_SERVICE_MATCHING_ELEMENTS);
            Assert.IsNotNull(node1);
            Assert.That(node1.Response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var node2 = fileNode2.GetMock(ServiceType.SOAP, "ProfileService", "GetProfile", string.Format(GETPROFILE_REQUEST, "1000001"), SOAP_SERVICE_MATCHING_ELEMENTS);
            Assert.IsNotNull(node2);
            Assert.IsTrue(node2.Response.ResponseBody.Content.Contains("<firstName>JOHN</firstName>"));
            var node3 = fileNode2.GetMock(ServiceType.SOAP, "ProfileService", "GetProfile", string.Format(GETPROFILE_REQUEST, "1000002"), SOAP_SERVICE_MATCHING_ELEMENTS);
            Assert.IsNotNull(node3);
            Assert.IsTrue(node3.Response.ResponseBody.Content.Contains("<firstName>TERRY</firstName>"));
            Assert.AreEqual(fileNode1.Nodes.Count, 1);
            Assert.AreEqual(fileNode2.Nodes.Count, 2);
        }
    }
}