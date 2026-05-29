using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public static class ControlExtensions
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

    private const int WM_SETREDRAW = 0x000B;

    public static void BeginUpdate(this Control control)
    {
        if (control != null && !control.IsDisposed && control.IsHandleCreated)
        {
            SendMessage(control.Handle, WM_SETREDRAW, 0, IntPtr.Zero);
        }
    }

    public static void EndUpdate(this Control control)
    {
        if (control != null && !control.IsDisposed && control.IsHandleCreated)
        {
            SendMessage(control.Handle, WM_SETREDRAW, 1, IntPtr.Zero);
            control.Invalidate(true); // Forces a full redraw of the control and its children
        }
    }
}
