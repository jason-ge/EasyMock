using ControlzEx.Theming;
using EasyMockLib;
using EasyMockLib.MatchingPolicies;
using EasyMockLib.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;

namespace EasyMock.UI
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const string RestServiceMatchingConfigFile = "RestServiceMatchingConfig.json";
        private const string SoapServiceMatchingConfigFile = "SoapServiceMatchingConfig.json";
        private readonly string _appRootFolder;
        private readonly string _mockConfigFolder;
        private readonly string _mockFileFolder;
        private readonly string _theme;
        private readonly string _bindingPortNumber;
        private readonly string _qaHost;
        private readonly string _qaCredential;

        private readonly Dictionary<string, Dictionary<string, List<string>>> _restMatchConfig;
        private readonly Dictionary<string, Dictionary<string, List<string>>> _soapMatchConfig;

        private readonly IDialogService _dialogService;
        private readonly IFileDialogService _fileDialogService;
        private readonly StringBuilder _logOutput = new StringBuilder();
        private readonly MockRepository _mockRepository;
        private MockNode? _copiedNode;
        private bool _isServiceRunning;

        private readonly HttpListener _listener;
        private CancellationTokenSource? tokenSource;

        public ObservableCollection<MockTreeNode> RootNodes { get; }
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
        public ICommand ResponseBodyDoubleClickCommand { get; }
        public ICommand ReplayInQACommand { get; }
        public ICommand SaveLogCommand { get; }
        public ICommand? AddMockNodeCommand { get; }
        public ICommand? EditMockNodeCommand { get; }
        public ICommand? CopyMockNodeCommand { get; }
        public ICommand? PasteMockNodeCommand { get; }
        public ICommand? RefreshMockNodeCommand { get; }
        public ICommand? RemoveMockNodeCommand { get; }
        public ICommand? SaveMockNodeCommand { get; }

        private bool _isBusy = false;
        public bool IsBusy 
        { 
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string LogOutput
        {
            get => _logOutput.ToString();
        }

        public MainWindowViewModel(IFileDialogService fileDialogService, IDialogService dialogService)
        {
            _appRootFolder = AssignAppRoot();
            _bindingPortNumber = AssignBindingPort();
            _mockConfigFolder = AssignMockConfigFolder();
            _mockFileFolder = AssignMockFileFolder();
            _qaCredential = App.Config["QACredential"] ?? string.Empty;
            _qaHost = App.Config["QAHost"] ?? string.Empty;
            _soapMatchConfig = LoadMatchingConfig(SoapServiceMatchingConfigFile);
            _restMatchConfig = LoadMatchingConfig(RestServiceMatchingConfigFile);
            _theme = App.Config["Theme"] ?? "Dark.Blue";
            _listener = new HttpListener { Prefixes = { $"http://localhost:{_bindingPortNumber}/" } };
            _mockRepository = new MockRepository(_restMatchConfig, _soapMatchConfig);
            ThemeManager.Current.ChangeTheme(Application.Current, _theme);

            if (!string.IsNullOrEmpty(_mockFileFolder) && Directory.Exists(_mockFileFolder))
            {
                List<MockTreeNode> mockTreeNodes = [];
                foreach (var file in Directory.EnumerateFiles(_mockFileFolder, "*.xml"))
                {
                    var mockTreeNode = new MockTreeNode(new MockFileNode(file)
                    {
                        Nodes = ParseXML(file)
                    }, NodeTypes.MockFile);
                    mockTreeNodes.Add(mockTreeNode);
                }
                RootNodes = new ObservableCollection<MockTreeNode>(mockTreeNodes);
                SyncMockLookup();
            }

            ClearLogCommand = new RelayCommand<object?>(_ =>
            {
                RequestResponsePairs.Clear();
                _logOutput.Clear();
                OnPropertyChanged(nameof(RequestResponsePairs));
                OnPropertyChanged(nameof(LogOutput));
            }, _ => RequestResponsePairs.Count > 0 || _logOutput.Length > 0);

            SaveLogCommand = new RelayCommand<object>(OnSaveLog, _ => RequestResponsePairs.Count > 0);

            NewMockFileCommand = new RelayCommand<object>(NewMockFile);
            LoadDevLogCommand = new RelayCommand<object?>(async => LoadDevLog());
            LoadMockFileCommand = new RelayCommand<object>(_ => LoadMockFile());
            StartServiceCommand = new RelayCommand<object>(_ => StartWebServer(), _ => !_isServiceRunning);
            StopServiceCommand = new RelayCommand<object>(_ => StopWebServer(), _ => _isServiceRunning);
            WindowCloseCommand = new RelayCommand<CancelEventArgs>(OnClosing);
            ResponseBodyMouseEnterCommand = new RelayCommand<RequestResponsePair>(OnResponseBodyMouseEnter);
            ResponseBodyMouseLeaveCommand = new RelayCommand<RequestResponsePair>(OnResponseBodyMouseLeave);
            ResponseBodyDoubleClickCommand = new RelayCommand<RequestResponsePair>(OnResponseBodyDoubleClick);
            ReplayInQACommand = new RelayCommand<RequestResponsePair>(OnReplayInQA);
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

        private string AssignAppRoot()
        {
            var appRootFolder = App.Config["AppRootFolder"];
            if (string.IsNullOrEmpty(appRootFolder))
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, "AppRootFolder is not configured properly in appsettings.json.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }, DispatcherPriority.ApplicationIdle);
                            }
            if (Directory.Exists(appRootFolder) == false)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"AppRootFolder '{appRootFolder}' does not exist.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }, DispatcherPriority.ApplicationIdle);
                            }
            return appRootFolder!;
        }

        private string AssignMockConfigFolder()
        {
            var mockConfigFolder = App.Config["MockConfigurationFolder"];
            if (string.IsNullOrEmpty(mockConfigFolder))
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, "MockConfigurationFolder is not configured properly in appsettings.json.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }, DispatcherPriority.ApplicationIdle);
            }
            if (Directory.Exists(mockConfigFolder) == false)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"MockConfigurationFolder '{mockConfigFolder}' does not exist.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }, DispatcherPriority.ApplicationIdle);
            }
            return mockConfigFolder!;
        }

        private string AssignMockFileFolder()
        {
            var mockFileFolder = App.Config["MockFileFolder"];

            if (string.IsNullOrEmpty(mockFileFolder))
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, "MockFileFolder is not configured properly in appsettings.json.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }, DispatcherPriority.ApplicationIdle);
            }
            if (Directory.Exists(mockFileFolder) == false)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(Application.Current.MainWindow, $"MockFileFolder '{mockFileFolder}' does not exist.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }, DispatcherPriority.ApplicationIdle);
            }
            return mockFileFolder!;
        }

        private string AssignBindingPort()
        {
            var bindingPortNumber = App.Config["BindingPort"];
            if (string.IsNullOrEmpty(bindingPortNumber))
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("BindingPort is not configured properly in appsettings.json.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }, DispatcherPriority.ApplicationIdle);
            }
            return bindingPortNumber!;
        }

        private Dictionary<string, Dictionary<string, List<string>>> LoadMatchingConfig(string configFile)
        {
            if (File.Exists(configFile))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(File.ReadAllText(configFile)) ?? [];
            }
            else
            {
                return [];
            }
        }

        private void SyncMockLookup()
        {
            _mockRepository.BuildRepository(RootNodes.Select(n => n.Tag as MockFileNode));
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

        private async void LoadDevLog()
        {
            var filePath = _fileDialogService.OpenFile("Dev Log Files|*.txt;*.log");
            if (string.IsNullOrEmpty(filePath)) return;

            IsBusy = true;
            var mockTreeNode = await Task.Run(() =>
            {
                var mockTreeNode = new MockTreeNode(new DevLogParser().Parse(filePath), NodeTypes.LogFile);
                foreach (var node in mockTreeNode.Children)
                {
                    var mock = node.Tag as MockNode;
                    if (!_mockRepository.IsMockExist(mock.Url, mock.MethodName))
                    {
                        node.IsNew = true;
                    }
                }
                return mockTreeNode;
            });
            RootNodes.Insert(0, mockTreeNode);
            IsBusy = false;

            SyncMockLookup();
        }

        private void NewMockFile(object? parameter)
        {
            var viewModel = new NewMockFileViewModel();
            var newMockFileWindow = new NewMockFileWindow() { DataContext = viewModel, Owner = Application.Current.MainWindow };

            if (newMockFileWindow.ShowDialog() == true)
            {
                var fileName = Path.Combine(_mockFileFolder, viewModel.MockFileName);
                if (File.Exists(fileName))
                {
                    MessageBox.Show("The mock file already exists.");
                    return;
                }
                MockTreeNode node = new MockTreeNode(new MockFileNode(fileName), NodeTypes.MockFile);
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

            var mockTreeNode = new MockTreeNode(new MockFileNode(filePath)
            {
                Nodes = ParseXML(filePath)
            }, NodeTypes.MockFile);
            RootNodes.Insert(0, mockTreeNode);
            SyncMockLookup();
        }

        private void StartWebServer()
        {
            tokenSource = new CancellationTokenSource();
            tokenSource.Token.Register(() =>
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
            });

            Task.Run(async () =>
            {
                _listener.Start();
                AppendOutput($"Mock service started.\n");
                Application.Current.Dispatcher.Invoke(() => _isServiceRunning = true);

                while (!tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
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
                foreach (var file in Directory.GetFiles(_mockConfigFolder, "*.*", SearchOption.AllDirectories))
                {
                    CopyIfMockFileChanged(file);
                }
            });
        }

        private void StopWebServer()
        {
            AppendOutput($"Mock service stopped.{Environment.NewLine}");
            Application.Current.Dispatcher.Invoke(() => _isServiceRunning = false);
            tokenSource?.Cancel();
        }

        private void OnSaveLog(object? parameter)
        {
            string? logFilePath = _fileDialogService.SaveFile("XML Files (*.xml)|*.xml", "MockServiceLog.xml");
            if (string.IsNullOrEmpty(logFilePath)) return;

            ObservableCollection<MockNode> nodes = new ObservableCollection<MockNode>();
            var mockFileNode = new MockFileNode(logFilePath) { Nodes = new List<MockNode>() };
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
            if (file.StartsWith(_appRootFolder, StringComparison.OrdinalIgnoreCase))
            {
                configFile = file;
                mockFile = file.Replace(_appRootFolder, _mockConfigFolder, StringComparison.OrdinalIgnoreCase);
            }
            else if (file.StartsWith(_mockConfigFolder))
            {
                mockFile = file;
                configFile = file.Replace(_mockConfigFolder, _appRootFolder, StringComparison.OrdinalIgnoreCase);
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
                        AddRequestResponsePair(mock, context, serviceType, method, requestContent, DateTime.Now.Subtract(timestamp).TotalMilliseconds);
                    }
                    else
                    {
                        SendResponseContent("", HttpStatusCode.NotFound, context, response);
                        AddRequestResponsePair(null, context, serviceType, method, requestContent, DateTime.Now.Subtract(timestamp).TotalMilliseconds);
                    }
                }
                catch (Exception e)
                {
                    SendResponseContent("", HttpStatusCode.InternalServerError, context, response);

                    // Optionally log the exception as a request/response pair
                    AddRequestResponsePair(null, context, serviceType, method, requestContent, DateTime.Now.Subtract(timestamp).TotalMilliseconds, e);
                }
            }
        }

        private (MockNode?, string, string) GetMock(HttpListenerContext context)
        {
            MockNode? mock = null;
            string requestContent = ReadRequest(context);
            string method = context.Request.HttpMethod.ToString();
            string urlLower = context.Request.Url.PathAndQuery.ToLower();
            if (context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) || context.Request.ContentType.StartsWith("application/json"))
            {
                // REST request
                method = context.Request.HttpMethod.ToString();
                mock = _mockRepository.GetMock(ServiceType.REST, urlLower, method, requestContent);
            }
            else if (context.Request.ContentType.StartsWith("text/xml"))
            {
                // SOAP request
                method = GetSoapAction(requestContent);
                mock = _mockRepository.GetMock(ServiceType.SOAP, urlLower, method, requestContent);
            }
            else
            {
                MessageBox.Show($"Unknown content type {context.Request.ContentType}");
            }
            return (mock, method, requestContent);
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
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Flush();
        }

        private void SendMockResponse(MockNode mock, HttpListenerContext context, HttpListenerResponse response)
        {
            if (mock != null && mock.Response != null)
            {
                if (mock.Response.Delay > 0)
                {
                    AppendOutput("Sleeping for " + mock.Response?.Delay + " seconds...");
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
                using (var writer = new StreamWriter(mockFileNode.MockFile.Replace(".txt", ".xml")))
                {
                    serializer.Serialize(writer, mockFileNode.Nodes);
                }
                MessageBox.Show($"Mocks saved to {mockFileNode.MockFile}.", "Saved", MessageBoxButton.OK);
            }
            foreach(var child in node.Children)
            {
                child.IsDirty = false;
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
                    try
                    {
                        var viewModel = editor.DataContext as MockNodeEditorViewModel;
                        var newMockNode = new MockNode()
                        {
                            ServiceType = viewModel.ServiceType,
                            MethodName = viewModel.MethodName.Trim(),
                            Url = viewModel.Url.Trim(),
                            Description = viewModel.Description,
                            Request = new Request
                            {
                                RequestBody = new Body
                                {
                                    Content = viewModel.RequestBody == null ? string.Empty : viewModel.RequestBody.Trim()
                                }
                            },
                            Response = new Response
                            {
                                ResponseBody = new Body
                                {
                                    Content = viewModel.ResponseBody == null ? string.Empty : viewModel.ResponseBody.Trim()
                                },
                                StatusCode = (HttpStatusCode)viewModel.SelectedStatusCodeOption.Code,
                                Delay = int.TryParse(viewModel.ResponseDelay, out int delay) ? delay : 0
                            }
                        };
                        mockFileNode.Nodes.Add(newMockNode);
                        node.Children.Add(new MockTreeNode(newMockNode) { Parent = node, IsDirty = true });
                        node.IsDirty = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to add mock node: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
                node.Children.Add(new MockTreeNode(mockNode) { Parent = node, IsDirty = true });
                node.IsDirty = true;
                SyncMockLookup();
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
                    try
                    {
                        bool urlChanged = !mockNode.Url.Equals(viewModel.Url, StringComparison.OrdinalIgnoreCase);
                        mockNode.ServiceType = viewModel.ServiceType;
                        if (!mockNode.MethodName.Equals(viewModel.MethodName, StringComparison.OrdinalIgnoreCase))
                        {
                            mockNode.MethodName = viewModel.MethodName.Trim();
                            node.OnMockNodePropertyChanged(this, new PropertyChangedEventArgs(nameof(MockNode.MethodName)));
                        }
                        if (urlChanged)
                        {
                            mockNode.Url = viewModel.Url.Trim();
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
                        if ((int)mockNode.Response.StatusCode != viewModel.SelectedStatusCodeOption?.Code)
                        {
                            mockNode.Response.StatusCode = (HttpStatusCode)viewModel.SelectedStatusCodeOption.Code;
                            node.OnMockNodePropertyChanged(this, new PropertyChangedEventArgs(nameof(Response.StatusCode)));
                        }
                        mockNode.Description = viewModel.Description;
                        node.IsDirty = true;
                        if (node.Parent is MockTreeNode mockFileNode)
                        {
                            mockFileNode.IsDirty = true;
                        }
                        if (urlChanged)
                        {
                            SyncMockLookup();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to update mock node: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                var newMockFileNode = new MockFileNode(mockFileNode.MockFile)
                {
                    Nodes = ParseXML(mockFileNode.MockFile)
                };
                RootNodes[index] = new MockTreeNode(newMockFileNode, NodeTypes.MockFile);
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
                            "You have made changes to the mockup. Do you want to discard the changes?",
                            "Confirmation", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                    RootNodes.Remove(node);
                    SyncMockLookup();
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
            _logOutput.Append(message);
            OnPropertyChanged(nameof(LogOutput));
        }

        public void AddRequestResponsePair(MockNode? node, HttpListenerContext context, ServiceType serviceType, string method, string requestContent, double responseTime, Exception? ex = null)
        {
            string requestBody = requestContent;
            string responseBody = node?.Response?.ResponseBody?.Content ?? string.Empty;

            var pair = new RequestResponsePair
            {
                Method = method,
                Url = context.Request.Url.PathAndQuery,
                ServiceType = serviceType,
                ResponseTimeInMs = (long)responseTime,
                Headers = [context.Request.Headers],
                RequestBody = requestContent,
                ResponseBody = responseBody,
                MockNodeSource = node
            };
            if (node == null || node.Response == null)
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
                pair.StatusCode = node.Response.StatusCode;
            }
            pair.CanReplayInQA = serviceType == ServiceType.REST && !string.IsNullOrEmpty(_qaHost) && !string.IsNullOrEmpty(_qaCredential);
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
                if (RootNodes != null && RootNodes.Any(n => n.IsDirty))
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
            if (pair == null) return;
            pair.MockTreeNodeSource = FindTreeNode(pair?.MockNodeSource);
            if (pair.MockTreeNodeSource != null)
            {
                pair.MockTreeNodeSource.IsHovered = true;
                pair.MockTreeNodeSource.UpdateAncestorStates();
            }
        }

        private void OnResponseBodyMouseLeave(RequestResponsePair? pair)
        {
            if (pair == null || pair.MockTreeNodeSource == null) return;

            pair.MockTreeNodeSource.IsHovered = false;
            pair.MockTreeNodeSource.UpdateAncestorStates();
        }
        private void OnResponseBodyDoubleClick(RequestResponsePair? pair)
        {
            if (pair == null || pair.MockTreeNodeSource == null) return;

            pair.MockTreeNodeSource = FindTreeNode(pair?.MockNodeSource);
            if (pair?.MockTreeNodeSource != null)
            {
                OnEditMockNodeAction(pair?.MockTreeNodeSource);
            }
        }

        private void OnReplayInQA(RequestResponsePair? pair)
        {
            if (pair == null) return;
            ReplayInQAViewModel viewModel = new ReplayInQAViewModel(pair, _qaHost, _qaCredential);
            var replayWindow = new ReplayInQAWindow() { DataContext = viewModel, Owner = Application.Current.MainWindow };
            if (replayWindow.ShowDialog() == true)
            {
            }
        }


        private MockTreeNode? FindTreeNode(MockNode? node)
        {
            if (node == null) return null;
            foreach (var root in RootNodes)
            {
                foreach(var child in root.Children)
                {
                    if (child.Tag == node)
                        return child;
                }
            }
            return null;
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