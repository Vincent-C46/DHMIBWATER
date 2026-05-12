using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace DHBIMWATER.UI.Behaviors
{
    public static class ListBoxSelectedItemsBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(ListBoxSelectedItemsBehavior),
                new PropertyMetadata(null, OnSelectedItemsChanged));

        public static void SetSelectedItems(DependencyObject element, IList value) =>
            element.SetValue(SelectedItemsProperty, value);

        public static IList GetSelectedItems(DependencyObject element) =>
            (IList)element.GetValue(SelectedItemsProperty);

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBox listBox)
            {
                listBox.SelectionChanged -= OnSelectionChanged;
                if (e.NewValue != null)
                    listBox.SelectionChanged += OnSelectionChanged;
            }
        }

        private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var bound = GetSelectedItems(listBox);
            if (bound == null) return;

            foreach (var item in e.RemovedItems) bound.Remove(item);
            foreach (var item in e.AddedItems) bound.Add(item);
        }
    }
}
