using System;
using System.Configuration;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using EasyMockLib;
using EasyMockLib.Models;
using Newtonsoft;
using Newtonsoft.Json;

namespace EasyMock
{
    public partial class EasyMockForm : Form
    {
        /// <summary>
        /// This is the heart of the web server
        /// </summary>
        private readonly HttpListener listener = new HttpListener { Prefixes = { $"http://localhost:{ConfigurationManager.AppSettings["BindingPort"]}/" } };
        private readonly int SERVICE_TIMEOUT_IN_SECONDS = int.Parse(ConfigurationManager.AppSettings["ServiceTimeoutInSeconds"]);
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private const string ToolStripItem_Remove = "toolStripRemove";
        private const string ToolStripItem_Refresh = "toolStripRefresh";
        private const string ToolStripItem_Save = "toolStripSave";
        private const string ToolStripItem_SimulateException = "toolStripSimulateException";
        private Image ImageCheck = Image.FromFile("checkmark.png");
        private Dictionary<string, Dictionary<string, List<string>>> soapMatchingConfig;

        private CancellationTokenSource tokenSource;
        public EasyMockForm()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            imageList.Images.Add(Image.FromFile("checkmark.png"));
            if (!HttpListener.IsSupported)
            {
                MessageBox.Show("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            soapMatchingConfig = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, List<string>>>>(File.ReadAllText("SoapServiceMatchingElements.json"));

            var parser = new MockFileParser();
            foreach (var file in Directory.EnumerateFiles(ConfigurationManager.AppSettings["MockFileFolder"], "*.txt"))
            {
                var mock = parser.Parse(file);
                mockTreeView.Nodes.Add(new MockTreeNode(mock));
            }
            btnStopService.Enabled = false;
        }
        private void Form1_FormClosing(object sender, EventArgs e)
        {
            StopWebServer();
        }
        /// <summary>
        /// Call this to start the web server
        /// </summary>
        public void StartWebServer()
        {
            tokenSource = new CancellationTokenSource();
            tokenSource.Token.Register(() =>
            {
                if (listener.IsListening)
                {
                    listener.Stop();
                }
            });
            Thread thread1 = new Thread(async () =>
            {
                listener.Start();
                AppendOutput($"Mock service started.{Environment.NewLine}");
                while (!tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        //GetContextAsync() returns when a new request come in
                        var context = await listener.GetContextAsync();
                        Task.Run(() => ProcessRequest(context), tokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        if (e is HttpListenerException)
                        {
                            //this gets thrown when the listener is stopped
                            return;
                        }
                        logger.Error(e);
                    }
                }
            });
            thread1.Start();
        }
        /// <summary>
        /// Call this to stop the web server. It will not kill any requests currently being processed.
        /// </summary>
        public void StopWebServer()
        {
            AppendOutput($"Mock service stopped.{Environment.NewLine}");
            tokenSource?.Cancel();
        }
        /// <summary>
        /// Handle an incoming request
        /// </summary>
        /// <param name="context">The context of the incoming request</param>
        private void ProcessRequest(HttpListenerContext context)
        {
            using (var response = context.Response)
            {
                try
                {
                    //var handled = false;
                    (var mock, var method, var requestContent) = GetMock(context);
                    if (mock != null)
                    {
                        //AppendOutput($"From file {mock.Parent.Text}{Environment.NewLine}");
                        if (mock.Request.RequestType == ServiceType.REST)
                        {
                            AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fff")} {method} {context.Request.Url.AbsoluteUri} Request{Environment.NewLine}");
                            AppendOutput($"RequestBody: {requestContent}");
                            AppendOutput(Environment.NewLine);
                        }
                        else
                        {
                            AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fff")} {context.Request.Url.AbsoluteUri} {method} Request{Environment.NewLine}");
                            AppendOutput($"{requestContent}");
                            AppendOutput(Environment.NewLine);
                        }
                        AppendOutput(Environment.NewLine);
                        if (!string.IsNullOrEmpty(mock.SimulateException))
                        {
                            if (mock.SimulateException == this.toolStripInternalServerError.Text)
                            {
                                OutputResponseContent("", HttpStatusCode.InternalServerError, context, response);
                            }
                            else if (mock.SimulateException == this.toolStripNotFound.Text)
                            {
                                OutputResponseContent("", HttpStatusCode.NotFound, context, response);
                            }
                            else if (mock.SimulateException == this.toolStripTimeOut.Text)
                            {
                                Thread.Sleep(SERVICE_TIMEOUT_IN_SECONDS * 1000);
                                return;
                            }
                        }
                        else if (mock.Response == null)
                        {
                            // No response received, do not send back anything to simulate timeout scenario
                            Thread.Sleep(SERVICE_TIMEOUT_IN_SECONDS * 1000);
                            return;
                        }
                        else if (mock.Response.StatusCode == HttpStatusCode.OK)
                        {
                            OutputMockResponse(mock, context, response);
                            if (mock.Request.RequestType == ServiceType.REST)
                            {
                                AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fff")} {method} {context.Request.Url.AbsoluteUri} Response{Environment.NewLine}");
                                AppendOutput($"ResponseBody: {mock.Response.ResponseBody.Content}");
                            }
                            else
                            {
                                AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fff")} {context.Request.Url.AbsoluteUri} {method} Response{Environment.NewLine}");
                                AppendOutput($"{mock.Response.ResponseBody.Content}");
                            }
                            AppendOutput(Environment.NewLine + Environment.NewLine);
                        }
                        else
                        {
                            if (mock.Response.ResponseBody != null)
                            {
                                OutputMockResponse(mock, context, response);
                            }
                            if (mock.Request.RequestType == ServiceType.REST)
                            {
                                AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fff")} {method} {context.Request.Url.AbsoluteUri} Response{Environment.NewLine}");
                                AppendOutput($"StatusCode: {mock.Response.StatusCode}");
                            }
                            else
                            {
                                AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fff")} {context.Request.Url.AbsoluteUri} {method} Response{Environment.NewLine}");
                                AppendOutput($"StatusCode: {mock.Response.StatusCode}");
                            }
                            AppendOutput(Environment.NewLine + Environment.NewLine);
                        }
                    }
                    else
                    {
                        AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fff")} {context.Request.Url.AbsoluteUri} {method} Request{Environment.NewLine}");
                        AppendOutput($"{requestContent}");
                        AppendOutput(Environment.NewLine + Environment.NewLine);
                        AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fff")} {context.Request.Url.AbsoluteUri} {method} Response{Environment.NewLine}");
                        AppendOutput($"StatusCode: {HttpStatusCode.NotFound}");
                        AppendOutput(Environment.NewLine + Environment.NewLine);
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                }
                catch (Exception e)
                {
                    response.StatusCode = 500;
                    response.ContentType = "application/json";
                    var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(e));
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
            }
        }
        private void OutputResponseContent(string content, HttpStatusCode status, HttpListenerContext context, HttpListenerResponse response)
        {
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
        private void OutputMockResponse(MockNode mock, HttpListenerContext context, HttpListenerResponse response)
        {
            OutputResponseContent(mock.Response.ResponseBody.Content, mock.Response.StatusCode, context, response);
        }
        private void AppendOutput(string output)
        {
            txtOutput.BeginInvoke(new Action(() => txtOutput.AppendText(output)));
        }
        private (MockNode, string, string) GetMock(HttpListenerContext context)
        {
            MockNode mock = null;
            string requestContent = ReadRequest(context);
            string method = context.Request.HttpMethod.ToString();
            if (context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) || context.Request.ContentType.StartsWith("application/json"))
            {
                // REST request
                method = context.Request.HttpMethod.ToString();
                string url = context.Request.HttpMethod == HttpMethod.Get.ToString() ? context.Request.Url.PathAndQuery : context.Request.Url.AbsolutePath.Substring(context.Request.Url.AbsolutePath.LastIndexOf('/') + 1);
                mock = GetMock(ServiceType.REST, url, method, requestContent);
            }
            else if (context.Request.ContentType.StartsWith("text/xml"))
            {
                // SOAP request
                method = GetSoapAction(requestContent);
                mock = GetMock(ServiceType.SOAP, context.Request.Url.AbsolutePath.Substring(context.Request.Url.AbsolutePath.LastIndexOf('/') + 1), method, requestContent);
            }
            else
            {
                MessageBox.Show($"Unknow content type {context.Request.ContentType}");
            }
            return (mock, method, requestContent);
        }
        private MockNode GetMock(ServiceType serviceType, string service, string method, string requestContent)
        {
            foreach (MockTreeNode node in mockTreeView.Nodes)
            {

                var mock = (node.Tag as MockFileNode)?.GetMock(serviceType, service, method, requestContent, soapMatchingConfig);
                if (mock != null)
                {
                    return mock;
                }
            }
            return null;
        }
        private static string GetSoapAction(string soapEnv)
        {
            XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
            XDocument doc = XDocument.Parse(soapEnv);
            return doc.Descendants(soap + "Body").First().Elements().First().Name.LocalName.Replace("Request", "");
        }

        private static string ReadRequest(HttpListenerContext context)
        {
            using (var body = context.Request.InputStream)
            using (var reader = new StreamReader(body, context.Request.ContentEncoding))
            {
                //Get the data that was sent to us
                return reader.ReadToEnd();
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopWebServer();
            tokenSource?.Dispose();
        }
        private void btnLoadMockFile_Click(object sender, EventArgs e)
        {
            if (dlgOpenMockFile.ShowDialog() == DialogResult.OK)
            {
                var parser = new MockFileParser();
                var qaMocks = parser.Parse(dlgOpenMockFile.FileName);
                mockTreeView.Nodes.Insert(0, new MockTreeNode(qaMocks));
            }
        }
        private void btnStartService_Click(object sender, EventArgs e)
        {
            StartWebServer();
            btnLoadMockFile.Enabled = false;
            btnStartService.Enabled = false;
            btnStopService.Enabled = true;
        }
        private void btnStopService_Click(object sender, EventArgs e)
        {
            StopWebServer();
            btnLoadMockFile.Enabled = true;
            btnStartService.Enabled = true;
            btnStopService.Enabled = false;
        }
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtOutput.Text = "";
        }
        private void mockTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var treeNode = e.Node as MockTreeNode;
            if (treeNode == null) return;
            if (e.Button == MouseButtons.Left)
            {
                if (treeNode?.NodeType == NodeTypes.MockItem)
                {
                    MockNode node = treeNode.Tag as MockNode;
                    if (node?.Request.RequestType == ServiceType.SOAP)
                    {
                        AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fffffff")} {node.Request.Url} {node.Request.ServiceName} Request\r\n{node.Request.RequestBody.Content}\r\n\r\n");
                        AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fffffff")} {node.Request.Url} {node.Request.ServiceName} Response\r\n");
                    }
                    else
                    {
                        AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fffffff")} {node.Request.MethodName} {node.Request.Url} Request\r\n{node.Request.RequestBody.Content}\r\n\r\n");
                        AppendOutput($"{DateTime.Now.ToString("HH:mm:ss.fffffff")} {node.Request.Url} {node.Request.ServiceName} Response\r\n");
                    }
                    if (node.Response != null)
                    {
                        if (node.Response.StatusCode != HttpStatusCode.OK)
                        {
                            AppendOutput(node.Response.StatusCode.ToString() + "\r\n\r\n");
                            if (node.Response.ResponseBody.Content != null)
                            {
                                AppendOutput(node.Response.ResponseBody.Content + "\r\n\r\n");
                            }
                        }
                        else
                        {
                            AppendOutput(node.Response.ResponseBody.Content + "\r\n\r\n");
                        }
                    }
                }
            }
            else
            {
                mockTreeView.SelectedNode = treeNode;
                // Convert from Tree coordinates to Screen coordinates 
                Point ScreenPoint = mockTreeView.PointToScreen(new Point(e.X, e.Y));
                // Convert from Screen coordinates to Form coordinates 
                Point FormPoint = this.PointToClient(ScreenPoint);
                if (treeNode.NodeType == NodeTypes.MockFile)
                {
                    // Root node
                    foreach (ToolStripItem item in contextMockNodeMenuStrip.Items)
                    {
                        if (item.Name == ToolStripItem_Save)
                        {
                            item.Enabled = treeNode.IsDirty;
                        }
                        else if (item.Name == ToolStripItem_SimulateException)
                        {
                            item.Enabled = false;
                        }
                    }
                }
                else
                {
                    MockNode node = treeNode.Tag as MockNode;
                    foreach (ToolStripMenuItem item in contextMockNodeMenuStrip.Items)
                    {
                        item.Enabled = false;
                        if (item.Name == ToolStripItem_SimulateException)
                        {
                            item.Enabled = true;
                            this.toolStripSimulateException.DropDownItems.Clear();
                            toolStripInternalServerError.Checked = node.SimulateException == toolStripInternalServerError.Text;
                            this.toolStripSimulateException.DropDownItems.Add(this.toolStripInternalServerError);
                            toolStripNotFound.Checked = node.SimulateException == toolStripNotFound.Text;
                            this.toolStripSimulateException.DropDownItems.Add(this.toolStripNotFound);
                            toolStripTimeOut.Checked = node.SimulateException == toolStripTimeOut.Text;
                            this.toolStripSimulateException.DropDownItems.Add(this.toolStripTimeOut);
                        }
                        else if (item.Name == ToolStripItem_Remove)
                        {
                            item.Enabled = true;
                        }
                    }
                }
                contextMockNodeMenuStrip.Show(this, FormPoint);
            }
        }
        #region Tree node drag and drop
        private void mockTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
        }
        // Set the target drop effect to the effect 
        // specified in the ItemDrag event handler.
        private void mockTreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }
        // Select the node under the mouse pointer to indicate the 
        // expected drop location.
        private void mockTreeView_DragOver(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse position.
            Point targetPoint = mockTreeView.PointToClient(new Point(e.X, e.Y));
            // Select the node at the mouse position.
            mockTreeView.SelectedNode = mockTreeView.GetNodeAt(targetPoint);
        }
        private void mockTreeView_DragDrop(object sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the drop location.
            Point targetPoint = mockTreeView.PointToClient(new Point(e.X, e.Y));
            // Retrieve the node at the drop location.
            MockTreeNode targetNode = (MockTreeNode)mockTreeView.GetNodeAt(targetPoint);
            // Retrieve the node that was dragged.
            MockTreeNode draggedNode = (MockTreeNode)e.Data.GetData(typeof(MockTreeNode));
            // Confirm that the node at the drop location is not 
            // the dragged node or a descendant of the dragged node.
            if (!draggedNode.Equals(targetNode))
            {
                // If it is a move operation, remove the node from its current 
                // location and add it to the node at the drop location.
                if (e.Effect == DragDropEffects.Move)
                {
                    (draggedNode.Parent as MockTreeNode).IsDirty = true;
                    draggedNode.Remove();
                    var root = (targetNode.Level == 0 ? targetNode : targetNode.Parent) as MockTreeNode;
                    root.IsDirty = true;
                    root.Nodes.Insert(targetNode.Index, draggedNode);
                }
                // If it is a copy operation, clone the dragged node 
                // and add it to the node at the drop location.
                else if (e.Effect == DragDropEffects.Copy)
                {
                    var node = draggedNode.Clone() as MockTreeNode;
                    var root = (targetNode.Level == 0 ? targetNode : targetNode.Parent) as MockTreeNode;
                    root.IsDirty = true;
                    root.Nodes.Insert(targetNode.Index, node);
                }
                // Expand the node at the location 
                // to show the dropped node.
                targetNode.Expand();
            }
        }
        #endregion
        private void SaveMock(MockTreeNode root)
        {
            using (StreamWriter writer = new StreamWriter((root.Tag as MockFileNode).MockFile))
            {
                foreach (MockTreeNode treeNode in root.Nodes)
                {
                    var node = treeNode.Tag as MockNode;
                    if (node?.Request.RequestType == ServiceType.REST)
                    {
                        writer.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")} {node.Request.MethodName} {node.Request.Url} Request");
                        writer.WriteLine($"RequestBody: {node.Request.RequestBody.Content}");
                        writer.WriteLine();
                        writer.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")} {node.Request.MethodName} {node.Request.Url} Response");
                        if (node.Response.StatusCode == HttpStatusCode.OK)
                        {
                            writer.WriteLine($"ResponseBody: {node.Response.ResponseBody.Content}");
                        }
                        else
                        {
                            writer.WriteLine($"StatusCode:{node.Response.StatusCode}");
                        }
                        writer.WriteLine();
                    }
                    else
                    {
                        writer.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")} {node.Request.Url} {node.Request.MethodName} Request");
                        if (!node.Request.MethodName.Equals(HttpMethod.Get.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteLine(node.Request.RequestBody.Content);
                        }
                        writer.WriteLine();
                        writer.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fffffff")} {node.Request.Url} {node.Request.MethodName} Response");
                        writer.WriteLine(node.Response.ResponseBody.Content);
                        writer.WriteLine();
                    }
                }
            }
        }

        private void toolstripSimulateException_ItemClicked(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            item.Checked = !item.Checked;
            if (item.Checked)
            {
                (((MockTreeNode)mockTreeView.SelectedNode).Tag as MockNode).SimulateException = item.Text;
                mockTreeView.SelectedNode.ForeColor = Color.Red;
                mockTreeView.SelectedNode.NodeFont = new Font(mockTreeView.Font, FontStyle.Bold);
                mockTreeView.SelectedNode.Text = mockTreeView.SelectedNode.Text;
            }
            else
            {
                (((MockTreeNode)mockTreeView.SelectedNode).Tag as MockNode).SimulateException = "";
                mockTreeView.SelectedNode.ForeColor = Color.Black;
                mockTreeView.SelectedNode.NodeFont = new Font(mockTreeView.Font, FontStyle.Regular); ;
            }
        }
        private void contextMockNodeMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            ToolStripItem item = e.ClickedItem;
            if (mockTreeView.SelectedNode == null)
            {
                return;
            }
            if (item.Name == ToolStripItem_Remove)
            {
                var node = (MockTreeNode)mockTreeView.SelectedNode;
                if (node.NodeType == NodeTypes.MockFile)
                {
                    if (node.IsDirty)
                    {
                        DialogResult dialogResult = MessageBox.Show("You have made changes to the mockup. Do you want to save it before remove it?", "Confirmation", MessageBoxButtons.YesNo);
                        if (dialogResult == DialogResult.Yes)
                        {
                            SaveMock(node);
                            node.IsDirty = false;
                            MessageBox.Show("Mock saved.", "Saved", MessageBoxButtons.OK);
                        }
                    }
                }
                else
                {
                    var root = (MockTreeNode)mockTreeView.SelectedNode;
                    while (root.Tag as MockFileNode == null)
                    {
                        root = (MockTreeNode)root.Parent;
                    }
                    root.IsDirty = true;
                }
                mockTreeView.Nodes.Remove(mockTreeView.SelectedNode);
            }
            else if (item.Name == ToolStripItem_Refresh)
            {
                int index = mockTreeView.SelectedNode.Index;
                var parser = new MockFileParser();
                var mocks = parser.Parse(((mockTreeView.SelectedNode as MockTreeNode).Tag as MockFileNode).MockFile);
                mockTreeView.Nodes.Remove(mockTreeView.SelectedNode);
                mockTreeView.Nodes.Insert(index, new MockTreeNode(mocks));
            }
            else if (item.Name == ToolStripItem_Save)
            {
                SaveMock((MockTreeNode)mockTreeView.SelectedNode);
            }
        }
    }
}