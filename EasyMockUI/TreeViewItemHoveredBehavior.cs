using System.Windows;
using System.Windows.Controls;

namespace EasyMock.UI
{
    public static class TreeViewItemHoveredBehavior
    {
        public static readonly DependencyProperty IsHoveredProperty =
            DependencyProperty.RegisterAttached(
                "IsHovered",
                typeof(bool),
                typeof(TreeViewItemHoveredBehavior),
                new PropertyMetadata(false, OnTreeViewItemHovered));

        public static bool GetIsHovered(DependencyObject obj) => (bool)obj.GetValue(IsHoveredProperty);
        public static void SetIsHovered(DependencyObject obj, bool value) => obj.SetValue(IsHoveredProperty, value);

        private static void OnTreeViewItemHovered(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeViewItem treeViewItem)
            {
                if (e.NewValue is bool isHovered && isHovered)
                {
                    treeViewItem.IsExpanded = true;
                    treeViewItem.BringIntoView();
                }
            }
        }
    }
}