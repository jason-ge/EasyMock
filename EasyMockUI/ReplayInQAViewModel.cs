using EasyMockLib.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace EasyMock.UI
{

    internal class BindableKeyValuePair : INotifyPropertyChanged
    {
        public string Key { get; }

        private string _value;
        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        public BindableKeyValuePair(string key, string value)
        {
            Key = key;
            _value = value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    internal class ReplayInQAViewModel : INotifyPropertyChanged
    {
        private HttpClient _httpClient;
        private readonly string _host;
        private readonly string _qaCredential;
        public ICommand SendRequestCommand { get; }

        private string _url;
        public string Url
        {
            get { return _url; }
            set
            {
                _url = value;
                if (Headers != null)
                {
                    foreach (var item in Headers.Where(pair => pair.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)))
                    {
                        item.Value = new Uri(_url).Host;
                    }
                }
                OnPropertyChanged(nameof(Url));
                OnPropertyChanged(nameof(Headers));
            }
        }
        public string Method { get; set; }
        public ServiceType ServiceType { get; set; }
        public string? RequestBody { get; set; }
        public string? ResponseStatus { get; set; }
        public string? ResponseBody { get; set; }

        private bool _hasResponse;
        public bool HasResponse
        {
            get => _hasResponse;
            set
            {
                _hasResponse = value;
                OnPropertyChanged(nameof(HasResponse));
            }
        }

        public ObservableCollection<BindableKeyValuePair> Headers { get; set; }

        public ReplayInQAViewModel(RequestResponsePair pair, string qaHost, string qaCredential)
        {
            _httpClient = App.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
            _host = qaHost;
            _qaCredential = qaCredential;
            Method = pair.Method;
            Url = $"https://{_host}{pair.Url}";
            ServiceType = pair.ServiceType;
            RequestBody = pair.RequestBody;
            ResponseBody = string.Empty;
            SendRequestCommand = new AsyncCommand<object>(OnSendRequest);
            List<BindableKeyValuePair> lstHeaders = [];
            foreach(var key in pair.Headers.AllKeys)
            {
                if (key == null) continue;
                if (key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    lstHeaders.Add(new BindableKeyValuePair(key, _host));
                    continue;
                }
                if (key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                lstHeaders.Add(new BindableKeyValuePair(key, pair.Headers[key]));
            }
            Headers = new ObservableCollection<BindableKeyValuePair>(lstHeaders);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task OnSendRequest(object? pair)
        {
            HasResponse = false;

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(_qaCredential);
            try
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = new HttpMethod(Method);
                    request.Content = (RequestBody != null) ? new StringContent(RequestBody) : null;
                    if (request.Content != null)
                    {
                        request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
                    }
                    foreach (var header in Headers)
                    {
                        if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        {
                            request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                    request.Headers.TryAddWithoutValidation("Authorization", "basic " + System.Convert.ToBase64String(plainTextBytes));
                    request.RequestUri = new Uri(Url, System.UriKind.RelativeOrAbsolute);

                    var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    string jsonResponse = (response.Content == null) ? string.Empty : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    ResponseStatus = $"Response Status: {response.StatusCode}";

                    if (string.IsNullOrEmpty(jsonResponse))
                    {
                        OnPropertyChanged(nameof(ResponseBody));
                        return;
                    }
                    else
                    {
                        try
                        {
                            ResponseBody = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonResponse), Formatting.Indented);
                        }
                        catch
                        {
                            ResponseBody = jsonResponse;
                        }
                    }
                    HasResponse = true;
                    OnPropertyChanged(nameof(ResponseStatus));
                    OnPropertyChanged(nameof(ResponseBody));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending request: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
