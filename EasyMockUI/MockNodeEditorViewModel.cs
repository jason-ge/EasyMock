using EasyMockLib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace EasyMock.UI
{
    public class MockNodeEditorViewModel
    {
        public double MaxEditorWidth => SystemParameters.WorkArea.Width * 0.8; // 80% of the screen width
        public ObservableCollection<StatusCodeOption> StatusCodeOptions { get; }
        public List<ServiceType> ServiceTypes { get; } = new() { ServiceType.REST, ServiceType.SOAP };

        public ServiceType ServiceType { get; set; }
        public string MethodName { get; set; }
        public string Url { get; set; }
        public string? RequestBody { get; set; }
        public string? ResponseBody { get; set; }
        public string ResponseDelay { get; set; }
        public StatusCodeOption SelectedStatusCodeOption { get; set; }
        public string? Description { get; set; }

        public ICommand OkCommand { get; }

        public MockNodeEditorViewModel()
        {
            OkCommand = new RelayCommand<object>(OnOk);

            StatusCodeOptions = new ObservableCollection<StatusCodeOption>
            {
                new StatusCodeOption { Code = 200, Name = HttpStatusCode.OK.ToString() },
                new StatusCodeOption { Code = 400, Name = HttpStatusCode.BadRequest.ToString() },
                new StatusCodeOption { Code = 401, Name = HttpStatusCode.Unauthorized.ToString() },
                new StatusCodeOption { Code = 403, Name = HttpStatusCode.Forbidden.ToString() },
                new StatusCodeOption { Code = 404, Name = HttpStatusCode.NotFound.ToString() },
                new StatusCodeOption { Code = 500, Name = HttpStatusCode.InternalServerError.ToString() },
                new StatusCodeOption { Code = 502, Name = HttpStatusCode.BadGateway.ToString() },
                new StatusCodeOption { Code = 504, Name = HttpStatusCode.GatewayTimeout.ToString() }
            };
        }

        private void OnOk(object? windowObj)
        {
            StringBuilder sbErrors = new StringBuilder();
            if (string.IsNullOrWhiteSpace(MethodName))
            {
                sbErrors.Append("Method Name is required." + Environment.NewLine);
            }
            if (string.IsNullOrWhiteSpace(Url))
            {
                sbErrors.Append("URL is required." + Environment.NewLine);
            }
            if (SelectedStatusCodeOption == null)
            {
                sbErrors.Append("Status Code is required." + Environment.NewLine);
            }
            if (!string.IsNullOrWhiteSpace(ResponseDelay) && (!int.TryParse(ResponseDelay, out int delay) || delay < 0))
            {
                sbErrors.Append("Response Delay must be a non-negative integer." + Environment.NewLine);
            }
            if (!string.IsNullOrEmpty(RequestBody))
            {
                var (formatSuccess, content) = FormatBody(ServiceType, RequestBody);
                if (formatSuccess)
                {
                    RequestBody = content;
                }
                else
                {
                    sbErrors.Append($"Request Body Error: {content}{Environment.NewLine}");
                }
            }
            if (!string.IsNullOrEmpty(ResponseBody))
            {
                var (formatSuccess, content) = FormatBody(ServiceType, ResponseBody);
                if (formatSuccess)
                {
                    ResponseBody = content;
                }
                else
                {
                    sbErrors.Append($"Response Body Error: {content}{Environment.NewLine}");
                }
            }
            if (sbErrors.Length > 0)
            {
                MessageBox.Show(sbErrors.ToString(), "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (windowObj is Window window)
            {
                window.DialogResult = true;
                window.Close();
            }
            else
            {
                MessageBox.Show("Invalid window object.");
            }
        }

        private static (bool, string) FormatBody(ServiceType serviceType, string bodyContent)
        {
            if (string.IsNullOrWhiteSpace(bodyContent)) return ( true, bodyContent );

            if (serviceType == ServiceType.REST)
            {
                bodyContent = bodyContent.Trim();
                try
                {
                    if (bodyContent.StartsWith("{"))
                        return (true, JObject.Parse(bodyContent).ToString(Formatting.Indented));
                    else if (bodyContent.StartsWith("["))
                        return (true, JArray.Parse(bodyContent).ToString(Formatting.Indented));
                    else
                        return (false, "Not a JSON format.");
                }
                catch(Exception ex)
                { 
                    return (false, $"Invalid JSON format: {ex.Message}");
                }   
            }
            else
            {
                try
                {
                    return (true, XElement.Parse(bodyContent).ToString());
                }
                catch(Exception ex)
                {
                    return (false, $"Invalid XML format: {ex.Message}");
                }
            }
        }
    }
}