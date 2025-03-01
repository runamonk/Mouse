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
        private int savedCursorY = 0;
        private int savedCursorX = 0;
        private bool cursorIsHidden = false;
        private int mouseTimeoutSeconds = 10;

        private DateTime lastKeyEventTime;
        // Windows cursor constants. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setsystemcursor
        private string[] cursors = { "32512", "32513", "32514", "32515", "32516", "32642", "32643", "32644", "32645", "32646", "32648", "32649", "32650" };
        private const string activeChars = "abcdefghijklmnopqrstuvwxyz0123456789!£$~¬`{}[],.<>/?_+-=";
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

        private void OnKeyPressed(Keys key)
        {
            // Hide cursor when cursor is visible and actual characters/symbols and not system keys
            if (cursorIsHidden || (activeChars.IndexOf(key.ToString().ToLower()) == -1))
                return;
            lastKeyEventTime = DateTime.Now;
            cursorIsHidden = true;
            savedCursorX = Cursor.Position.X;
            savedCursorY = Cursor.Position.Y;

            foreach (string c in cursors)
            {
                SetSystemCursor(LoadCursorFromFile(@Path.GetDirectoryName(Application.ExecutablePath) + "\\blank_cursor.cur"), Convert.ToUInt32(c));
            }
            CursorResetTimer.Start();
        }

        public Mouse()
        {
            notifyIcon.Visible = true;
            notifyIcon.Icon = new System.Drawing.Icon(@Path.GetDirectoryName(Application.ExecutablePath) + "\\hide.ico");
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
            CursorResetTimer.Interval = 500;
            CursorResetTimer.Tick += (sender, args) =>
            {
                TimeSpan ts = DateTime.Now - lastKeyEventTime;
                if (Cursor.Position.X != savedCursorX || Cursor.Position.Y != savedCursorY || Math.Round(ts.TotalSeconds) > mouseTimeoutSeconds)
                    ResetCursor();            
            };

            ResetCursor();
            hooks.OnKeyPress += OnKeyPressed;
            hooks.Start();
        }
        ~Mouse()
        {
            ResetCursor();
        }

        private void ResetCursor()
        {
            CursorResetTimer.Stop();
            savedCursorX = Cursor.Position.X;
            savedCursorY = Cursor.Position.Y;
            cursorIsHidden = false;
            SystemParametersInfo(0x0057, 0, null, 0);
        }
    }
}
