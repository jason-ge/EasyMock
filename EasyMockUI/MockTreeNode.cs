using EasyMockLib.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;

namespace EasyMock.UI
{
    public enum NodeTypes
    {
        LogFile,
        MockFile,
        MockItem
    }
    public class MockTreeNode : INotifyPropertyChanged
    {
        public ObservableCollection<MockTreeNode> Children { get; set; } = [];
        public MockTreeNode? Parent { get; set; }
        public object? Tag { get; set; }
        public string Header
        {
            get
            {
                if (Tag is MockFileNode fileNode)
                {
                    return Path.GetFileNameWithoutExtension(fileNode.MockFile);
                }
                else if (Tag is MockNode mockNode)
                {
                    return $"{mockNode.Url} - {mockNode.MethodName}";
                }
                return "";
            }
        }

        public MockTreeNode(MockFileNode fileNode)
        {
            NodeType = NodeTypes.MockFile;
            Tag = fileNode;

            foreach (MockNode node in fileNode.Nodes)
            {
                var child = new MockTreeNode(node) { Parent = this };
                this.Children.Add(child);
            }
        }

        public MockTreeNode(MockNode node)
        {
            NodeType = NodeTypes.MockItem;
            Tag = node;
        }

        public Visibility MockNodeMenuItemVisibility
        {
            get
            {
                return NodeType == NodeTypes.MockItem ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility MockFileMenuItemVisibility
        {
            get
            {
                return NodeType == NodeTypes.MockFile ? Visibility.Visible: Visibility.Collapsed;
            }
        }

        public Visibility SaveMenuItemVisibility
        {
            get
            {
                return IsDirty && NodeType != NodeTypes.MockItem ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged(nameof(IsDirty));
                    OnPropertyChanged(nameof(SaveMenuItemVisibility));
                }
            }
        }

        public NodeTypes NodeType { get; set; }

        public bool IsErrorStatusCode
        {
            get
            {
                if (Tag is MockNode mockNode && mockNode.Response != null)
                    return mockNode.Response.StatusCode != HttpStatusCode.OK;
                else return false;
            }
        }

        private bool _isHovered;
        public bool IsHovered
        {
            get => _isHovered;
            set
            {
                if (_isHovered != value)
                {
                    _isHovered = value;
                    OnPropertyChanged(nameof(IsHovered));
                }
            }
        }

        private bool _isDescendant;
        public bool IsDescendant
        {
            get => _isDescendant;
            set
            {
                if (_isDescendant != value)
                {
                    _isDescendant = value;
                    OnPropertyChanged(nameof(IsDescendant));
                }
            }
        }

        public void UpdateAncestorStates()
        {
            MockTreeNode? node = this.Parent as MockTreeNode;
            while (node != null)
            {
                node.IsHovered = this.IsHovered;
                node = node.Parent as MockTreeNode;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void OnMockNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MockNode.Url) || e.PropertyName == nameof(MockNode.MethodName))
            {
                OnPropertyChanged(nameof(Header));
            }
            if (e.PropertyName == nameof(Response.StatusCode))
            {
                OnPropertyChanged(nameof(IsErrorStatusCode));
            }
        }
    }
}
