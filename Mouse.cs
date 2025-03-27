using System;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace zuul
{
    public class Mouse
    {
        private NotifyIcon notifyIcon = new NotifyIcon();
        private ContextMenu contextMenu = new ContextMenu();
        private LowLevelHooks hooks = new LowLevelHooks();

        private bool cursorIsHidden = false;
        private int mouseTimeoutSeconds = 3;

        // Windows cursor constants. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setsystemcursor
        private string[] cursors = { "32512", "32513", "32514", "32515", "32516", "32642", "32643", "32644", "32645", "32646", "32648", "32649", "32650" };
        private System.Windows.Forms.Timer CursorResetTimer;

        [DllImport("user32.dll")]
        private static extern bool SetSystemCursor(IntPtr hcur, uint id);

        [DllImport("user32.dll")]
        private static extern IntPtr CreateCursor(IntPtr hInst, int xHotSpot, int yHotSpot, int nWidth, int nHeight, byte[] pvANDPlane, byte[] pvXORPlane);

        [DllImport("user32.dll")]
        private static extern IntPtr CopyImage(IntPtr h, uint type, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursorFromFile(string lpFileName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        private void HideMouseCursor()
        {
            if (cursorIsHidden)
                return;

            Console.WriteLine("HideMouseCursor()");
            cursorIsHidden = true;

            foreach (string c in cursors)
            {
                SetSystemCursor(LoadCursorFromFile(@Path.GetDirectoryName(Application.ExecutablePath) + "\\blank_cursor.cur"), Convert.ToUInt32(c));
            }
            CursorResetTimer.Start();
        }

        public Mouse()
        {
            notifyIcon.Visible = true;
            notifyIcon.Icon = new System.Drawing.Icon(@Path.GetDirectoryName(Application.ExecutablePath) + "\\mouse.ico");
            notifyIcon.ContextMenu = contextMenu;
            MenuItem menuExit = new MenuItem("Exit");
            menuExit.Click += (sender, args) =>
            {
                hooks.Stop();
                ResetCursor();
                Application.ExitThread();
            };
            contextMenu.MenuItems.Add(menuExit);

            // MouseTimer checks to see if there has been an event (default 10 seconds) that requires the mouse to stay hidden, otherwise resets the mouse cursor.
            CursorResetTimer = new System.Windows.Forms.Timer();
            CursorResetTimer.Interval = mouseTimeoutSeconds * 1000;
            CursorResetTimer.Tick += (sender, args) =>
            {
                ResetCursor();            
            };
            hooks.OnKeyWasPressed += HideMouseCursor;
            hooks.OnMouseWasDiddled += ResetCursor;

            ResetCursor();
            hooks.Start();
        }

        ~Mouse()
        {
            ResetCursor();
        }

        private void ResetCursor()
        {
            if (!cursorIsHidden)
                return;
           
            CursorResetTimer.Stop();
            cursorIsHidden = false;
            SystemParametersInfo(0x0057, 0, null, 0);
            Console.WriteLine("ResetCursor()");
        }
    }
}
