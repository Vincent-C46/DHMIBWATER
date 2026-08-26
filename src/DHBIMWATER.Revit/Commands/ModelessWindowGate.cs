using System.Windows;

namespace DHBIMWATER.Revit.Commands;

internal static class ModelessWindowGate
{
    private static readonly object Sync = new();
    private static Window? _open;

    public static bool IsOpen
    {
        get { lock (Sync) return _open is not null; }
    }

    public static string OpenTitle
    {
        get { lock (Sync) return _open?.Title ?? string.Empty; }
    }

    public static void Open(Window view)
    {
        lock (Sync) _open = view;
    }

    public static void Close(Window view)
    {
        lock (Sync)
        {
            if (ReferenceEquals(_open, view)) _open = null;
        }
    }

    public static void ActivateOpen()
    {
        Window? view;
        lock (Sync) view = _open;
        if (view is null) return;

        view.Dispatcher.InvokeAsync(() =>
        {
            if (view.WindowState == WindowState.Minimized) view.WindowState = WindowState.Normal;
            view.Activate();
        });
    }
}
