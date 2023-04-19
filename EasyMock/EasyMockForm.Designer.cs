namespace EasyMock
{
    partial class EasyMockForm
    {
        /// <summary>
         /// Required designer variable.
         /// </summary>
        private System.ComponentModel.IContainer components = null;
        /// <summary>
         /// Clean up any resources being used.
         /// </summary>
         /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        #region Windows Form Designer generated code
        /// <summary>
         /// Required method for Designer support - do not modify
         /// the contents of this method with the code editor.
         /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            txtOutput = new TextBox();
            btnLoadMockFile = new Button();
            btnStartService = new Button();
            btnStopService = new Button();
            btnClearLog = new Button();
            splitContainer = new SplitContainer();
            mockTreeView = new TreeView();
            tableLayoutPanel1 = new TableLayoutPanel();
            flowLayoutPanel1 = new FlowLayoutPanel();
            dlgOpenMockFile = new OpenFileDialog();
            saveFileDialog1 = new SaveFileDialog();
            contextMockNodeMenuStrip = new ContextMenuStrip(components);
            toolStripRemove = new ToolStripMenuItem();
            toolStripRefresh = new ToolStripMenuItem();
            toolStripSave = new ToolStripMenuItem();
            toolStripSimulateException = new ToolStripMenuItem();
            toolStripNotFound = new ToolStripMenuItem();
            toolStripTimeOut = new ToolStripMenuItem();
            toolStripInternalServerError = new ToolStripMenuItem();
            imageList = new ImageList(components);
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            contextMockNodeMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // txtOutput
            // 
            txtOutput.Dock = DockStyle.Fill;
            txtOutput.Location = new Point(4, 114);
            txtOutput.Margin = new Padding(4, 6, 4, 6);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ScrollBars = ScrollBars.Vertical;
            txtOutput.Size = new Size(1351, 1164);
            txtOutput.TabIndex = 0;
            // 
            // btnLoadMockFile
            // 
            btnLoadMockFile.Location = new Point(423, 6);
            btnLoadMockFile.Margin = new Padding(4, 6, 4, 6);
            btnLoadMockFile.Name = "btnLoadMockFile";
            btnLoadMockFile.Size = new Size(252, 68);
            btnLoadMockFile.TabIndex = 1;
            btnLoadMockFile.Text = "Load Mock File";
            btnLoadMockFile.UseVisualStyleBackColor = true;
            btnLoadMockFile.Click += btnLoadMockFile_Click;
            // 
            // btnStartService
            // 
            btnStartService.Location = new Point(12, 6);
            btnStartService.Margin = new Padding(4, 6, 4, 6);
            btnStartService.Name = "btnStartService";
            btnStartService.Size = new Size(207, 68);
            btnStartService.TabIndex = 2;
            btnStartService.Text = "Start Service";
            btnStartService.UseVisualStyleBackColor = true;
            btnStartService.Click += btnStartService_Click;
            // 
            // btnStopService
            // 
            btnStopService.Location = new Point(227, 6);
            btnStopService.Margin = new Padding(4, 6, 4, 6);
            btnStopService.Name = "btnStopService";
            btnStopService.Size = new Size(188, 68);
            btnStopService.TabIndex = 3;
            btnStopService.Text = "Stop Service";
            btnStopService.UseVisualStyleBackColor = true;
            btnStopService.Click += btnStopService_Click;
            // 
            // btnClearLog
            // 
            btnClearLog.Location = new Point(683, 6);
            btnClearLog.Margin = new Padding(4, 6, 4, 6);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Size = new Size(188, 68);
            btnClearLog.TabIndex = 3;
            btnClearLog.Text = "Clear Log";
            btnClearLog.UseVisualStyleBackColor = true;
            btnClearLog.Click += btnClearLog_Click;
            // 
            // splitContainer
            // 
            splitContainer.BorderStyle = BorderStyle.Fixed3D;
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 0);
            splitContainer.Margin = new Padding(4, 6, 4, 6);
            splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(mockTreeView);
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(tableLayoutPanel1);
            splitContainer.Size = new Size(1893, 1288);
            splitContainer.SplitterDistance = 522;
            splitContainer.SplitterWidth = 8;
            splitContainer.TabIndex = 4;
            // 
            // mockTreeView
            // 
            mockTreeView.AllowDrop = true;
            mockTreeView.Dock = DockStyle.Fill;
            mockTreeView.Location = new Point(0, 0);
            mockTreeView.Margin = new Padding(4, 6, 4, 6);
            mockTreeView.Name = "mockTreeView";
            mockTreeView.Size = new Size(518, 1284);
            mockTreeView.TabIndex = 0;
            mockTreeView.ItemDrag += mockTreeView_ItemDrag;
            mockTreeView.NodeMouseClick += mockTreeView_NodeMouseClick;
            mockTreeView.DragDrop += mockTreeView_DragDrop;
            mockTreeView.DragEnter += mockTreeView_DragEnter;
            mockTreeView.DragOver += mockTreeView_DragOver;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 0);
            tableLayoutPanel1.Controls.Add(txtOutput, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4, 6, 4, 6);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1359, 1284);
            tableLayoutPanel1.TabIndex = 5;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(btnStartService);
            flowLayoutPanel1.Controls.Add(btnStopService);
            flowLayoutPanel1.Controls.Add(btnLoadMockFile);
            flowLayoutPanel1.Controls.Add(btnClearLog);
            flowLayoutPanel1.Location = new Point(4, 6);
            flowLayoutPanel1.Margin = new Padding(4, 6, 4, 6);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Padding = new Padding(8, 0, 8, 0);
            flowLayoutPanel1.Size = new Size(1344, 96);
            flowLayoutPanel1.TabIndex = 4;
            // 
            // contextMockNodeMenuStrip
            // 
            contextMockNodeMenuStrip.ImageScalingSize = new Size(20, 20);
            contextMockNodeMenuStrip.Items.AddRange(new ToolStripItem[] { toolStripRemove, toolStripRefresh, toolStripSave, toolStripSimulateException });
            contextMockNodeMenuStrip.Name = "contextMockNodeMenuStrip";
            contextMockNodeMenuStrip.Size = new Size(263, 148);
            contextMockNodeMenuStrip.ItemClicked += contextMockNodeMenuStrip_ItemClicked;
            // 
            // toolStripRemove
            // 
            toolStripRemove.Name = "toolStripRemove";
            toolStripRemove.Size = new Size(262, 36);
            toolStripRemove.Text = "Remove";
            // 
            // toolStripRefresh
            // 
            toolStripRefresh.Name = "toolStripRefresh";
            toolStripRefresh.Size = new Size(262, 36);
            toolStripRefresh.Text = "Refresh";
            // 
            // toolStripSave
            // 
            toolStripSave.Name = "toolStripSave";
            toolStripSave.Size = new Size(262, 36);
            toolStripSave.Text = "Save";
            // 
            // toolStripSimulateException
            // 
            toolStripSimulateException.Name = "toolStripSimulateException";
            toolStripSimulateException.Size = new Size(262, 36);
            toolStripSimulateException.Text = "Simulate Exception";
            // 
            // toolStripNotFound
            // 
            toolStripNotFound.Name = "toolStripNotFound";
            toolStripNotFound.Size = new Size(178, 26);
            toolStripNotFound.Text = "Not Found";
            toolStripNotFound.Click += toolstripSimulateException_ItemClicked;
            // 
            // toolStripTimeOut
            // 
            toolStripTimeOut.Name = "toolStripTimeOut";
            toolStripTimeOut.Size = new Size(178, 26);
            toolStripTimeOut.Text = "TimeOut";
            toolStripTimeOut.Click += toolstripSimulateException_ItemClicked;
            // 
            // toolStripInternalServerError
            // 
            toolStripInternalServerError.Name = "toolStripInternalServerError";
            toolStripInternalServerError.Size = new Size(178, 26);
            toolStripInternalServerError.Text = "Internal Server Error";
            toolStripInternalServerError.Click += toolstripSimulateException_ItemClicked;
            // 
            // imageList
            // 
            imageList.ColorDepth = ColorDepth.Depth8Bit;
            imageList.ImageSize = new Size(16, 16);
            imageList.TransparentColor = Color.Transparent;
            // 
            // EasyMockForm1
            // 
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1893, 1288);
            Controls.Add(splitContainer);
            Margin = new Padding(4, 6, 4, 6);
            Name = "EasyMockForm1";
            Text = "Easy Mock";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            contextMockNodeMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
        }
        #endregion
        private System.Windows.Forms.TextBox txtOutput;
        private System.Windows.Forms.Button btnLoadMockFile;
        private System.Windows.Forms.Button btnStartService;
        private System.Windows.Forms.Button btnStopService;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TreeView mockTreeView;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.OpenFileDialog dlgOpenMockFile;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ContextMenuStrip contextMockNodeMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem toolStripRemove;
        private System.Windows.Forms.ToolStripMenuItem toolStripRefresh;
        private System.Windows.Forms.ToolStripMenuItem toolStripSave;
        private System.Windows.Forms.ToolStripMenuItem toolStripSimulateException;
        private System.Windows.Forms.ToolStripMenuItem toolStripNotFound;
        private System.Windows.Forms.ToolStripMenuItem toolStripTimeOut;
        private System.Windows.Forms.ToolStripMenuItem toolStripInternalServerError;
        private ImageList imageList;
    }
}