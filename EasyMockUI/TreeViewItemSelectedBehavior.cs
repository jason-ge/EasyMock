using System.Windows;
using System.Windows.Controls;

namespace EasyMock.UI
{
    public static class TreeViewItemSelectedBehavior
    {
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.RegisterAttached(
                "IsSelected",
                typeof(bool),
                typeof(TreeViewItemSelectedBehavior),
                new PropertyMetadata(false, OnTreeViewItemSelected));

        public static bool GetIsSelected(DependencyObject obj) => (bool)obj.GetValue(IsSelectedProperty);
        public static void SetIsSelected(DependencyObject obj, bool value) => obj.SetValue(IsSelectedProperty, value);

        private static void OnTreeViewItemSelected(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeViewItem treeViewItem)
            {
                if (e.NewValue is bool isSelected && isSelected)
                {
                    treeViewItem.IsSelected = true;
                }
                else
                {
                    treeViewItem.IsSelected = false;
                }
            }
        }
    }
}