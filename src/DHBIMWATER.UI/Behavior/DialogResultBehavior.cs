using System.Windows;

namespace DHBIMWATER.UI.Behaviors
{
    public static class DialogResultBehavior
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached(
                "DialogResult",
                typeof(bool?),
                typeof(DialogResultBehavior),
                new PropertyMetadata(null, OnDialogResultChanged));

        public static void SetDialogResult(Window target, bool? value) =>
            target.SetValue(DialogResultProperty, value);

        public static bool? GetDialogResult(Window target) =>
            (bool?)target.GetValue(DialogResultProperty);
        private static void OnDialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window)
                return;

            if (e.NewValue is not bool result)
                return;

            try
            {
                window.DialogResult = result;
            }
            catch (InvalidOperationException)
            {
                window.Close();
            }
        }

    }
}
