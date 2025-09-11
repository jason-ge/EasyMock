using EasyMockLib;
using EasyMockLib.MatchingPolicies;
using EasyMockLib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.Serialization;
using Application = System.Windows.Application;

namespace EasyMock.UI
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly string APP_ROOT_FOLDER = ConfigurationManager.AppSettings["AppRootFolder"];
        private readonly string MOCK_CONFIGURATION_FOLDER = ConfigurationManager.AppSettings["MockConfigurationFolder"];

        private RestRequestValueMatchingPolicy restMatchPolicy = new RestRequestValueMatchingPolicy()
        {
            RestServiceMatchingConfig = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(File.ReadAllText("RestServiceMatchingConfig.json"))
        };

        private SoapRequestValueMatchingPolicy soapMatchPolicy = new SoapRequestValueMatchingPolicy()
        {
            SoapServiceMatchingConfig = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(File.ReadAllText("SoapServiceMatchingConfig.json"))
        };
        private readonly IDialogService _dialogService;
        private readonly IFileDialogService _fileDialogService;
        private StringBuilder _logOutput = new StringBuilder();
        private readonly Dictionary<string, List<MockTreeNode>> MockNodeLookup = [];
        private MockNode _copiedNode;

        public ObservableCollection<MockTreeNode> RootNodes { get; } = [];
        public ObservableCollection<RequestResponsePair> RequestResponsePairs { get; } = [];
        public ICommand NewMockFileCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand LoadMockFileCommand { get; }
        public ICommand LoadDevLogCommand { get; }
        public ICommand StartServiceCommand { get; }
        public ICommand StopServiceCommand { get; }
        public ICommand TreeNodeDoubleClickCommand { get; }
        public ICommand TreeNodeRightClickCommand { get; }
        public ICommand WindowCloseCommand { get; }
        public ICommand ResponseBodyMouseEnterCommand { get; }
        public ICommand ResponseBodyMouseLeaveCommand { get; }
        public ICommand SaveLogCommand { get; }
        public ICommand? AddMockNodeCommand { get; }
        public ICommand? EditMockNodeCommand { get; }
        public ICommand? CopyMockNodeCommand { get; }
        public ICommand? PasteMockNodeCommand { get; }
        public ICommand? RefreshMockNodeCommand { get; }
        public ICommand? RemoveMockNodeCommand { get; }
        public ICommand? SaveMockNodeCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string LogOutput
        {
            get => _logOutput.ToString();
        }

        private bool IsServiceRunning { get; set; } = false;
        private bool IsMockTreeLoaded { get; set; } = false;

        private HttpListener listener = new HttpListener { Prefixes = { $"http://localhost:{ConfigurationManager.AppSettings["BindingPort"]}/" } };
        private CancellationTokenSource? tokenSource;

        public MainWindowViewModel(IFileDialogService fileDialogService, IDialogService dialogService)
        {
            foreach (var file in Directory.EnumerateFiles(ConfigurationManager.AppSettings["MockFileFolder"], "*.xml"))
            {
                var mockTreeNode = new MockTreeNode(new MockFileNode()
                {
                    MockFile = file,
                    Nodes = ParseXML(file)
                });
                RootNodes.Add(mockTreeNode);
            }
            SyncMockLookup();

            LoadDevLogCommand = new RelayCommand<object?>(_ => LoadDevLog(), _ => IsMockTreeLoaded);
            ClearLogCommand = new RelayCommand<object?>(_ =>
            {
                RequestResponsePairs.Clear();
                _logOutput = new StringBuilder();
                OnPropertyChanged(nameof(RequestResponsePairs));
                OnPropertyChanged(nameof(LogOutput));
            }, _ => RequestResponsePairs.Count > 0);

            SaveLogCommand = new RelayCommand<object>(OnSaveLog, _ => RequestResponsePairs.Count > 0);
            NewMockFileCommand = new RelayCommand<object>(NewMockFile);

            LoadMockFileCommand = new RelayCommand<object>(_ => LoadMockFile(), _ => IsMockTreeLoaded);
            StartServiceCommand = new RelayCommand<object>(_ => StartWebServer(), _ => !IsServiceRunning && IsMockTreeLoaded);
            StopServiceCommand = new RelayCommand<object>(_ => StopWebServer(), _ => IsServiceRunning);
            WindowCloseCommand = new RelayCommand<CancelEventArgs>(OnClosing);
            ResponseBodyMouseEnterCommand = new RelayCommand<RequestResponsePair>(OnResponseBodyMouseEnter);
            ResponseBodyMouseLeaveCommand = new RelayCommand<RequestResponsePair>(OnResponseBodyMouseLeave);
            AddMockNodeCommand = new RelayCommand<MockTreeNode?>(OnAddMockNodeAction);
            EditMockNodeCommand = new RelayCommand<MockTreeNode?>(OnEditMockNodeAction);
            CopyMockNodeCommand = new RelayCommand<MockTreeNode?>(OnCopyMockNodeAction);
            PasteMockNodeCommand = new RelayCommand<MockTreeNode?>(OnPasteMockNodeAction, _ => _copiedNode != null);
            RefreshMockNodeCommand = new RelayCommand<MockTreeNode?>(OnRefreshMockNodeAction);
            RemoveMockNodeCommand = new RelayCommand<MockTreeNode?>(OnRemoveMockNodeAction);
            SaveMockNodeCommand = new RelayCommand<MockTreeNode?>(OnSaveMockNodeAction);
            TreeNodeDoubleClickCommand = new RelayCommand<MockTreeNode?>(OnTreeViewItemDoubleClick);
            TreeNodeRightClickCommand = new RelayCommand<MockTreeNode?>(OnTreeNodeRightClickCommand);

            _dialogService = dialogService;
            _fileDialogService = fileDialogService;
        }

        private void SyncMockLookup()
        {
            lock (MockNodeLookup)
            {
                IsMockTreeLoaded = false;
                MockNodeLookup.Clear();
                foreach (var node in RootNodes)
                {
                    AppendMockLookup(node);
                }
                IsMockTreeLoaded = true;
            }
        }

        private void AppendMockLookup(MockTreeNode mockTreeNode)
        {
            var mockFileNode = mockTreeNode.Tag as MockFileNode;
            if (mockFileNode == null)
            {
                throw new ArgumentException($"{nameof(mockTreeNode)} is not MockFileNode type");
            }
            lock (MockNodeLookup)
            {
                foreach (var node in mockTreeNode.Children)
                {
                    if (!MockNodeLookup.ContainsKey($"{((MockNode)node.Tag).Url.ToLower()}"))
                    {
                        MockNodeLookup.Add(((MockNode)node.Tag).Url.ToLower(), [mockTreeNode]);
                    }
                    else
                    {
                        MockNodeLookup[((MockNode)node.Tag).Url.ToLower()].Add(mockTreeNode);
                    }
                }
            }
        }

        private void PrependMockLookup(MockTreeNode mockTreeNode)
        {
            var mockFileNode = mockTreeNode.Tag as MockFileNode;
            if (mockFileNode == null)
            {
                throw new ArgumentException($"{nameof(mockTreeNode)} is not MockFileNode type");
            }
            lock (MockNodeLookup)
            {
                foreach (var node in mockTreeNode.Children)
                {
                    var mockNode = (MockNode)node.Tag;
                    if (!MockNodeLookup.ContainsKey(mockNode.Url.ToLower()))
                    {
                        MockNodeLookup.Add(mockNode.Url.ToLower(), [mockTreeNode]);
                    }
                    else
                    {
                        MockNodeLookup[mockNode.Url.ToLower()].Insert(0, mockTreeNode);
                    }
                }
            }
        }

        private void UpdateMockLookup(MockTreeNode mockTreeNode, string url)
        {
            var mockFileNode = mockTreeNode.Tag as MockFileNode;
            if (mockFileNode == null)
            {
                throw new ArgumentException($"{nameof(mockTreeNode)} is not MockFileNode type");
            }
            if (MockNodeLookup.ContainsKey(url.ToLower()))
            {
                var list = MockNodeLookup[url.ToLower()];
                if (list.IndexOf(mockTreeNode) < 0)
                {
                    list.Add(mockTreeNode);
                }
            }
            else
            {
                MockNodeLookup.Add(url.ToLower(), [mockTreeNode]);
            }
        }
        private void RemoveMockLookup(MockTreeNode mockTreeNode)
        {
            var mockFileNode = mockTreeNode.Tag as MockFileNode;
            if (mockFileNode == null)
            {
                throw new ArgumentException($"{nameof(mockTreeNode)} is not MockFileNode type");
            }
            foreach (var node in mockTreeNode.Children)
            {
                lock (MockNodeLookup)
                {
                    var mockNode = (MockNode)node.Tag;
                    if (MockNodeLookup.ContainsKey(mockNode.Url.ToLower()))
                    {
                        var list = MockNodeLookup[mockNode.Url.ToLower()];
                        if (list.IndexOf(mockTreeNode) >= 0)
                        {
                            list.Remove(mockTreeNode);
                        }
                        if (list.Count == 0)
                        {
                            MockNodeLookup.Remove(mockNode.Url.ToLower());
                        }
                    }
                }
            }
        }

        private static void DedentRequestResponse(MockNode mock)
        {
            if (mock == null) return;

            if (mock.ServiceType == ServiceType.REST)
            {
                // Request
                var reqContent = mock.Request?.RequestBody?.Content;
                if (!string.IsNullOrWhiteSpace(reqContent))
                {
                    reqContent = reqContent.Trim();
                    if (reqContent.StartsWith("{"))
                        mock.Request.RequestBody.Content = JObject.Parse(reqContent).ToString(Formatting.Indented);
                    else if (reqContent.StartsWith("["))
                        mock.Request.RequestBody.Content = JArray.Parse(reqContent).ToString(Formatting.Indented);
                }

                // Response
                var respContent = mock.Response?.ResponseBody?.Content;
                if (!string.IsNullOrWhiteSpace(respContent))
                {
                    respContent = respContent.Trim();
                    if (respContent.StartsWith("{"))
                        mock.Response.ResponseBody.Content = JObject.Parse(respContent).ToString(Formatting.Indented);
                    else if (respContent.StartsWith("["))
                        mock.Response.ResponseBody.Content = JArray.Parse(respContent).ToString(Formatting.Indented);
                }
            }
            else if (mock.ServiceType == ServiceType.SOAP)
            {
                // Request
                var reqContent = mock.Request?.RequestBody?.Content;
                if (!string.IsNullOrWhiteSpace(reqContent))
                {
                    mock.Request.RequestBody.Content = XElement.Parse(reqContent.Trim()).ToString();
                }

                // Response
                var respContent = mock.Response?.ResponseBody?.Content;
                if (!string.IsNullOrWhiteSpace(respContent))
                {
                    mock.Response.ResponseBody.Content = XElement.Parse(respContent.Trim()).ToString();
                }
            }
        }

        private void LoadDevLog()
        {
            var filePath = _fileDialogService.OpenFile("Dev Log Files|*.txt;*.log");
            if (string.IsNullOrEmpty(filePath)) return;

            var mockTreeNode = new MockTreeNode(new DevLogParser().Parse(filePath)) { IsDirty = true };
            PrependMockLookup(mockTreeNode);
            RootNodes.Insert(0, mockTreeNode);
        }

        private void NewMockFile(object? parameter)
        {
            var viewModel = new NewMockFileViewModel();
            var newMockFileWindow = new NewMockFileWindow() { DataContext = viewModel, Owner = Application.Current.MainWindow };

            if (newMockFileWindow.ShowDialog() == true)
            {
                var fileName = Path.Combine(ConfigurationManager.AppSettings["MockFileFolder"], viewModel.MockFileName);
                if (File.Exists(fileName))
                {
                    MessageBox.Show("The mock file already exists.");
                    return;
                }
                MockTreeNode node = new MockTreeNode(new MockFileNode()
                {
                    MockFile = fileName,
                });
                node.IsDirty = true;
                int i = 0;
                for (; i < RootNodes.Count; i++)
                {
                    if (Path.GetFileName(((MockFileNode)RootNodes[i].Tag).MockFile).CompareTo(viewModel.MockFileName) < 0)
                    {
                        continue;
                    }
                    break;
                }
                if (i == RootNodes.Count)
                {
                    RootNodes.Add(node);
                }
                else
                {
                    RootNodes.Insert(i, node);
                }
            }
        }

        private void LoadMockFile()
        {
            var filePath = _fileDialogService.OpenFile("XML Files (*.xml)|*.xml");
            if (string.IsNullOrEmpty(filePath)) return;

            var mockTreeNode = new MockTreeNode(new MockFileNode()
            {
                MockFile = filePath,
                Nodes = ParseXML(filePath)
            });
            PrependMockLookup(mockTreeNode);
            RootNodes.Insert(0, mockTreeNode);
        }

        private void StartWebServer()
        {
            tokenSource = new CancellationTokenSource();
            tokenSource.Token.Register(() =>
            {
                if (listener.IsListening)
                {
                    listener.Stop();
                }
            });

            Task.Run(async () =>
            {
                listener.Start();
                AppendOutput($"Mock service started.\n");
                Application.Current.Dispatcher.Invoke(() => IsServiceRunning = true);

                while (!tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var context = await listener.GetContextAsync();
                        Task.Run(() => ProcessRequest(context), tokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        if (e is HttpListenerException) return;
                    }
                }
            });
            Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(MOCK_CONFIGURATION_FOLDER, "*.*", SearchOption.AllDirectories))
                {
                    CopyIfMockFileChanged(file);
                }
            });
        }

        private void StopWebServer()
        {
            AppendOutput($"Mock service stopped.{Environment.NewLine}");
            Application.Current.Dispatcher.Invoke(() => IsServiceRunning = false);
            tokenSource?.Cancel();
        }

        private void OnSaveLog(object? parameter)
        {
            string? logFilePath = _fileDialogService.SaveFile("XML Files (*.xml)|*.xml", "MockServiceLog.xml");
            if (string.IsNullOrEmpty(logFilePath)) return;

            ObservableCollection<MockNode> nodes = new ObservableCollection<MockNode>();
            var mockFileNode = new MockFileNode() { MockFile = logFilePath, Nodes = new List<MockNode>() };
            foreach (RequestResponsePair pair in RequestResponsePairs)
            {
                MockNode node = new MockNode()
                {
                    Request = new Request(),
                    Response = new Response(),
                    Url = pair.Url,
                    MethodName = pair.Method,
                    ServiceType = pair.ServiceType
                };

                node.Request.RequestBody.Content = pair.RequestBody;
                node.Response.ResponseBody.Content = pair.ResponseBody;
                node.Response.StatusCode = pair.StatusCode;
                nodes.Add(node);
            }

            var serializer = new XmlSerializer(typeof(ObservableCollection<MockNode>));
            using (var writer = new StreamWriter(logFilePath))
            {
                serializer.Serialize(writer, nodes);
            }

            MessageBox.Show("Log saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyIfMockFileChanged(string file)
        {
            string configFile, mockFile;
            if (file.StartsWith(APP_ROOT_FOLDER, StringComparison.OrdinalIgnoreCase))
            {
                configFile = file;
                mockFile = file.Replace(APP_ROOT_FOLDER, MOCK_CONFIGURATION_FOLDER, StringComparison.OrdinalIgnoreCase);
            }
            else if (file.StartsWith(MOCK_CONFIGURATION_FOLDER))
            {
                mockFile = file;
                configFile = file.Replace(MOCK_CONFIGURATION_FOLDER, APP_ROOT_FOLDER, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return;
            }
            if (File.Exists(mockFile) && !File.GetLastWriteTime(mockFile).Equals(File.GetLastWriteTime(configFile)))
            {
                File.Copy(mockFile, configFile, true);
                AppendOutput($"Replaced {configFile} with mock config.{Environment.NewLine}");
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            DateTime timestamp = DateTime.Now;
            using (var response = context.Response)
            {
                (var mock, var method, var requestContent) = GetMock(context);
                var serviceType = context.Request.ContentType == null ? ServiceType.REST : context.Request.ContentType.StartsWith("text/xml") ? ServiceType.SOAP : ServiceType.REST;
                try
                {
                    if (mock != null)
                    {
                        SendMockResponse(mock, context, response);
                        AddRequestResponsePair(mock, context.Request.Url.PathAndQuery, serviceType, method, requestContent, DateTime.Now.Subtract(timestamp).TotalMilliseconds);
                    }
                    else
                    {
                        SendResponseContent("", HttpStatusCode.NotFound, context, response);
                        AddRequestResponsePair(null, context.Request.Url.PathAndQuery, serviceType, method, requestContent, DateTime.Now.Subtract(timestamp).TotalMilliseconds);
                    }
                }
                catch (Exception e)
                {
                    SendResponseContent("", HttpStatusCode.InternalServerError, context, response);

                    // Optionally log the exception as a request/response pair
                    AddRequestResponsePair(null, context.Request.Url.PathAndQuery, serviceType, method, requestContent, DateTime.Now.Subtract(timestamp).TotalMilliseconds, e);
                }
            }
        }

        private (MockTreeNode?, string, string) GetMock(HttpListenerContext context)
        {
            MockTreeNode? mock = null;
            string requestContent = ReadRequest(context);
            string method = context.Request.HttpMethod.ToString();
            if (context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) || context.Request.ContentType.StartsWith("application/json"))
            {
                // REST request
                method = context.Request.HttpMethod.ToString();
                mock = GetMock(ServiceType.REST, context.Request.Url.PathAndQuery, method, requestContent, restMatchPolicy);
            }
            else if (context.Request.ContentType.StartsWith("text/xml"))
            {
                // SOAP request
                method = GetSoapAction(requestContent);
                mock = GetMock(ServiceType.SOAP, context.Request.Url.PathAndQuery, method, requestContent, soapMatchPolicy);
            }
            else
            {
                MessageBox.Show($"Unknown content type {context.Request.ContentType}");
            }
            return (mock, method, requestContent);
        }
        private MockTreeNode?  GetMock(ServiceType serviceType, string url, string method, string requestContent, IMatchingPolicy matchingPolicy)
        {
            var urlLower = url.ToLower();
            lock (MockNodeLookup)
            {
                if (MockNodeLookup.ContainsKey(urlLower))
                {
                    foreach (var node in MockNodeLookup[urlLower])
                    {
                        MockNode? mock = ((MockFileNode)node.Tag).GetMock(serviceType, url, method, requestContent, matchingPolicy);
                        if (mock != null)
                        {
                            return node.Children.FirstOrDefault(c => c.Tag == mock);
                        }
                    }
                }
            }
            return null;
        }

        private string ReadRequest(HttpListenerContext context)
        {
            using (var body = context.Request.InputStream)
            using (var reader = new StreamReader(body, context.Request.ContentEncoding))
            {
                //Get the data that was sent to us
                return FormatRequestContent(reader.ReadToEnd());
            }
        }
        private static string GetSoapAction(string soapEnv)
        {
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XDocument doc = XDocument.Parse(soapEnv);
            return doc.Descendants(soap + "Body").First().Elements().First().Name.LocalName.Replace("Request", "");
        }
        private void SendResponseContent(string content, HttpStatusCode status, HttpListenerContext context, HttpListenerResponse response)
        {
            content ??= string.Empty;
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            response.ContentType = context.Request.ContentType;
            response.StatusCode = (int)status;
            var correlationId = context.Request.Headers.AllKeys.FirstOrDefault(h => h.Equals("X-BMO-CorrelationId", StringComparison.OrdinalIgnoreCase));
            if (correlationId != null)
            {
                response.Headers.Add(correlationId, context.Request.Headers[correlationId]);
            }
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Flush();
        }

        private void SendMockResponse(MockTreeNode node, HttpListenerContext context, HttpListenerResponse response)
        {
            var mock = node.Tag as MockNode;
            if (mock != null)
            {
                if (mock.Response.Delay != 0)
                {
                    AppendOutput("Sleeping for " + mock.Response.Delay + " seconds...");
                    Thread.Sleep(mock.Response.Delay * 1000);
                }
                 SendResponseContent(mock.Response.ResponseBody.Content, mock.Response.StatusCode, context, response);
            } 
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void OnTreeNodeRightClickCommand(MockTreeNode? node)
        {
            if (node != null)
            {
                node.IsSelected = true;
            }
        }

        private void OnTreeViewItemDoubleClick(MockTreeNode? node)
        {            
            if (node != null && node.NodeType == NodeTypes.MockItem)
            {
                OnEditMockNodeAction(node);
            }
        }

        private void SaveMock(MockTreeNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            var mockFileNode = node.Tag as MockFileNode;
            if (mockFileNode != null)
            {
                var serializer = new XmlSerializer(typeof(List<MockNode>));
                mockFileNode.MockFile = mockFileNode.MockFile.Replace(".txt", ".xml");
                using (var writer = new StreamWriter(mockFileNode.MockFile))
                {
                    serializer.Serialize(writer, mockFileNode.Nodes);
                }
                MessageBox.Show($"Mocks saved to {mockFileNode.MockFile}.", "Saved", MessageBoxButton.OK);
            }
            node.IsDirty = false;
        }

        private void OnAddMockNodeAction(MockTreeNode? node)
        {
            if (node != null && node.Tag is MockFileNode mockFileNode && mockFileNode != null)
            {
                var editor = new MockNodeEditorWindow();
                if (editor.ShowDialog() == true)
                {
                    var viewModel = editor.DataContext as MockNodeEditorViewModel;
                    var newMockNode = new MockNode()
                    {
                        ServiceType = viewModel.ServiceType,
                        MethodName = viewModel.MethodName,
                        Url = viewModel.Url,
                        Description = viewModel.Description,
                        Request = new Request
                        {
                            RequestBody = new Body
                            {
                                Content = viewModel.RequestBody
                            }
                        },
                        Response = new Response
                        {
                            ResponseBody = new Body
                            {
                                Content = viewModel.ResponseBody
                            },
                            StatusCode = (HttpStatusCode)viewModel.SelectedStatusCodeOption.Code,
                            Delay = int.TryParse(viewModel.ResponseDelay, out int delay) ? delay : 0
                        }
                    };
                    mockFileNode.Nodes.Add(newMockNode);
                    node.Children.Add(new MockTreeNode(newMockNode) { Parent = node });
                    node.IsDirty = true;
                }
            }
        }

        private void OnCopyMockNodeAction(MockTreeNode? node)
        {
            if (node != null && node.Tag is MockNode mockNode && mockNode != null)
            {
                _copiedNode = mockNode;
            }
        }

        private void OnPasteMockNodeAction(MockTreeNode? node)
        {
            if (node != null && node.Tag is MockFileNode mockFileNode && mockFileNode != null && _copiedNode != null)
            {
                var mockNode = new MockNode()
                {
                    Url = _copiedNode.Url,
                    MethodName = _copiedNode.MethodName,
                    Description = _copiedNode.Description,
                    ServiceType = _copiedNode.ServiceType,
                };
                if (_copiedNode.Request != null)
                {
                    mockNode.Request = new Request();
                    mockNode.Request.RequestBody.Content = _copiedNode.Request.RequestBody.Content;
                }
                if (_copiedNode.Response != null)
                {
                    mockNode.Response = new Response();
                    mockNode.Response.ResponseBody.Content = _copiedNode.Response.ResponseBody.Content;
                    mockNode.Response.StatusCode = _copiedNode.Response.StatusCode;
                    mockNode.Response.Delay = _copiedNode.Response.Delay;
                }

                mockFileNode.Nodes.Add(mockNode);
                node.Children.Add(new MockTreeNode(mockNode) { Parent = node });
                node.IsDirty = true;
                UpdateMockLookup(node, mockNode.Url);
                _copiedNode = null!;
            }
        }

        private void OnEditMockNodeAction(MockTreeNode? node)
        {
            if (node != null && node.Tag is MockNode mockNode && mockNode != null)
            {
                var viewModel = new MockNodeEditorViewModel
                {
                    ServiceType = mockNode.ServiceType,
                    MethodName = mockNode.MethodName,
                    Url = mockNode.Url,
                    RequestBody = mockNode.Request?.RequestBody.Content,
                    ResponseBody = mockNode.Response.ResponseBody.Content,
                    ResponseDelay = mockNode.Response.Delay.ToString(),
                    Description = mockNode.Description
                };

                viewModel.SelectedStatusCodeOption = viewModel.StatusCodeOptions
                    .FirstOrDefault(o => o.Code == (int)mockNode.Response.StatusCode);

                var mockNodeEditor = new MockNodeEditorWindow() { DataContext = viewModel, Owner = Application.Current.MainWindow };

                if (mockNodeEditor.ShowDialog() == true)
                {
                    mockNode.ServiceType = viewModel.ServiceType;
                    if (!mockNode.MethodName.Equals(viewModel.MethodName, StringComparison.OrdinalIgnoreCase))
                    {
                        mockNode.MethodName = viewModel.MethodName;
                        node.OnMockNodePropertyChanged(this, new PropertyChangedEventArgs(nameof(MockNode.MethodName)));
                    }
                    if (!mockNode.Url.Equals(viewModel.Url, StringComparison.OrdinalIgnoreCase))
                    {
                        mockNode.Url = viewModel.Url;
                        node.OnMockNodePropertyChanged(this, new PropertyChangedEventArgs(nameof(MockNode.Url)));
                    }

                    if (mockNode.ServiceType == ServiceType.REST && mockNode.MethodName == "GET")
                    {
                        mockNode.Request = null; // GET requests typically don't have a body
                    }
                    else
                    {
                        if (mockNode.Request == null) mockNode.Request = new Request();
                        mockNode.Request.RequestBody.Content = viewModel.RequestBody;
                    }
                    mockNode.Response.ResponseBody.Content = viewModel.ResponseBody;
                    mockNode.Response.Delay = int.TryParse(viewModel.ResponseDelay, out int delay) ? delay : 0;
                    if ((int)mockNode.Response.StatusCode != viewModel.SelectedStatusCodeOption?.Code){
                        mockNode.Response.StatusCode = (HttpStatusCode)viewModel.SelectedStatusCodeOption.Code;
                        node.OnMockNodePropertyChanged(this, new PropertyChangedEventArgs(nameof(Response.StatusCode)));
                    }
                    mockNode.Description = viewModel.Description;

                    if (node.Parent is MockTreeNode mockFileNode)
                    {
                        mockFileNode.IsDirty = true;
                        UpdateMockLookup(mockFileNode, mockNode.Url);
                    }
                }
            }
        }
        private void OnRefreshMockNodeAction(MockTreeNode? node)
        {
            if (node != null && node.Tag is MockFileNode mockFileNode && mockFileNode != null)
            {
                if (node.IsDirty)
                {
                    var result = MessageBox.Show(
                        "You have made changes to the mockup. Do you want to discard the changes?",
                        "Confirmation", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No) return;
                }
                var index = RootNodes.IndexOf(node);
                if (index < 0) return;

                var newMockFileNode = new MockFileNode()
                {
                    MockFile = mockFileNode.MockFile,
                    Nodes = ParseXML(mockFileNode.MockFile)
                };
                RootNodes[index] = new MockTreeNode(newMockFileNode);
                SyncMockLookup();
            }
        }

        private void OnRemoveMockNodeAction(MockTreeNode? node)
        {
            if (node != null)
            {
                if (node.Tag is MockFileNode mockFileNode && mockFileNode != null)
                {
                    if (node.IsDirty)
                    {
                        var result = MessageBox.Show(
                            "You have made changes to the mockup. Do you want to save it before remove it?",
                            "Confirmation", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            SaveMock(node);
                        }
                    }
                    RemoveMockLookup(node);
                    RootNodes.Remove(node);
                }
                else if (node.Tag is MockNode mockNode && mockNode != null)
                {
                    var fileTreeNode = node.Parent;
                    if (fileTreeNode != null)
                    {
                        var fileNode = fileTreeNode.Tag as MockFileNode;
                        if (fileNode != null)
                            fileNode.Nodes.Remove(mockNode);

                        fileTreeNode.IsDirty = true;
                        fileTreeNode.Children.Remove(node);
                        SyncMockLookup();
                    }
                }
            }
        }

        private void OnSaveMockNodeAction(MockTreeNode? node)
        {
            if (node != null)
            {
                if (!node.IsDirty)
                {
                    MessageBox.Show("There is no changes to save.", "Warning");
                    return;
                }
                SaveMock(node);
            }
        }

        public void AppendOutput(string message)
        {
            _logOutput.Append(message + Environment.NewLine);
            OnPropertyChanged(nameof(LogOutput));
        }

        public void AddRequestResponsePair(MockTreeNode? node, string url, ServiceType serviceType, string method, string requestContent, double responseTime, Exception? ex = null)
        {
            var mock = node?.Tag as MockNode;
            string requestBody = requestContent;
            string responseBody = mock?.Response.ResponseBody?.Content ?? string.Empty;

            var pair = new RequestResponsePair
            {
                Method = method,
                Url = url,
                ServiceType = serviceType,
                ResponseTimeInMs = (long)responseTime,
                RequestBody = requestContent,
                ResponseBody = mock?.Response?.ResponseBody?.Content ?? string.Empty,
                MockNodeSource = node
            };
            if (mock == null || mock.Response == null)
            {
                if (ex == null)
                {
                    pair.StatusCode = HttpStatusCode.NotFound;
                }
                else
                {
                    pair.StatusCode = HttpStatusCode.InternalServerError;
                    pair.ResponseBody = ex.Message;
                }
            }
            else
            {
                pair.StatusCode = mock.Response.StatusCode;
            }
            Application.Current.Dispatcher.Invoke(() =>
                {
                    RequestResponsePairs.Add(pair);
                });

            OnPropertyChanged(nameof(RequestResponsePairs));
        }

        private List<MockNode> ParseXML(string filepath)
        {
            var serializer = new XmlSerializer(typeof(List<MockNode>));
            List<MockNode> nodes;
            try
            {

                using (var xml = File.OpenRead(filepath))
                {
                    nodes = (List<MockNode>)serializer.Deserialize(xml);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load mock file '{filepath}':\n{ex.Message}", "File Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<MockNode>();
            }

            // Dedent and assign ContentObject for each node's request and response body
            foreach (var mockNode in nodes)
            {
                try
                {


                    DedentRequestResponse(mockNode);

                    if (mockNode.Request?.RequestBody?.Content != null)
                    {
                        var content = mockNode.Request.RequestBody.Content.Trim();
                        if (mockNode.ServiceType == ServiceType.REST)
                        {
                            try { mockNode.Request.RequestBody.ContentObject = JToken.Parse(content); }
                            catch { mockNode.Request.RequestBody.ContentObject = null; }
                        }
                        else if (mockNode.ServiceType == ServiceType.SOAP)
                        {
                            try { mockNode.Request.RequestBody.ContentObject = XElement.Parse(content); }
                            catch { mockNode.Request.RequestBody.ContentObject = null; }
                        }
                    }

                    if (mockNode.Response?.ResponseBody?.Content != null)
                    {
                        var content = mockNode.Response.ResponseBody.Content.Trim();
                        if (mockNode.ServiceType == ServiceType.REST)
                        {
                            try { mockNode.Response.ResponseBody.ContentObject = JToken.Parse(content); }
                            catch { mockNode.Response.ResponseBody.ContentObject = null; }
                        }
                        else if (mockNode.ServiceType == ServiceType.SOAP)
                        {
                            try { mockNode.Response.ResponseBody.ContentObject = XElement.Parse(content); }
                            catch { mockNode.Response.ResponseBody.ContentObject = null; }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing mock node in '{filepath}':\n{ex.Message}", "MockNode Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            return nodes;
        }

        private void OnClosing(CancelEventArgs? e)
        {
            if (e != null)
            {
                if (RootNodes.Any(n => n.IsDirty))
                {
                    if (!_dialogService.ConfirmCloseWithUnsavedChanges())
                    {
                        e.Cancel = true; // Cancel the close operation
                        return; // User chose not to close
                    }
                }
            }

            Application.Current.Shutdown();
        }

        private void OnResponseBodyMouseEnter(RequestResponsePair? pair)
        {
            var node = pair?.MockNodeSource as MockTreeNode;
            if (node != null)
            {
                node.IsHovered = true;
                node.UpdateAncestorStates();
            }
        }

        private void OnResponseBodyMouseLeave(RequestResponsePair? pair)
        {
            var node = pair?.MockNodeSource as MockTreeNode;
            if (node != null)
            {
                node.IsHovered = false;
                node.UpdateAncestorStates();
            }
        }

        private string FormatRequestContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            content = content.Trim();

            try
            {
                if (content.StartsWith("{") || content.StartsWith("["))
                {
                    // JSON
                    var parsed = Newtonsoft.Json.Linq.JToken.Parse(content);
                    return parsed.ToString(Newtonsoft.Json.Formatting.Indented);
                }
                else if (content.StartsWith("<"))
                {
                    // XML
                    var parsed = System.Xml.Linq.XElement.Parse(content);
                    return parsed.ToString();
                }
            }
            catch
            {
                // If parsing fails, return original content
            }

            return content;
        }
    }
}