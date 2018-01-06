
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Hooks;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;

namespace System.Hooks
{
    /// <summary>
    /// A wrapper for a native win32 window
    /// </summary>
    public class Win32Window
    {
        public IntPtr hWnd;

        /// <summary>
        /// Creates a new Win32Window instance from existing window.
        /// </summary>
        /// <param name="hWnd"></param>
        public Win32Window(IntPtr hWnd)
        {
            this.hWnd = new IntPtr(hWnd.ToInt32());
        }

        public string Title
        {
            get
            {
                StringBuilder sb = new StringBuilder(GetWindowTextLength(hWnd) + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                return sb.ToString();
            }

            set
            {
                SetWindowText(hWnd, value);
            }
        }

        public string ClassName
        {
            get
            {
                int length = 64;
                while (true)
                {
                    StringBuilder sb = new StringBuilder(length);
                    int OK=GetClassName(hWnd, sb, sb.Capacity);
                    if (OK == 0) { throw new Win32Exception(Marshal.GetLastWin32Error()); }


                    if (sb.Length != length - 1)
                    {
                        return sb.ToString();
                    }
                    length *= 2;
                }
            }
        }

        public bool Visible
        {
            get
            {
                return IsWindowVisible(hWnd);
            }
        }

        public bool Active
        {
            get
            {
                return hWnd.Equals(GetForegroundWindow());
            }
        }
        public RECT Position
        {
            get
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.length = Marshal.SizeOf(wp);
                GetWindowPlacement(hWnd, ref wp);
                return wp.rcNormalPosition;
            }

            set
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.length = Marshal.SizeOf(wp);
                GetWindowPlacement(hWnd, ref wp);
                wp.rcNormalPosition = value;
                SetWindowPlacement(hWnd, ref wp);
            }
        }
        public ShowWindowCommands ShowCmd
        {
            get
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.length = Marshal.SizeOf(wp);
                GetWindowPlacement(hWnd, ref wp);
                return wp.showCmd;
            }

            set
            {
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                wp.length = Marshal.SizeOf(wp);
                GetWindowPlacement(hWnd, ref wp);
                wp.showCmd = value;
                SetWindowPlacement(hWnd, ref wp);
            }
        }

        public Process Process
        {
            get
            {
                int pid;
                GetWindowThreadProcessId(hWnd, out pid);
                return Process.GetProcessById(pid);
            }
        }

        public ProcessThread GUIthread
        {
            get
            {
                int pid;
                int tid = GetWindowThreadProcessId(hWnd, out pid);
                foreach (ProcessThread t in Process.GetProcessById(pid).Threads)
                {
                    if (t.Id == tid) return t;
                }
                throw new Exception("Thread not found");
            }
        }


        public override string ToString()
        {
            return  "Window: " + Process.ProcessName + " Title=" + Title + " " + "X: " + Position.X + ",Y: " + Position.Y;
        }


        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType,
            IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        #region PInvoke Declarations

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate int EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        public static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr
            hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess,
            uint idThread, uint dwFlags);


        public enum GWL : int
        {
            GWL_WNDPROC = (-4),
            GWL_HINSTANCE = (-6),
            GWL_HWNDPARENT = (-8),
            GWL_STYLE = (-16),
            GWL_EXSTYLE = (-20),
            GWL_USERDATA = (-21),
            GWL_ID = (-12)
        }

        // This helper static method is required because the 32-bit version of user32.dll does not contain this API
        // (on any versions of Windows), so linking the method will fail at run-time. The bridge dispatches the request
        // to the correct function (GetWindowLong in 32-bit mode and GetWindowLongPtr in 64-bit mode)
        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);
        public enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowPlacement(IntPtr hWnd,
           ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPlacement(IntPtr hWnd,
           [In] ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect,
           int nBottomRect);

        [DllImport("user32.dll")]
        public static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth,
           int nHeight, IntPtr hObjSource, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        public enum TernaryRasterOperations : uint
        {
            SRCCOPY = 0x00CC0020,
            SRCPAINT = 0x00EE0086,
            SRCAND = 0x008800C6,
            SRCINVERT = 0x00660046,
            SRCERASE = 0x00440328,
            NOTSRCCOPY = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY = 0x00C000CA,
            MERGEPAINT = 0x00BB0226,
            PATCOPY = 0x00F00021,
            PATPAINT = 0x00FB0A09,
            PATINVERT = 0x005A0049,
            DSTINVERT = 0x00550009,
            BLACKNESS = 0x00000042,
            WHITENESS = 0x00FF0062
        }

        public enum GetWindowRegnReturnValues : int
        {
            ERROR = 0,
            NULLREGION = 1,
            SIMPLEREGION = 2,
            COMPLEXREGION = 3
        }

        public static readonly uint EM_GETPASSWORDCHAR = 0xD2, EM_SETPASSWORDCHAR = 0xCC;
        public static readonly uint BM_GETCHECK = 0xF0, BM_SETCHECK = 0xF1;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        public static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, [Out] StringBuilder lParam);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
           int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        public const int WM_CLOSE = 16;

        public enum GetWindow_Cmd
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        public enum RDW : uint
        {
            RDW_INVALIDATE = 0x0001,
            RDW_INTERNALPAINT = 0x0002,
            RDW_ERASE = 0x0004,

            RDW_VALIDATE = 0x0008,
            RDW_NOINTERNALPAINT = 0x0010,
            RDW_NOERASE = 0x0020,

            RDW_NOCHILDREN = 0x0040,
            RDW_ALLCHILDREN = 0x0080,

            RDW_UPDATENOW = 0x0100,
            RDW_ERASENOW = 0x0200,

            RDW_FRAME = 0x0400,
            RDW_NOFRAME = 0x0800,
        }

        [DllImport("user32.dll")]
        public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, RDW flags);

        #endregion
    }

	public enum WindowsMessages: int
	{
		WM_NULL = 0x0000,
		WM_CREATE = 0x0001,
		WM_DESTROY = 0x0002,
		WM_MOVE = 0x0003,
		WM_SIZE = 0x0005,
		WM_ACTIVATE = 0x0006,
		WM_SETFOCUS = 0x0007,
		WM_KILLFOCUS = 0x0008,
		WM_ENABLE = 0x000A,
		WM_SETREDRAW = 0x000B,
		WM_SETTEXT = 0x000C,
		WM_GETTEXT = 0x000D,
		WM_GETTEXTLENGTH = 0x000E,
		WM_PAINT = 0x000F,
		WM_CLOSE = 0x0010,
		WM_QUERYENDSESSION = 0x0011,
		WM_QUERYOPEN = 0x0013,
		WM_ENDSESSION = 0x0016,
		WM_QUIT = 0x0012,
		WM_ERASEBKGND = 0x0014,
		WM_SYSCOLORCHANGE = 0x0015,
		WM_SHOWWINDOW = 0x0018,
		WM_WININICHANGE = 0x001A,
		WM_SETTINGCHANGE = 0x001A,
		WM_DEVMODECHANGE = 0x001B,
		WM_ACTIVATEAPP = 0x001C,
		WM_FONTCHANGE = 0x001D,
		WM_TIMECHANGE = 0x001E,
		WM_CANCELMODE = 0x001F,
		WM_SETCURSOR = 0x0020,
		WM_MOUSEACTIVATE = 0x0021,
		WM_CHILDACTIVATE = 0x0022,
		WM_QUEUESYNC = 0x0023,
		WM_GETMINMAXINFO = 0x0024,
		WM_PAINTICON = 0x0026,
		WM_ICONERASEBKGND = 0x0027,
		WM_NEXTDLGCTL = 0x0028,
		WM_SPOOLERSTATUS = 0x002A,
		WM_DRAWITEM = 0x002B,
		WM_MEASUREITEM = 0x002C,
		WM_DELETEITEM = 0x002D,
		WM_VKEYTOITEM = 0x002E,
		WM_CHARTOITEM = 0x002F,
		WM_SETFONT = 0x0030,
		WM_GETFONT = 0x0031,
		WM_SETHOTKEY = 0x0032,
		WM_GETHOTKEY = 0x0033,
		WM_QUERYDRAGICON = 0x0037,
		WM_COMPAREITEM = 0x0039,
		WM_GETOBJECT = 0x003D,
		WM_COMPACTING = 0x0041,
		WM_COMMNOTIFY = 0x0044,
		WM_WINDOWPOSCHANGING = 0x0046,
		WM_WINDOWPOSCHANGED = 0x0047,
		WM_POWER = 0x0048,
		WM_COPYDATA = 0x004A,
		WM_CANCELJOURNAL = 0x004B,
		WM_NOTIFY = 0x004E,
		WM_INPUTLANGCHANGEREQUEST = 0x0050,
		WM_INPUTLANGCHANGE = 0x0051,
		WM_TCARD = 0x0052,
		WM_HELP = 0x0053,
		WM_USERCHANGED = 0x0054,
		WM_NOTIFYFORMAT = 0x0055,
		WM_CONTEXTMENU = 0x007B,
		WM_STYLECHANGING = 0x007C,
		WM_STYLECHANGED = 0x007D,
		WM_DISPLAYCHANGE = 0x007E,
		WM_GETICON = 0x007F,
		WM_SETICON = 0x0080,
		WM_NCCREATE = 0x0081,
		WM_NCDESTROY = 0x0082,
		WM_NCCALCSIZE = 0x0083,
		WM_NCHITTEST = 0x0084,
		WM_NCPAINT = 0x0085,
		WM_NCACTIVATE = 0x0086,
		WM_GETDLGCODE = 0x0087,
		WM_SYNCPAINT = 0x0088,
		WM_NCMOUSEMOVE = 0x00A0,
		WM_NCLBUTTONDOWN = 0x00A1,
		WM_NCLBUTTONUP = 0x00A2,
		WM_NCLBUTTONDBLCLK = 0x00A3,
		WM_NCRBUTTONDOWN = 0x00A4,
		WM_NCRBUTTONUP = 0x00A5,
		WM_NCRBUTTONDBLCLK = 0x00A6,
		WM_NCMBUTTONDOWN = 0x00A7,
		WM_NCMBUTTONUP = 0x00A8,
		WM_NCMBUTTONDBLCLK = 0x00A9,
		WM_NCXBUTTONDOWN = 0x00AB,
		WM_NCXBUTTONUP = 0x00AC,
		WM_NCXBUTTONDBLCLK = 0x00AD,
		WM_INPUT = 0x00FF,
		WM_KEYFIRST = 0x0100,
		WM_KEYDOWN = 0x0100,
		WM_KEYUP = 0x0101,
		WM_CHAR = 0x0102,
		WM_DEADCHAR = 0x0103,
		WM_SYSKEYDOWN = 0x0104,
		WM_SYSKEYUP = 0x0105,
		WM_SYSCHAR = 0x0106,
		WM_SYSDEADCHAR = 0x0107,
		WM_UNICHAR = 0x0109,
		WM_KEYLAST_NT501 = 0x0109,
		UNICODE_NOCHAR = 0xFFFF,
		WM_KEYLAST_PRE501 = 0x0108,
		WM_IME_STARTCOMPOSITION = 0x010D,
		WM_IME_ENDCOMPOSITION = 0x010E,
		WM_IME_COMPOSITION = 0x010F,
		WM_IME_KEYLAST = 0x010F,
		WM_INITDIALOG = 0x0110,
		WM_COMMAND = 0x0111,
		WM_SYSCOMMAND = 0x0112,
		WM_TIMER = 0x0113,
		WM_HSCROLL = 0x0114,
		WM_VSCROLL = 0x0115,
		WM_INITMENU = 0x0116,
		WM_INITMENUPOPUP = 0x0117,
		WM_MENUSELECT = 0x011F,
		WM_MENUCHAR = 0x0120,
		WM_ENTERIDLE = 0x0121,
		WM_MENURBUTTONUP = 0x0122,
		WM_MENUDRAG = 0x0123,
		WM_MENUGETOBJECT = 0x0124,
		WM_UNINITMENUPOPUP = 0x0125,
		WM_MENUCOMMAND = 0x0126,
		WM_CHANGEUISTATE = 0x0127,
		WM_UPDATEUISTATE = 0x0128,
		WM_QUERYUISTATE = 0x0129,
		WM_CTLCOLORMSGBOX = 0x0132,
		WM_CTLCOLOREDIT = 0x0133,
		WM_CTLCOLORLISTBOX = 0x0134,
		WM_CTLCOLORBTN = 0x0135,
		WM_CTLCOLORDLG = 0x0136,
		WM_CTLCOLORSCROLLBAR = 0x0137,
		WM_CTLCOLORSTATIC = 0x0138,
		WM_MOUSEFIRST = 0x0200,
		WM_MOUSEMOVE = 0x0200,
		WM_LBUTTONDOWN = 0x0201,
		WM_LBUTTONUP = 0x0202,
		WM_LBUTTONDBLCLK = 0x0203,
		WM_RBUTTONDOWN = 0x0204,
		WM_RBUTTONUP = 0x0205,
		WM_RBUTTONDBLCLK = 0x0206,
		WM_MBUTTONDOWN = 0x0207,
		WM_MBUTTONUP = 0x0208,
		WM_MBUTTONDBLCLK = 0x0209,
		WM_MOUSEWHEEL = 0x020A,
		WM_XBUTTONDOWN = 0x020B,
		WM_XBUTTONUP = 0x020C,
		WM_XBUTTONDBLCLK = 0x020D,
		WM_MOUSELAST_5 = 0x020D,
		WM_MOUSELAST_4 = 0x020A,
		WM_MOUSELAST_PRE_4 = 0x0209,
		WM_PARENTNOTIFY = 0x0210,
		WM_ENTERMENULOOP = 0x0211,
		WM_EXITMENULOOP = 0x0212,
		WM_NEXTMENU = 0x0213,
		WM_SIZING = 0x0214,
		WM_CAPTURECHANGED = 0x0215,
		WM_MOVING = 0x0216,
		WM_POWERBROADCAST = 0x0218,
		WM_DEVICECHANGE = 0x0219,
		WM_MDICREATE = 0x0220,
		WM_MDIDESTROY = 0x0221,
		WM_MDIACTIVATE = 0x0222,
		WM_MDIRESTORE = 0x0223,
		WM_MDINEXT = 0x0224,
		WM_MDIMAXIMIZE = 0x0225,
		WM_MDITILE = 0x0226,
		WM_MDICASCADE = 0x0227,
		WM_MDIICONARRANGE = 0x0228,
		WM_MDIGETACTIVE = 0x0229,
		WM_MDISETMENU = 0x0230,
		WM_ENTERSIZEMOVE = 0x0231,
		WM_EXITSIZEMOVE = 0x0232,
		WM_DROPFILES = 0x0233,
		WM_MDIREFRESHMENU = 0x0234,
		WM_IME_SETCONTEXT = 0x0281,
		WM_IME_NOTIFY = 0x0282,
		WM_IME_CONTROL = 0x0283,
		WM_IME_COMPOSITIONFULL = 0x0284,
		WM_IME_SELECT = 0x0285,
		WM_IME_CHAR = 0x0286,
		WM_IME_REQUEST = 0x0288,
		WM_IME_KEYDOWN = 0x0290,
		WM_IME_KEYUP = 0x0291,
		WM_MOUSEHOVER = 0x02A1,
		WM_MOUSELEAVE = 0x02A3,
		WM_NCMOUSEHOVER = 0x02A0,
		WM_NCMOUSELEAVE = 0x02A2,
		WM_WTSSESSION_CHANGE = 0x02B1,
		WM_TABLET_FIRST = 0x02c0,
		WM_TABLET_LAST = 0x02df,
		WM_CUT = 0x0300,
		WM_COPY = 0x0301,
		WM_PASTE = 0x0302,
		WM_CLEAR = 0x0303,
		WM_UNDO = 0x0304,
		WM_RENDERFORMAT = 0x0305,
		WM_RENDERALLFORMATS = 0x0306,
		WM_DESTROYCLIPBOARD = 0x0307,
		WM_DRAWCLIPBOARD = 0x0308,
		WM_PAINTCLIPBOARD = 0x0309,
		WM_VSCROLLCLIPBOARD = 0x030A,
		WM_SIZECLIPBOARD = 0x030B,
		WM_ASKCBFORMATNAME = 0x030C,
		WM_CHANGECBCHAIN = 0x030D,
		WM_HSCROLLCLIPBOARD = 0x030E,
		WM_QUERYNEWPALETTE = 0x030F,
		WM_PALETTEISCHANGING = 0x0310,
		WM_PALETTECHANGED = 0x0311,
		WM_HOTKEY = 0x0312,
		WM_PRINT = 0x0317,
		WM_PRINTCLIENT = 0x0318,
		WM_APPCOMMAND = 0x0319,
		WM_THEMECHANGED = 0x031A,
		WM_HANDHELDFIRST = 0x0358,
		WM_HANDHELDLAST = 0x035F,
		WM_AFXFIRST = 0x0360,
		WM_AFXLAST = 0x037F,
		WM_PENWINFIRST = 0x0380,
		WM_PENWINLAST = 0x038F,
		WM_APP = 0x8000,
		WM_USER = 0x0400,
		EM_GETSEL = 0x00B0,
		EM_SETSEL = 0x00B1,
		EM_GETRECT = 0x00B2,
		EM_SETRECT = 0x00B3,
		EM_SETRECTNP = 0x00B4,
		EM_SCROLL = 0x00B5,
		EM_LINESCROLL = 0x00B6,
		EM_SCROLLCARET = 0x00B7,
		EM_GETMODIFY = 0x00B8,
		EM_SETMODIFY = 0x00B9,
		EM_GETLINECOUNT = 0x00BA,
		EM_LINEINDEX = 0x00BB,
		EM_SETHANDLE = 0x00BC,
		EM_GETHANDLE = 0x00BD,
		EM_GETTHUMB = 0x00BE,
		EM_LINELENGTH = 0x00C1,
		EM_REPLACESEL = 0x00C2,
		EM_GETLINE = 0x00C4,
		EM_LIMITTEXT = 0x00C5,
		EM_CANUNDO = 0x00C6,
		EM_UNDO = 0x00C7,
		EM_FMTLINES = 0x00C8,
		EM_LINEFROMCHAR = 0x00C9,
		EM_SETTABSTOPS = 0x00CB,
		EM_SETPASSWORDCHAR = 0x00CC,
		EM_EMPTYUNDOBUFFER = 0x00CD,
		EM_GETFIRSTVISIBLELINE = 0x00CE,
		EM_SETREADONLY = 0x00CF,
		EM_SETWORDBREAKPROC = 0x00D0,
		EM_GETWORDBREAKPROC = 0x00D1,
		EM_GETPASSWORDCHAR = 0x00D2,
		EM_SETMARGINS = 0x00D3,
		EM_GETMARGINS = 0x00D4,
		EM_SETLIMITTEXT = EM_LIMITTEXT,
		EM_GETLIMITTEXT = 0x00D5,
		EM_POSFROMCHAR = 0x00D6,
		EM_CHARFROMPOS = 0x00D7,
		EM_SETIMESTATUS = 0x00D8,
		EM_GETIMESTATUS = 0x00D9,
		BM_GETCHECK= 0x00F0,
		BM_SETCHECK= 0x00F1,
		BM_GETSTATE= 0x00F2,
		BM_SETSTATE= 0x00F3,
		BM_SETSTYLE= 0x00F4,
		BM_CLICK = 0x00F5,
		BM_GETIMAGE= 0x00F6,
		BM_SETIMAGE= 0x00F7,
		STM_SETICON = 0x0170,
		STM_GETICON = 0x0171,
		STM_SETIMAGE = 0x0172,
		STM_GETIMAGE = 0x0173,
		STM_MSGMAX = 0x0174,
		DM_GETDEFID = (WM_USER+0),
		DM_SETDEFID = (WM_USER+1),
		DM_REPOSITION = (WM_USER+2),
		LB_ADDSTRING = 0x0180,
		LB_INSERTSTRING = 0x0181,
		LB_DELETESTRING = 0x0182,
		LB_SELITEMRANGEEX= 0x0183,
		LB_RESETCONTENT = 0x0184,
		LB_SETSEL = 0x0185,
		LB_SETCURSEL = 0x0186,
		LB_GETSEL = 0x0187,
		LB_GETCURSEL = 0x0188,
		LB_GETTEXT = 0x0189,
		LB_GETTEXTLEN = 0x018A,
		LB_GETCOUNT = 0x018B,
		LB_SELECTSTRING = 0x018C,
		LB_DIR = 0x018D,
		LB_GETTOPINDEX = 0x018E,
		LB_FINDSTRING = 0x018F,
		LB_GETSELCOUNT = 0x0190,
		LB_GETSELITEMS = 0x0191,
		LB_SETTABSTOPS = 0x0192,
		LB_GETHORIZONTALEXTENT = 0x0193,
		LB_SETHORIZONTALEXTENT = 0x0194,
		LB_SETCOLUMNWIDTH = 0x0195,
		LB_ADDFILE = 0x0196,
		LB_SETTOPINDEX = 0x0197,
		LB_GETITEMRECT = 0x0198,
		LB_GETITEMDATA = 0x0199,
		LB_SETITEMDATA = 0x019A,
		LB_SELITEMRANGE = 0x019B,
		LB_SETANCHORINDEX = 0x019C,
		LB_GETANCHORINDEX = 0x019D,
		LB_SETCARETINDEX = 0x019E,
		LB_GETCARETINDEX = 0x019F,
		LB_SETITEMHEIGHT = 0x01A0,
		LB_GETITEMHEIGHT = 0x01A1,
		LB_FINDSTRINGEXACT = 0x01A2,
		LB_SETLOCALE = 0x01A5,
		LB_GETLOCALE = 0x01A6,
		LB_SETCOUNT = 0x01A7,
		LB_INITSTORAGE = 0x01A8,
		LB_ITEMFROMPOINT = 0x01A9,
		LB_MULTIPLEADDSTRING = 0x01B1,
		LB_GETLISTBOXINFO= 0x01B2,
		LB_MSGMAX_501 = 0x01B3,
		LB_MSGMAX_WCE4 = 0x01B1,
		LB_MSGMAX_4 = 0x01B0,
		LB_MSGMAX_PRE4 = 0x01A8,
		CB_GETEDITSEL = 0x0140,
		CB_LIMITTEXT = 0x0141,
		CB_SETEDITSEL = 0x0142,
		CB_ADDSTRING = 0x0143,
		CB_DELETESTRING = 0x0144,
		CB_DIR = 0x0145,
		CB_GETCOUNT = 0x0146,
		CB_GETCURSEL = 0x0147,
		CB_GETLBTEXT = 0x0148,
		CB_GETLBTEXTLEN = 0x0149,
		CB_INSERTSTRING = 0x014A,
		CB_RESETCONTENT = 0x014B,
		CB_FINDSTRING = 0x014C,
		CB_SELECTSTRING = 0x014D,
		CB_SETCURSEL = 0x014E,
		CB_SHOWDROPDOWN = 0x014F,
		CB_GETITEMDATA = 0x0150,
		CB_SETITEMDATA = 0x0151,
		CB_GETDROPPEDCONTROLRECT = 0x0152,
		CB_SETITEMHEIGHT = 0x0153,
		CB_GETITEMHEIGHT = 0x0154,
		CB_SETEXTENDEDUI = 0x0155,
		CB_GETEXTENDEDUI = 0x0156,
		CB_GETDROPPEDSTATE = 0x0157,
		CB_FINDSTRINGEXACT = 0x0158,
		CB_SETLOCALE = 0x0159,
		CB_GETLOCALE = 0x015A,
		CB_GETTOPINDEX = 0x015B,
		CB_SETTOPINDEX = 0x015C,
		CB_GETHORIZONTALEXTENT = 0x015d,
		CB_SETHORIZONTALEXTENT = 0x015e,
		CB_GETDROPPEDWIDTH = 0x015f,
		CB_SETDROPPEDWIDTH = 0x0160,
		CB_INITSTORAGE = 0x0161,
		CB_MULTIPLEADDSTRING = 0x0163,
		CB_GETCOMBOBOXINFO = 0x0164,
		CB_MSGMAX_501 = 0x0165,
		CB_MSGMAX_WCE400 = 0x0163,
		CB_MSGMAX_400 = 0x0162,
		CB_MSGMAX_PRE400 = 0x015B,
		SBM_SETPOS = 0x00E0, 
		SBM_GETPOS = 0x00E1, 
		SBM_SETRANGE = 0x00E2, 
		SBM_SETRANGEREDRAW = 0x00E6,
		SBM_GETRANGE = 0x00E3,
		SBM_ENABLE_ARROWS = 0x00E4,
		SBM_SETSCROLLINFO = 0x00E9,
		SBM_GETSCROLLINFO = 0x00EA,
		SBM_GETSCROLLBARINFO= 0x00EB,
		LVM_FIRST = 0x1000,// ListView messages
		TV_FIRST = 0x1100,// TreeView messages
		HDM_FIRST = 0x1200,// Header messages
		TCM_FIRST = 0x1300,// Tab control messages
		PGM_FIRST = 0x1400,// Pager control messages
		ECM_FIRST = 0x1500,// Edit control messages
		BCM_FIRST = 0x1600,// Button control messages
		CBM_FIRST = 0x1700,// Combobox control messages
		CCM_FIRST = 0x2000,// Common control shared messages
		CCM_LAST =(CCM_FIRST + 0x200),
		CCM_SETBKCOLOR = (CCM_FIRST + 1),
		CCM_SETCOLORSCHEME = (CCM_FIRST + 2),
		CCM_GETCOLORSCHEME = (CCM_FIRST + 3),
		CCM_GETDROPTARGET = (CCM_FIRST + 4),
		CCM_SETUNICODEFORMAT = (CCM_FIRST + 5),
		CCM_GETUNICODEFORMAT = (CCM_FIRST + 6),
		CCM_SETVERSION = (CCM_FIRST + 0x7),
		CCM_GETVERSION = (CCM_FIRST + 0x8),
		CCM_SETNOTIFYWINDOW = (CCM_FIRST + 0x9),
		CCM_SETWINDOWTHEME = (CCM_FIRST + 0xb),
		CCM_DPISCALE = (CCM_FIRST + 0xc),
		HDM_GETITEMCOUNT = (HDM_FIRST + 0),
		HDM_INSERTITEMA = (HDM_FIRST + 1),
		HDM_INSERTITEMW = (HDM_FIRST + 10),
		HDM_DELETEITEM = (HDM_FIRST + 2),
		HDM_GETITEMA = (HDM_FIRST + 3),
		HDM_GETITEMW = (HDM_FIRST + 11),
		HDM_SETITEMA = (HDM_FIRST + 4),
		HDM_SETITEMW = (HDM_FIRST + 12),
		HDM_LAYOUT = (HDM_FIRST + 5),
		HDM_HITTEST = (HDM_FIRST + 6),
		HDM_GETITEMRECT = (HDM_FIRST + 7),
		HDM_SETIMAGELIST = (HDM_FIRST + 8),
		HDM_GETIMAGELIST = (HDM_FIRST + 9),
		HDM_ORDERTOINDEX = (HDM_FIRST + 15),
		HDM_CREATEDRAGIMAGE = (HDM_FIRST + 16),
		HDM_GETORDERARRAY = (HDM_FIRST + 17),
		HDM_SETORDERARRAY = (HDM_FIRST + 18),
		HDM_SETHOTDIVIDER = (HDM_FIRST + 19),
		HDM_SETBITMAPMARGIN = (HDM_FIRST + 20),
		HDM_GETBITMAPMARGIN = (HDM_FIRST + 21),
		HDM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		HDM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		HDM_SETFILTERCHANGETIMEOUT = (HDM_FIRST+22),
		HDM_EDITFILTER = (HDM_FIRST+23),
		HDM_CLEARFILTER = (HDM_FIRST+24),
		TB_ENABLEBUTTON = (WM_USER + 1),
		TB_CHECKBUTTON = (WM_USER + 2),
		TB_PRESSBUTTON = (WM_USER + 3),
		TB_HIDEBUTTON = (WM_USER + 4),
		TB_INDETERMINATE = (WM_USER + 5),
		TB_MARKBUTTON = (WM_USER + 6),
		TB_ISBUTTONENABLED = (WM_USER + 9),
		TB_ISBUTTONCHECKED = (WM_USER + 10),
		TB_ISBUTTONPRESSED = (WM_USER + 11),
		TB_ISBUTTONHIDDEN = (WM_USER + 12),
		TB_ISBUTTONINDETERMINATE = (WM_USER + 13),
		TB_ISBUTTONHIGHLIGHTED = (WM_USER + 14),
		TB_SETSTATE = (WM_USER + 17),
		TB_GETSTATE = (WM_USER + 18),
		TB_ADDBITMAP = (WM_USER + 19),
		TB_ADDBUTTONSA = (WM_USER + 20),
		TB_INSERTBUTTONA = (WM_USER + 21),
		TB_ADDBUTTONS = (WM_USER + 20),
		TB_INSERTBUTTON = (WM_USER + 21),
		TB_DELETEBUTTON = (WM_USER + 22),
		TB_GETBUTTON = (WM_USER + 23),
		TB_BUTTONCOUNT = (WM_USER + 24),
		TB_COMMANDTOINDEX = (WM_USER + 25),
		TB_SAVERESTOREA = (WM_USER + 26),
		TB_SAVERESTOREW = (WM_USER + 76),
		TB_CUSTOMIZE = (WM_USER + 27),
		TB_ADDSTRINGA = (WM_USER + 28),
		TB_ADDSTRINGW = (WM_USER + 77),
		TB_GETITEMRECT = (WM_USER + 29),
		TB_BUTTONSTRUCTSIZE = (WM_USER + 30),
		TB_SETBUTTONSIZE = (WM_USER + 31),
		TB_SETBITMAPSIZE = (WM_USER + 32),
		TB_AUTOSIZE = (WM_USER + 33),
		TB_GETTOOLTIPS = (WM_USER + 35),
		TB_SETTOOLTIPS = (WM_USER + 36),
		TB_SETPARENT = (WM_USER + 37),
		TB_SETROWS = (WM_USER + 39),
		TB_GETROWS = (WM_USER + 40),
		TB_SETCMDID = (WM_USER + 42),
		TB_CHANGEBITMAP = (WM_USER + 43),
		TB_GETBITMAP = (WM_USER + 44),
		TB_GETBUTTONTEXTA = (WM_USER + 45),
		TB_GETBUTTONTEXTW = (WM_USER + 75),
		TB_REPLACEBITMAP = (WM_USER + 46),
		TB_SETINDENT = (WM_USER + 47),
		TB_SETIMAGELIST = (WM_USER + 48),
		TB_GETIMAGELIST = (WM_USER + 49),
		TB_LOADIMAGES = (WM_USER + 50),
		TB_GETRECT = (WM_USER + 51),
		TB_SETHOTIMAGELIST = (WM_USER + 52),
		TB_GETHOTIMAGELIST = (WM_USER + 53),
		TB_SETDISABLEDIMAGELIST = (WM_USER + 54),
		TB_GETDISABLEDIMAGELIST = (WM_USER + 55),
		TB_SETSTYLE = (WM_USER + 56),
		TB_GETSTYLE = (WM_USER + 57),
		TB_GETBUTTONSIZE = (WM_USER + 58),
		TB_SETBUTTONWIDTH = (WM_USER + 59),
		TB_SETMAXTEXTROWS = (WM_USER + 60),
		TB_GETTEXTROWS = (WM_USER + 61),
		TB_GETOBJECT = (WM_USER + 62),
		TB_GETHOTITEM = (WM_USER + 71),
		TB_SETHOTITEM = (WM_USER + 72), 
		TB_SETANCHORHIGHLIGHT = (WM_USER + 73), 
		TB_GETANCHORHIGHLIGHT = (WM_USER + 74),
		TB_MAPACCELERATORA = (WM_USER + 78), 
		TB_GETINSERTMARK = (WM_USER + 79), 
		TB_SETINSERTMARK = (WM_USER + 80), 
		TB_INSERTMARKHITTEST = (WM_USER + 81),
		TB_MOVEBUTTON = (WM_USER + 82),
		TB_GETMAXSIZE = (WM_USER + 83),
		TB_SETEXTENDEDSTYLE = (WM_USER + 84),
		TB_GETEXTENDEDSTYLE = (WM_USER + 85),
		TB_GETPADDING = (WM_USER + 86),
		TB_SETPADDING = (WM_USER + 87),
		TB_SETINSERTMARKCOLOR = (WM_USER + 88),
		TB_GETINSERTMARKCOLOR = (WM_USER + 89),
		TB_SETCOLORSCHEME = CCM_SETCOLORSCHEME,
		TB_GETCOLORSCHEME = CCM_GETCOLORSCHEME,
		TB_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		TB_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		TB_MAPACCELERATORW = (WM_USER + 90),
		TB_GETBITMAPFLAGS = (WM_USER + 41),
		TB_GETBUTTONINFOW = (WM_USER + 63),
		TB_SETBUTTONINFOW = (WM_USER + 64),
		TB_GETBUTTONINFOA = (WM_USER + 65),
		TB_SETBUTTONINFOA = (WM_USER + 66),
		TB_INSERTBUTTONW = (WM_USER + 67),
		TB_ADDBUTTONSW = (WM_USER + 68),
		TB_HITTEST = (WM_USER + 69),
		TB_SETDRAWTEXTFLAGS = (WM_USER + 70),
		TB_GETSTRINGW = (WM_USER + 91),
		TB_GETSTRINGA = (WM_USER + 92),
		TB_GETMETRICS = (WM_USER + 101),
		TB_SETMETRICS = (WM_USER + 102),
		TB_SETWINDOWTHEME = CCM_SETWINDOWTHEME,
		RB_INSERTBANDA = (WM_USER + 1),
		RB_DELETEBAND = (WM_USER + 2),
		RB_GETBARINFO = (WM_USER + 3),
		RB_SETBARINFO = (WM_USER + 4),
		RB_GETBANDINFO = (WM_USER + 5),
		RB_SETBANDINFOA = (WM_USER + 6),
		RB_SETPARENT = (WM_USER + 7),
		RB_HITTEST = (WM_USER + 8),
		RB_GETRECT = (WM_USER + 9),
		RB_INSERTBANDW = (WM_USER + 10),
		RB_SETBANDINFOW = (WM_USER + 11),
		RB_GETBANDCOUNT = (WM_USER + 12),
		RB_GETROWCOUNT = (WM_USER + 13),
		RB_GETROWHEIGHT = (WM_USER + 14),
		RB_IDTOINDEX = (WM_USER + 16), 
		RB_GETTOOLTIPS = (WM_USER + 17),
		RB_SETTOOLTIPS = (WM_USER + 18),
		RB_SETBKCOLOR = (WM_USER + 19),
		RB_GETBKCOLOR = (WM_USER + 20),
		RB_SETTEXTCOLOR = (WM_USER + 21),
		RB_GETTEXTCOLOR = (WM_USER + 22),
		RB_SIZETORECT = (WM_USER + 23),
		RB_SETCOLORSCHEME = CCM_SETCOLORSCHEME,
		RB_GETCOLORSCHEME = CCM_GETCOLORSCHEME,
		RB_BEGINDRAG = (WM_USER + 24),
		RB_ENDDRAG = (WM_USER + 25),
		RB_DRAGMOVE = (WM_USER + 26),
		RB_GETBARHEIGHT = (WM_USER + 27),
		RB_GETBANDINFOW = (WM_USER + 28),
		RB_GETBANDINFOA = (WM_USER + 29),
		RB_MINIMIZEBAND = (WM_USER + 30),
		RB_MAXIMIZEBAND = (WM_USER + 31),
		RB_GETDROPTARGET = (CCM_GETDROPTARGET),
		RB_GETBANDBORDERS = (WM_USER + 34), 
		RB_SHOWBAND = (WM_USER + 35), 
		RB_SETPALETTE = (WM_USER + 37),
		RB_GETPALETTE = (WM_USER + 38),
		RB_MOVEBAND = (WM_USER + 39),
		RB_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		RB_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		RB_GETBANDMARGINS = (WM_USER + 40),
		RB_SETWINDOWTHEME = CCM_SETWINDOWTHEME,
		RB_PUSHCHEVRON = (WM_USER + 43),
		TTM_ACTIVATE = (WM_USER + 1),
		TTM_SETDELAYTIME = (WM_USER + 3),
		TTM_ADDTOOLA = (WM_USER + 4),
		TTM_ADDTOOLW = (WM_USER + 50),
		TTM_DELTOOLA = (WM_USER + 5),
		TTM_DELTOOLW = (WM_USER + 51),
		TTM_NEWTOOLRECTA = (WM_USER + 6),
		TTM_NEWTOOLRECTW = (WM_USER + 52),
		TTM_RELAYEVENT = (WM_USER + 7),
		TTM_GETTOOLINFOA = (WM_USER + 8),
		TTM_GETTOOLINFOW = (WM_USER + 53),
		TTM_SETTOOLINFOA = (WM_USER + 9),
		TTM_SETTOOLINFOW = (WM_USER + 54),
		TTM_HITTESTA = (WM_USER +10),
		TTM_HITTESTW = (WM_USER +55),
		TTM_GETTEXTA = (WM_USER +11),
		TTM_GETTEXTW = (WM_USER +56),
		TTM_UPDATETIPTEXTA = (WM_USER +12),
		TTM_UPDATETIPTEXTW = (WM_USER +57),
		TTM_GETTOOLCOUNT = (WM_USER +13),
		TTM_ENUMTOOLSA = (WM_USER +14),
		TTM_ENUMTOOLSW = (WM_USER +58),
		TTM_GETCURRENTTOOLA = (WM_USER + 15),
		TTM_GETCURRENTTOOLW = (WM_USER + 59),
		TTM_WINDOWFROMPOINT = (WM_USER + 16),
		TTM_TRACKACTIVATE = (WM_USER + 17),
		TTM_TRACKPOSITION = (WM_USER + 18),
		TTM_SETTIPBKCOLOR = (WM_USER + 19),
		TTM_SETTIPTEXTCOLOR = (WM_USER + 20),
		TTM_GETDELAYTIME = (WM_USER + 21),
		TTM_GETTIPBKCOLOR = (WM_USER + 22),
		TTM_GETTIPTEXTCOLOR = (WM_USER + 23),
		TTM_SETMAXTIPWIDTH = (WM_USER + 24),
		TTM_GETMAXTIPWIDTH = (WM_USER + 25),
		TTM_SETMARGIN = (WM_USER + 26),
		TTM_GETMARGIN = (WM_USER + 27),
		TTM_POP = (WM_USER + 28),
		TTM_UPDATE = (WM_USER + 29),
		TTM_GETBUBBLESIZE = (WM_USER + 30),
		TTM_ADJUSTRECT = (WM_USER + 31),
		TTM_SETTITLEA = (WM_USER + 32),
		TTM_SETTITLEW = (WM_USER + 33),
		TTM_POPUP = (WM_USER + 34),
		TTM_GETTITLE = (WM_USER + 35),
		TTM_SETWINDOWTHEME = CCM_SETWINDOWTHEME,
		SB_SETTEXTA = (WM_USER+1),
		SB_SETTEXTW = (WM_USER+11),
		SB_GETTEXTA = (WM_USER+2),
		SB_GETTEXTW = (WM_USER+13),
		SB_GETTEXTLENGTHA = (WM_USER+3),
		SB_GETTEXTLENGTHW = (WM_USER+12),
		SB_SETPARTS = (WM_USER+4),
		SB_GETPARTS = (WM_USER+6),
		SB_GETBORDERS = (WM_USER+7),
		SB_SETMINHEIGHT = (WM_USER+8),
		SB_SIMPLE = (WM_USER+9),
		SB_GETRECT = (WM_USER+10),
		SB_ISSIMPLE = (WM_USER+14),
		SB_SETICON = (WM_USER+15),
		SB_SETTIPTEXTA = (WM_USER+16),
		SB_SETTIPTEXTW = (WM_USER+17),
		SB_GETTIPTEXTA = (WM_USER+18),
		SB_GETTIPTEXTW = (WM_USER+19),
		SB_GETICON = (WM_USER+20),
		SB_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		SB_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		SB_SETBKCOLOR = CCM_SETBKCOLOR,
		SB_SIMPLEID = 0x00ff,
		TBM_GETPOS = (WM_USER),
		TBM_GETRANGEMIN = (WM_USER+1),
		TBM_GETRANGEMAX = (WM_USER+2),
		TBM_GETTIC = (WM_USER+3),
		TBM_SETTIC = (WM_USER+4),
		TBM_SETPOS = (WM_USER+5),
		TBM_SETRANGE = (WM_USER+6),
		TBM_SETRANGEMIN = (WM_USER+7),
		TBM_SETRANGEMAX = (WM_USER+8),
		TBM_CLEARTICS = (WM_USER+9),
		TBM_SETSEL = (WM_USER+10),
		TBM_SETSELSTART = (WM_USER+11),
		TBM_SETSELEND = (WM_USER+12),
		TBM_GETPTICS = (WM_USER+14),
		TBM_GETTICPOS = (WM_USER+15),
		TBM_GETNUMTICS = (WM_USER+16),
		TBM_GETSELSTART = (WM_USER+17),
		TBM_GETSELEND = (WM_USER+18),
		TBM_CLEARSEL = (WM_USER+19),
		TBM_SETTICFREQ = (WM_USER+20),
		TBM_SETPAGESIZE = (WM_USER+21),
		TBM_GETPAGESIZE = (WM_USER+22),
		TBM_SETLINESIZE = (WM_USER+23),
		TBM_GETLINESIZE = (WM_USER+24),
		TBM_GETTHUMBRECT = (WM_USER+25),
		TBM_GETCHANNELRECT = (WM_USER+26),
		TBM_SETTHUMBLENGTH = (WM_USER+27),
		TBM_GETTHUMBLENGTH = (WM_USER+28),
		TBM_SETTOOLTIPS = (WM_USER+29),
		TBM_GETTOOLTIPS = (WM_USER+30),
		TBM_SETTIPSIDE = (WM_USER+31),
		TBM_SETBUDDY = (WM_USER+32), 
		TBM_GETBUDDY = (WM_USER+33), 
		TBM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		TBM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		DL_BEGINDRAG = (WM_USER+133),
		DL_DRAGGING = (WM_USER+134),
		DL_DROPPED = (WM_USER+135),
		DL_CANCELDRAG = (WM_USER+136),
		UDM_SETRANGE = (WM_USER+101),
		UDM_GETRANGE = (WM_USER+102),
		UDM_SETPOS = (WM_USER+103),
		UDM_GETPOS = (WM_USER+104),
		UDM_SETBUDDY = (WM_USER+105),
		UDM_GETBUDDY = (WM_USER+106),
		UDM_SETACCEL = (WM_USER+107),
		UDM_GETACCEL = (WM_USER+108),
		UDM_SETBASE = (WM_USER+109),
		UDM_GETBASE = (WM_USER+110),
		UDM_SETRANGE32 = (WM_USER+111),
		UDM_GETRANGE32 = (WM_USER+112),
		UDM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		UDM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		UDM_SETPOS32 = (WM_USER+113),
		UDM_GETPOS32 = (WM_USER+114),
		PBM_SETRANGE = (WM_USER+1),
		PBM_SETPOS = (WM_USER+2),
		PBM_DELTAPOS = (WM_USER+3),
		PBM_SETSTEP = (WM_USER+4),
		PBM_STEPIT = (WM_USER+5),
		PBM_SETRANGE32 = (WM_USER+6),
		PBM_GETRANGE = (WM_USER+7), 
		PBM_GETPOS = (WM_USER+8),
		PBM_SETBARCOLOR = (WM_USER+9),
		PBM_SETBKCOLOR = CCM_SETBKCOLOR, 
		HKM_SETHOTKEY = (WM_USER+1),
		HKM_GETHOTKEY = (WM_USER+2),
		HKM_SETRULES = (WM_USER+3),
		LVM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		LVM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		LVM_GETBKCOLOR = (LVM_FIRST + 0),
		LVM_SETBKCOLOR = (LVM_FIRST + 1),
		LVM_GETIMAGELIST = (LVM_FIRST + 2),
		LVM_SETIMAGELIST = (LVM_FIRST + 3),
		LVM_GETITEMCOUNT = (LVM_FIRST + 4),
		LVM_GETITEMA = (LVM_FIRST + 5),
		LVM_GETITEMW = (LVM_FIRST + 75),
		LVM_SETITEMA = (LVM_FIRST + 6),
		LVM_SETITEMW = (LVM_FIRST + 76),
		LVM_INSERTITEMA = (LVM_FIRST + 7),
		LVM_INSERTITEMW = (LVM_FIRST + 77),
		LVM_DELETEITEM = (LVM_FIRST + 8),
		LVM_DELETEALLITEMS = (LVM_FIRST + 9),
		LVM_GETCALLBACKMASK = (LVM_FIRST + 10),
		LVM_SETCALLBACKMASK = (LVM_FIRST + 11),
		LVM_FINDITEMA = (LVM_FIRST + 13),
		LVM_FINDITEMW = (LVM_FIRST + 83),
		LVM_GETITEMRECT = (LVM_FIRST + 14),
		LVM_SETITEMPOSITION = (LVM_FIRST + 15),
		LVM_GETITEMPOSITION = (LVM_FIRST + 16),
		LVM_GETSTRINGWIDTHA = (LVM_FIRST + 17),
		LVM_GETSTRINGWIDTHW = (LVM_FIRST + 87),
		LVM_HITTEST = (LVM_FIRST + 18),
		LVM_ENSUREVISIBLE = (LVM_FIRST + 19),
		LVM_SCROLL = (LVM_FIRST + 20),
		LVM_REDRAWITEMS = (LVM_FIRST + 21),
		LVM_ARRANGE = (LVM_FIRST + 22),
		LVM_EDITLABELA = (LVM_FIRST + 23),
		LVM_EDITLABELW = (LVM_FIRST + 118),
		LVM_GETEDITCONTROL = (LVM_FIRST + 24),
		LVM_GETCOLUMNA = (LVM_FIRST + 25),
		LVM_GETCOLUMNW = (LVM_FIRST + 95),
		LVM_SETCOLUMNA = (LVM_FIRST + 26),
		LVM_SETCOLUMNW = (LVM_FIRST + 96),
		LVM_INSERTCOLUMNA = (LVM_FIRST + 27),
		LVM_INSERTCOLUMNW = (LVM_FIRST + 97),
		LVM_DELETECOLUMN = (LVM_FIRST + 28),
		LVM_GETCOLUMNWIDTH = (LVM_FIRST + 29),
		LVM_SETCOLUMNWIDTH = (LVM_FIRST + 30),
		LVM_CREATEDRAGIMAGE = (LVM_FIRST + 33),
		LVM_GETVIEWRECT = (LVM_FIRST + 34),
		LVM_GETTEXTCOLOR = (LVM_FIRST + 35),
		LVM_SETTEXTCOLOR = (LVM_FIRST + 36),
		LVM_GETTEXTBKCOLOR = (LVM_FIRST + 37),
		LVM_SETTEXTBKCOLOR = (LVM_FIRST + 38),
		LVM_GETTOPINDEX = (LVM_FIRST + 39),
		LVM_GETCOUNTPERPAGE = (LVM_FIRST + 40),
		LVM_GETORIGIN = (LVM_FIRST + 41),
		LVM_UPDATE = (LVM_FIRST + 42),
		LVM_SETITEMSTATE = (LVM_FIRST + 43),
		LVM_GETITEMSTATE = (LVM_FIRST + 44),
		LVM_GETITEMTEXTA = (LVM_FIRST + 45),
		LVM_GETITEMTEXTW = (LVM_FIRST + 115),
		LVM_SETITEMTEXTA = (LVM_FIRST + 46),
		LVM_SETITEMTEXTW = (LVM_FIRST + 116),
		LVM_SETITEMCOUNT = (LVM_FIRST + 47),
		LVM_SORTITEMS = (LVM_FIRST + 48),
		LVM_SETITEMPOSITION32 = (LVM_FIRST + 49),
		LVM_GETSELECTEDCOUNT = (LVM_FIRST + 50),
		LVM_GETITEMSPACING = (LVM_FIRST + 51),
		LVM_GETISEARCHSTRINGA = (LVM_FIRST + 52),
		LVM_GETISEARCHSTRINGW = (LVM_FIRST + 117),
		LVM_SETICONSPACING = (LVM_FIRST + 53),
		LVM_SETEXTENDEDLISTVIEWSTYLE = (LVM_FIRST + 54),
		LVM_GETEXTENDEDLISTVIEWSTYLE = (LVM_FIRST + 55),
		LVM_GETSUBITEMRECT = (LVM_FIRST + 56),
		LVM_SUBITEMHITTEST = (LVM_FIRST + 57),
		LVM_SETCOLUMNORDERARRAY = (LVM_FIRST + 58),
		LVM_GETCOLUMNORDERARRAY = (LVM_FIRST + 59),
		LVM_SETHOTITEM = (LVM_FIRST + 60),
		LVM_GETHOTITEM = (LVM_FIRST + 61),
		LVM_SETHOTCURSOR = (LVM_FIRST + 62),
		LVM_GETHOTCURSOR = (LVM_FIRST + 63),
		LVM_APPROXIMATEVIEWRECT = (LVM_FIRST + 64),
		LVM_SETWORKAREAS = (LVM_FIRST + 65),
		LVM_GETWORKAREAS = (LVM_FIRST + 70),
		LVM_GETNUMBEROFWORKAREAS = (LVM_FIRST + 73),
		LVM_GETSELECTIONMARK = (LVM_FIRST + 66),
		LVM_SETSELECTIONMARK = (LVM_FIRST + 67),
		LVM_SETHOVERTIME = (LVM_FIRST + 71),
		LVM_GETHOVERTIME = (LVM_FIRST + 72),
		LVM_SETTOOLTIPS = (LVM_FIRST + 74),
		LVM_GETTOOLTIPS = (LVM_FIRST + 78),
		LVM_SORTITEMSEX = (LVM_FIRST + 81),
		LVM_SETBKIMAGEA = (LVM_FIRST + 68),
		LVM_SETBKIMAGEW = (LVM_FIRST + 138),
		LVM_GETBKIMAGEA = (LVM_FIRST + 69),
		LVM_GETBKIMAGEW = (LVM_FIRST + 139),
		LVM_SETSELECTEDCOLUMN = (LVM_FIRST + 140),
		LVM_SETTILEWIDTH = (LVM_FIRST + 141),
		LVM_SETVIEW = (LVM_FIRST + 142),
		LVM_GETVIEW = (LVM_FIRST + 143),
		LVM_INSERTGROUP = (LVM_FIRST + 145),
		LVM_SETGROUPINFO = (LVM_FIRST + 147),
		LVM_GETGROUPINFO = (LVM_FIRST + 149),
		LVM_REMOVEGROUP = (LVM_FIRST + 150),
		LVM_MOVEGROUP = (LVM_FIRST + 151),
		LVM_MOVEITEMTOGROUP = (LVM_FIRST + 154),
		LVM_SETGROUPMETRICS = (LVM_FIRST + 155),
		LVM_GETGROUPMETRICS = (LVM_FIRST + 156),
		LVM_ENABLEGROUPVIEW = (LVM_FIRST + 157),
		LVM_SORTGROUPS = (LVM_FIRST + 158),
		LVM_INSERTGROUPSORTED = (LVM_FIRST + 159),
		LVM_REMOVEALLGROUPS = (LVM_FIRST + 160),
		LVM_HASGROUP = (LVM_FIRST + 161),
		LVM_SETTILEVIEWINFO = (LVM_FIRST + 162),
		LVM_GETTILEVIEWINFO = (LVM_FIRST + 163),
		LVM_SETTILEINFO = (LVM_FIRST + 164),
		LVM_GETTILEINFO = (LVM_FIRST + 165),
		LVM_SETINSERTMARK = (LVM_FIRST + 166),
		LVM_GETINSERTMARK = (LVM_FIRST + 167),
		LVM_INSERTMARKHITTEST = (LVM_FIRST + 168),
		LVM_GETINSERTMARKRECT = (LVM_FIRST + 169),
		LVM_SETINSERTMARKCOLOR = (LVM_FIRST + 170),
		LVM_GETINSERTMARKCOLOR = (LVM_FIRST + 171),
		LVM_SETINFOTIP = (LVM_FIRST + 173),
		LVM_GETSELECTEDCOLUMN = (LVM_FIRST + 174),
		LVM_ISGROUPVIEWENABLED = (LVM_FIRST + 175),
		LVM_GETOUTLINECOLOR = (LVM_FIRST + 176),
		LVM_SETOUTLINECOLOR = (LVM_FIRST + 177),
		LVM_CANCELEDITLABEL = (LVM_FIRST + 179),
		LVM_MAPINDEXTOID = (LVM_FIRST + 180),
		LVM_MAPIDTOINDEX = (LVM_FIRST + 181),
		TVM_INSERTITEMA = (TV_FIRST + 0),
		TVM_INSERTITEMW = (TV_FIRST + 50),
		TVM_DELETEITEM = (TV_FIRST + 1),
		TVM_EXPAND = (TV_FIRST + 2),
		TVM_GETITEMRECT = (TV_FIRST + 4),
		TVM_GETCOUNT = (TV_FIRST + 5),
		TVM_GETINDENT = (TV_FIRST + 6),
		TVM_SETINDENT = (TV_FIRST + 7),
		TVM_GETIMAGELIST = (TV_FIRST + 8),
		TVM_SETIMAGELIST = (TV_FIRST + 9),
		TVM_GETNEXTITEM = (TV_FIRST + 10),
		TVM_SELECTITEM = (TV_FIRST + 11),
		TVM_GETITEMA = (TV_FIRST + 12),
		TVM_GETITEMW = (TV_FIRST + 62),
		TVM_SETITEMA = (TV_FIRST + 13),
		TVM_SETITEMW = (TV_FIRST + 63),
		TVM_EDITLABELA = (TV_FIRST + 14),
		TVM_EDITLABELW = (TV_FIRST + 65),
		TVM_GETEDITCONTROL = (TV_FIRST + 15),
		TVM_GETVISIBLECOUNT = (TV_FIRST + 16),
		TVM_HITTEST = (TV_FIRST + 17),
		TVM_CREATEDRAGIMAGE = (TV_FIRST + 18),
		TVM_SORTCHILDREN = (TV_FIRST + 19),
		TVM_ENSUREVISIBLE = (TV_FIRST + 20),
		TVM_SORTCHILDRENCB = (TV_FIRST + 21),
		TVM_ENDEDITLABELNOW = (TV_FIRST + 22),
		TVM_GETISEARCHSTRINGA = (TV_FIRST + 23),
		TVM_GETISEARCHSTRINGW = (TV_FIRST + 64),
		TVM_SETTOOLTIPS = (TV_FIRST + 24),
		TVM_GETTOOLTIPS = (TV_FIRST + 25),
		TVM_SETINSERTMARK = (TV_FIRST + 26),
		TVM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		TVM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		TVM_SETITEMHEIGHT = (TV_FIRST + 27),
		TVM_GETITEMHEIGHT = (TV_FIRST + 28),
		TVM_SETBKCOLOR = (TV_FIRST + 29),
		TVM_SETTEXTCOLOR = (TV_FIRST + 30),
		TVM_GETBKCOLOR = (TV_FIRST + 31),
		TVM_GETTEXTCOLOR = (TV_FIRST + 32),
		TVM_SETSCROLLTIME = (TV_FIRST + 33),
		TVM_GETSCROLLTIME = (TV_FIRST + 34),
		TVM_SETINSERTMARKCOLOR = (TV_FIRST + 37),
		TVM_GETINSERTMARKCOLOR = (TV_FIRST + 38),
		TVM_GETITEMSTATE = (TV_FIRST + 39),
		TVM_SETLINECOLOR = (TV_FIRST + 40),
		TVM_GETLINECOLOR = (TV_FIRST + 41),
		TVM_MAPACCIDTOHTREEITEM = (TV_FIRST + 42),
		TVM_MAPHTREEITEMTOACCID = (TV_FIRST + 43),
		CBEM_INSERTITEMA = (WM_USER + 1),
		CBEM_SETIMAGELIST = (WM_USER + 2),
		CBEM_GETIMAGELIST = (WM_USER + 3),
		CBEM_GETITEMA = (WM_USER + 4),
		CBEM_SETITEMA = (WM_USER + 5),
		CBEM_DELETEITEM = CB_DELETESTRING,
		CBEM_GETCOMBOCONTROL = (WM_USER + 6),
		CBEM_GETEDITCONTROL = (WM_USER + 7),
		CBEM_SETEXTENDEDSTYLE = (WM_USER + 14),
		CBEM_GETEXTENDEDSTYLE = (WM_USER + 9),
		CBEM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		CBEM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		CBEM_SETEXSTYLE = (WM_USER + 8),
		CBEM_GETEXSTYLE = (WM_USER + 9),
		CBEM_HASEDITCHANGED = (WM_USER + 10),
		CBEM_INSERTITEMW = (WM_USER + 11),
		CBEM_SETITEMW = (WM_USER + 12),
		CBEM_GETITEMW = (WM_USER + 13),
		TCM_GETIMAGELIST = (TCM_FIRST + 2),
		TCM_SETIMAGELIST = (TCM_FIRST + 3),
		TCM_GETITEMCOUNT = (TCM_FIRST + 4),
		TCM_GETITEMA = (TCM_FIRST + 5),
		TCM_GETITEMW = (TCM_FIRST + 60),
		TCM_SETITEMA = (TCM_FIRST + 6),
		TCM_SETITEMW = (TCM_FIRST + 61),
		TCM_INSERTITEMA = (TCM_FIRST + 7),
		TCM_INSERTITEMW = (TCM_FIRST + 62),
		TCM_DELETEITEM = (TCM_FIRST + 8),
		TCM_DELETEALLITEMS = (TCM_FIRST + 9),
		TCM_GETITEMRECT = (TCM_FIRST + 10),
		TCM_GETCURSEL = (TCM_FIRST + 11),
		TCM_SETCURSEL = (TCM_FIRST + 12),
		TCM_HITTEST = (TCM_FIRST + 13),
		TCM_SETITEMEXTRA = (TCM_FIRST + 14),
		TCM_ADJUSTRECT = (TCM_FIRST + 40),
		TCM_SETITEMSIZE = (TCM_FIRST + 41),
		TCM_REMOVEIMAGE = (TCM_FIRST + 42),
		TCM_SETPADDING = (TCM_FIRST + 43),
		TCM_GETROWCOUNT = (TCM_FIRST + 44),
		TCM_GETTOOLTIPS = (TCM_FIRST + 45),
		TCM_SETTOOLTIPS = (TCM_FIRST + 46),
		TCM_GETCURFOCUS = (TCM_FIRST + 47),
		TCM_SETCURFOCUS = (TCM_FIRST + 48),
		TCM_SETMINTABWIDTH = (TCM_FIRST + 49),
		TCM_DESELECTALL = (TCM_FIRST + 50),
		TCM_HIGHLIGHTITEM = (TCM_FIRST + 51),
		TCM_SETEXTENDEDSTYLE = (TCM_FIRST + 52),
		TCM_GETEXTENDEDSTYLE = (TCM_FIRST + 53),
		TCM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		TCM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		ACM_OPENA = (WM_USER+100),
		ACM_OPENW = (WM_USER+103),
		ACM_PLAY = (WM_USER+101),
		ACM_STOP = (WM_USER+102),
		MCM_FIRST = 0x1000,
		MCM_GETCURSEL = (MCM_FIRST + 1),
		MCM_SETCURSEL = (MCM_FIRST + 2),
		MCM_GETMAXSELCOUNT = (MCM_FIRST + 3),
		MCM_SETMAXSELCOUNT = (MCM_FIRST + 4),
		MCM_GETSELRANGE = (MCM_FIRST + 5),
		MCM_SETSELRANGE = (MCM_FIRST + 6),
		MCM_GETMONTHRANGE = (MCM_FIRST + 7),
		MCM_SETDAYSTATE = (MCM_FIRST + 8),
		MCM_GETMINREQRECT = (MCM_FIRST + 9),
		MCM_SETCOLOR = (MCM_FIRST + 10),
		MCM_GETCOLOR = (MCM_FIRST + 11),
		MCM_SETTODAY = (MCM_FIRST + 12),
		MCM_GETTODAY = (MCM_FIRST + 13),
		MCM_HITTEST = (MCM_FIRST + 14),
		MCM_SETFIRSTDAYOFWEEK = (MCM_FIRST + 15),
		MCM_GETFIRSTDAYOFWEEK = (MCM_FIRST + 16),
		MCM_GETRANGE = (MCM_FIRST + 17),
		MCM_SETRANGE = (MCM_FIRST + 18),
		MCM_GETMONTHDELTA = (MCM_FIRST + 19),
		MCM_SETMONTHDELTA = (MCM_FIRST + 20),
		MCM_GETMAXTODAYWIDTH = (MCM_FIRST + 21),
		MCM_SETUNICODEFORMAT = CCM_SETUNICODEFORMAT,
		MCM_GETUNICODEFORMAT = CCM_GETUNICODEFORMAT,
		DTM_FIRST = 0x1000,
		DTM_GETSYSTEMTIME = (DTM_FIRST + 1),
		DTM_SETSYSTEMTIME = (DTM_FIRST + 2),
		DTM_GETRANGE = (DTM_FIRST + 3),
		DTM_SETRANGE = (DTM_FIRST + 4),
		DTM_SETFORMATA = (DTM_FIRST + 5),
		DTM_SETFORMATW = (DTM_FIRST + 50),
		DTM_SETMCCOLOR = (DTM_FIRST + 6),
		DTM_GETMCCOLOR = (DTM_FIRST + 7),
		DTM_GETMONTHCAL = (DTM_FIRST + 8),
		DTM_SETMCFONT = (DTM_FIRST + 9),
		DTM_GETMCFONT = (DTM_FIRST + 10),
		PGM_SETCHILD = (PGM_FIRST + 1),
		PGM_RECALCSIZE = (PGM_FIRST + 2),
		PGM_FORWARDMOUSE = (PGM_FIRST + 3),
		PGM_SETBKCOLOR = (PGM_FIRST + 4),
		PGM_GETBKCOLOR = (PGM_FIRST + 5),
		PGM_SETBORDER = (PGM_FIRST + 6),
		PGM_GETBORDER = (PGM_FIRST + 7),
		PGM_SETPOS = (PGM_FIRST + 8),
		PGM_GETPOS = (PGM_FIRST + 9),
		PGM_SETBUTTONSIZE = (PGM_FIRST + 10),
		PGM_GETBUTTONSIZE = (PGM_FIRST + 11),
		PGM_GETBUTTONSTATE = (PGM_FIRST + 12),
		PGM_GETDROPTARGET = CCM_GETDROPTARGET,
		BCM_GETIDEALSIZE = (BCM_FIRST + 0x0001),
		BCM_SETIMAGELIST = (BCM_FIRST + 0x0002),
		BCM_GETIMAGELIST = (BCM_FIRST + 0x0003),
		BCM_SETTEXTMARGIN = (BCM_FIRST + 0x0004),
		BCM_GETTEXTMARGIN = (BCM_FIRST + 0x0005),
		EM_SETCUEBANNER	 = (ECM_FIRST + 1),
		EM_GETCUEBANNER	 = (ECM_FIRST + 2),
		EM_SHOWBALLOONTIP = (ECM_FIRST + 3),
		EM_HIDEBALLOONTIP = (ECM_FIRST + 4), 
		CB_SETMINVISIBLE = (CBM_FIRST + 1),
		CB_GETMINVISIBLE = (CBM_FIRST + 2),
		LM_HITTEST = (WM_USER + 0x300), 
		LM_GETIDEALHEIGHT = (WM_USER + 0x301),
		LM_SETITEM = (WM_USER + 0x302), 
		LM_GETITEM = (WM_USER + 0x303)
	}

    public enum MouseMessages
    {
        WM_CTLCOLORMSGBOX = 0x0132,
        WM_CTLCOLOREDIT = 0x0133,
        WM_CTLCOLORLISTBOX = 0x0134,
        WM_CTLCOLORBTN = 0x0135,
        WM_CTLCOLORDLG = 0x0136,
        WM_CTLCOLORSCROLLBAR = 0x0137,
        WM_CTLCOLORSTATIC = 0x0138,
        MN_GETHMENU = 0x01E1,
        WM_MOUSEFIRST = 0x0200,
        WM_MOUSEMOVE = 0x0200,
        WM_LBUTTONDOWN = 0x0201,
        WM_LBUTTONUP = 0x0202,
        WM_LBUTTONDBLCLK = 0x0203,
        WM_RBUTTONDOWN = 0x0204,
        WM_RBUTTONUP = 0x0205,
        WM_RBUTTONDBLCLK = 0x0206,
        WM_MBUTTONDOWN = 0x0207,
        WM_MBUTTONUP = 0x0208,
        WM_MBUTTONDBLCLK = 0x0209,
        WM_MOUSEHWHEEL = 0x20A
    }

    enum SysCommands : int
    {
        SC_SIZE = 0xF000,
        SC_MOVE = 0xF010,
        SC_MINIMIZE = 0xF020,
        SC_MAXIMIZE = 0xF030,
        SC_NEXTWINDOW = 0xF040,
        SC_PREVWINDOW = 0xF050,
        SC_CLOSE = 0xF060,
        SC_VSCROLL = 0xF070,
        SC_HSCROLL = 0xF080,
        SC_MOUSEMENU = 0xF090,
        SC_KEYMENU = 0xF100,
        SC_ARRANGE = 0xF110,
        SC_RESTORE = 0xF120,
        SC_TASKLIST = 0xF130,
        SC_SCREENSAVE = 0xF140,
        SC_HOTKEY = 0xF150,
        //#if(WINVER >= 0x0400) //Win95
        SC_DEFAULT = 0xF160,
        SC_MONITORPOWER = 0xF170,
        SC_CONTEXTHELP = 0xF180,
        SC_SEPARATOR = 0xF00F,
        //#endif /* WINVER >= 0x0400 */

        //#if(WINVER >= 0x0600) //Vista
        SCF_ISSECURE = 0x00000001,
        //#endif /* WINVER >= 0x0600 */

        /*
          * Obsolete names
          */
        SC_ICON = SC_MINIMIZE,
        SC_ZOOM = SC_MAXIMIZE,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEHOOKSTRUCT
    {
        public Point pt;
        public IntPtr hwnd;
        public uint wHitTestCode;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CBTACTIVATESTRUCT
    {
        public bool fMouse;
        public IntPtr hWndActive;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CREATEWND
    {
        public IntPtr lpcs;
        public IntPtr hwndInsertAfter;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct CREATESTRUCT
    {
        public IntPtr lpCreateParams;
        public IntPtr hInstance;
        public IntPtr hMenu;
        public IntPtr hwndParent;
        public int cy;
        public int cx;
        public int y;
        public int x;
        public int style;
        public IntPtr lpszName;
        public IntPtr lpszClass;
        public int dwExStyle;

        public override string ToString()
        {
            return "X: " + x + ",Y: " + y;
        }
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MOUSEPARAMSTRUCT
    {
        public int RepeatCount;
        public Keys Key;
        public bool ExtendedKey;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public UInt32 message;
        public IntPtr wParam;
        public IntPtr lParam;
        public UInt32 time;
        public POINT pt;
    }

    public enum WindowShowStyle : uint
    {
        /// <summary>Hides the window and activates another window.</summary>
        /// <remarks>See SW_HIDE</remarks>
        Hide = 0,
        /// <summary>Activates and displays a window. If the window is minimized
        /// or maximized, the system restores it to its original size and
        /// position. An application should specify this flag when displaying
        /// the window for the first time.</summary>
        /// <remarks>See SW_SHOWNORMAL</remarks>
        ShowNormal = 1,
        /// <summary>Activates the window and displays it as a minimized window.</summary>
        /// <remarks>See SW_SHOWMINIMIZED</remarks>
        ShowMinimized = 2,
        /// <summary>Activates the window and displays it as a maximized window.</summary>
        /// <remarks>See SW_SHOWMAXIMIZED</remarks>
        ShowMaximized = 3,
        /// <summary>Maximizes the specified window.</summary>
        /// <remarks>See SW_MAXIMIZE</remarks>
        Maximize = 3,
        /// <summary>Displays a window in its most recent size and position.
        /// This value is similar to "ShowNormal", except the window is not
        /// actived.</summary>
        /// <remarks>See SW_SHOWNOACTIVATE</remarks>
        ShowNormalNoActivate = 4,
        /// <summary>Activates the window and displays it in its current size
        /// and position.</summary>
        /// <remarks>See SW_SHOW</remarks>
        Show = 5,
        /// <summary>Minimizes the specified window and activates the next
        /// top-level window in the Z order.</summary>
        /// <remarks>See SW_MINIMIZE</remarks>
        Minimize = 6,
        /// <summary>Displays the window as a minimized window. This value is
        /// similar to "ShowMinimized", except the window is not activated.</summary>
        /// <remarks>See SW_SHOWMINNOACTIVE</remarks>
        ShowMinNoActivate = 7,
        /// <summary>Displays the window in its current size and position. This
        /// value is similar to "Show", except the window is not activated.</summary>
        /// <remarks>See SW_SHOWNA</remarks>
        ShowNoActivate = 8,
        /// <summary>Activates and displays the window. If the window is
        /// minimized or maximized, the system restores it to its original size
        /// and position. An application should specify this flag when restoring
        /// a minimized window.</summary>
        /// <remarks>See SW_RESTORE</remarks>
        Restore = 9,
        /// <summary>Sets the show state based on the SW_ value specified in the
        /// STARTUPINFO structure passed to the CreateProcess function by the
        /// program that started the application.</summary>
        /// <remarks>See SW_SHOWDEFAULT</remarks>
        ShowDefault = 10,
        /// <summary>Windows 2000/XP: Minimizes a window, even if the thread
        /// that owns the window is hung. This flag should only be used when
        /// minimizing windows from a different thread.</summary>
        /// <remarks>See SW_FORCEMINIMIZE</remarks>
        ForceMinimized = 11
    }



    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

        public int X
        {
            get { return Left; }
            set { Right -= (Left - value); Left = value; }
        }

        public int Y
        {
            get { return Top; }
            set { Bottom -= (Top - value); Top = value; }
        }

        public int Height
        {
            get { return Bottom - Top; }
            set { Bottom = value + Top; }
        }

        public int Width
        {
            get { return Right - Left; }
            set { Right = value + Left; }
        }

        public System.Drawing.Point Location
        {
            get { return new System.Drawing.Point(Left, Top); }
            set { X = value.X; Y = value.Y; }
        }

        public System.Drawing.Size Size
        {
            get { return new System.Drawing.Size(Width, Height); }
            set { Width = value.Width; Height = value.Height; }
        }

        public static implicit operator System.Drawing.Rectangle(RECT r)
        {
            return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
        }

        public static implicit operator RECT(System.Drawing.Rectangle r)
        {
            return new RECT(r);
        }

        public static bool operator ==(RECT r1, RECT r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(RECT r1, RECT r2)
        {
            return !r1.Equals(r2);
        }

        public bool Equals(RECT r)
        {
            return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
        }

        public override bool Equals(object obj)
        {
            if (obj is RECT)
                return Equals((RECT)obj);
            else if (obj is System.Drawing.Rectangle)
                return Equals(new RECT((System.Drawing.Rectangle)obj));
            return false;
        }

        public override int GetHashCode()
        {
            return ((System.Drawing.Rectangle)this).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y) { }

        public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }

    public enum HitTest : int
    {
        /// <summary>
        /// On the screen background or on a dividing line between windows (same as HTNOWHERE, except that the DefWindowProc function produces a system beep to indicate an error).
        /// </summary>
        HTERROR = -2,

        /// <summary>
        /// In a window currently covered by another window in the same thread (the message will be sent to underlying windows in the same thread until one of them returns a code that is not HTTRANSPARENT).
        /// </summary>
        HTTRANSPARENT = -1,

        /// <summary>
        /// On the screen background or on a dividing line between windows.
        /// </summary>
        HTNOWHERE = 0,

        /// <summary>
        /// In a client area.
        /// </summary>
        HTCLIENT = 1,

        /// <summary>
        /// In a title bar.
        /// </summary>
        HTCAPTION = 2,

        /// <summary>
        /// In a window menu or in a Close button in a child window.
        /// </summary>
        HTSYSMENU = 3,

        /// <summary>
        /// In a size box (same as HTSIZE).
        /// </summary>
        HTGROWBOX = 4,

        /// <summary>
        /// In a size box (same as HTGROWBOX).
        /// </summary>
        HTSIZE = 4,

        /// <summary>
        /// In a menu.
        /// </summary>
        HTMENU = 5,

        /// <summary>
        /// In a horizontal scroll bar.
        /// </summary>
        HTHSCROLL = 6,

        /// <summary>
        /// In the vertical scroll bar.
        /// </summary>
        HTVSCROLL = 7,

        /// <summary>
        /// In a Minimize button.
        /// </summary>
        HTMINBUTTON = 8,

        /// <summary>
        /// In a Minimize button.
        /// </summary>
        HTREDUCE = 8,

        /// <summary>
        /// In a Maximize button.
        /// </summary>
        HTMAXBUTTON = 9,

        /// <summary>
        /// In a Maximize button.
        /// </summary>
        HTZOOM = 9,

        /// <summary>
        /// In the left border of a resizable window (the user can click the mouse to resize the window horizontally).
        /// </summary>
        HTLEFT = 10,

        /// <summary>
        /// In the right border of a resizable window (the user can click the mouse to resize the window horizontally).
        /// </summary>
        HTRIGHT = 11,

        /// <summary>
        /// In the upper-horizontal border of a window.
        /// </summary>
        HTTOP = 12,

        /// <summary>
        /// In the upper-left corner of a window border.
        /// </summary>
        HTTOPLEFT = 13,

        /// <summary>
        /// In the upper-right corner of a window border.
        /// </summary>
        HTTOPRIGHT = 14,

        /// <summary>
        /// In the lower-horizontal border of a resizable window (the user can click the mouse to resize the window vertically).
        /// </summary>
        HTBOTTOM = 15,

        /// <summary>
        /// In the lower-left corner of a border of a resizable window (the user can click the mouse to resize the window diagonally).
        /// </summary>
        HTBOTTOMLEFT = 16,

        /// <summary>
        /// In the lower-right corner of a border of a resizable window (the user can click the mouse to resize the window diagonally).
        /// </summary>
        HTBOTTOMRIGHT = 17,

        /// <summary>
        /// In the border of a window that does not have a sizing border.
        /// </summary>
        HTBORDER = 18,

        /// <summary>
        /// In a Close button.
        /// </summary>
        HTCLOSE = 20,

        /// <summary>
        /// In a Help button.
        /// </summary>
        HTHELP = 21,
    };

}