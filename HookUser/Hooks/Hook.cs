using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime;
using System.ComponentModel;
using System.Windows.Interop;
using System.Diagnostics;
using System.Collections;
using System.Threading;
using System.IO;

namespace System.Hooks
{


    class MessageLoop : Form
    {
        int filter = 0;

        public IntPtr hWnd
        {
            get { return base.Handle; }
        }

        public MessageLoop(WindowsMessages Filter)
            : base()
        {
            filter = (int)Filter;

            base.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            base.ShowInTaskbar = false;
            base.StartPosition = FormStartPosition.Manual;
            base.Location = new System.Drawing.Point(-2000, -2000);
            base.Size = new System.Drawing.Size(1, 1);
            base.Show();
        }

        protected override void WndProc(ref Message m)
        {
            bool Intercept = false;

            if (m.Msg == filter && MessageCallback != null)
            {
                MessageCallback(ref m, ref Intercept);
            }

            base.WndProc(ref m);

            if (Intercept)
            {
                m.Result = new IntPtr(1);
            }
        }


        public event dWndProc MessageCallback;
        public delegate void dWndProc(ref Message m, ref bool Intercept);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct AllHookMSG
    {
        public int HookType;

        public int nCode;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint Process;

        public long Time;
        public int MilliSecond;
    }

    /// <summary>
    /// All types of hooks in an enumeration
    /// </summary>
    public enum HookType : int
    {
        // WH_JOURNALRECORD = 0,    missing implementation
        // WH_JOURNALPLAYBACK = 1,  missing implementation
        WH_KEYBOARD = 2,
        WH_GETMESSAGE = 3,
        WH_CALLWNDPROC = 4,
        WH_CBT = 5,
        WH_SYSMSGFILTER = 6,
        WH_MOUSE = 7,
        // WH_HARDWARE = 8, not existent in any win32, placeholder for hardware messages (except mouse)
        WH_DEBUG = 9,
        WH_SHELL = 10,
        WH_FOREGROUNDIDLE = 11,
        WH_CALLWNDPROCRET = 12,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }


    /// <summary>
    /// The CallWndProc wrapper for easy usage. Pointers are not valid outside of HookTriggered!
    /// </summary>
    public class HookArguments : EventArgs
    {
        /// <summary>
        /// A defined code used by the hook procedure to determine how to process the message.
        /// </summary>
        public int nCode;

        /// <summary>
        /// A defined pointer used by the hook procedure to determine how to process the message.
        /// </summary>
        public IntPtr wParam;

        /// <summary>
        /// A defined pointer used by the hook procedure to determine how to process the message.
        /// </summary>
        public IntPtr lParam;

        /// <summary>
        /// The Process from which the hook function was called
        /// </summary>
        public Process Process;

        /// <summary>
        /// Exact time of the hook callback
        /// </summary>
        public DateTime TimeStamp;
    }

    static class HookDll
    {
        [
            DllImport("HookDll.dll", CharSet = CharSet.Auto,
            EntryPoint = "?SetHook@@YGKHHKPAUHWND__@@0@Z",
                          
            ExactSpelling = false, CallingConvention = CallingConvention.StdCall)
        ]
        public static extern uint SetHook(int HookType, bool bInstall, [MarshalAs(UnmanagedType.U4)] UInt32 dwThreadId, IntPtr hWndCaller, IntPtr hWndTarget);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();


        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    }

    public abstract class IHook
    {
        /// <summary>
        /// Interface for all windows messages and hooks
        /// </summary>
        /// <param name="Msg"></param>
        public IHook(HookArguments Msg)
        {
            Code = Msg.nCode;
            wParam = Msg.wParam;
            lParam = Msg.lParam;
            Caller = Msg.Process;
            Time = Msg.TimeStamp;
        }

        /// <summary>
        /// A defined code used by the hook procedure to determine how to process the message. 
        /// </summary>
        public int Code { get; private set; }

        /// <summary>
        /// A defined pointer used by the hook procedure to determine how to process the message.
        /// </summary>
        public IntPtr wParam { get; private set; }

        /// <summary>
        /// A defined pointer used by the hook procedure to determine how to process the message.
        /// </summary>
        public IntPtr lParam { get; private set; }

        /// <summary>
        /// The Process from which the hook function was called
        /// </summary>
        public Process Caller { get; private set; }

        /// <summary>
        /// Exact time of the hook callback
        /// </summary>
        public DateTime Time { get; private set; }

        /// <summary>
        /// Description of the HookType
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Returns wheter the hook can be intercepted or not
        /// </summary>
        public abstract bool InterceptEffective { get; }
    }



    public class Hook
    {
        MessageLoop MessageHandler;
        public HookType HookType;


        /// <summary>
        /// Inserts a low level hook to the specified process
        /// </summary>
        /// <param name="ToWatch">Process to intercept messages</param>
        public Hook(HookType hooktype, Process ToWatch, IntPtr hWndTarget)
        {
            MessageHandler = new MessageLoop(WindowsMessages.WM_COPYDATA);
            MessageHandler.MessageCallback += MessageHandler_WndProc;

            HookType = hooktype;


            uint TID = (uint)ToWatch.Threads[0].Id;

            if (HookType == HookType.WH_SYSMSGFILTER) { TID = 0; }

            uint HookEnabled = HookDll.SetHook((int)HookType, true, TID, MessageHandler.Handle, hWndTarget);
            if (HookEnabled != 0) { throw new Win32Exception((int)HookEnabled); }
        }


        bool Global = false;

        /// <summary>
        /// Insert a low level hook to the current window, or hook to all windows in the system.
        /// </summary>
        /// <param name="Global">Specifies if HookDll is injected to all processes</param>
        public Hook(HookType hooktype, bool Global, IntPtr hWndTarget)
        {
            MessageHandler = new MessageLoop(WindowsMessages.WM_COPYDATA);
            MessageHandler.MessageCallback += MessageHandler_WndProc;

            this.Global = Global;
            HookType = hooktype;
            if (HookType == HookType.WH_SYSMSGFILTER) { Global = true; }


            //Set Hook
            uint HookEnabled = 0;

            if (Global) { HookEnabled = HookDll.SetHook((int)HookType, true, (uint)0, MessageHandler.Handle, hWndTarget); }
            else { HookEnabled = HookDll.SetHook((int)HookType, true, HookDll.GetCurrentThreadId(), MessageHandler.Handle, hWndTarget); }
            if (HookEnabled != 0) { throw new Win32Exception((int)HookEnabled); }
        }

        int CurrentProcessID = Process.GetCurrentProcess().Id;

        void MessageHandler_WndProc(ref Message m, ref bool Intercept)
        {
            if (HookTriggered == null) return;


            var InfoBoat = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT));
            var HookInfo = (AllHookMSG)Marshal.PtrToStructure(InfoBoat.lpData, typeof(AllHookMSG));

            var time = new System.DateTime(1970, 1, 1).AddSeconds(HookInfo.Time).ToLocalTime().AddMilliseconds(HookInfo.MilliSecond);
            var process = Process.GetProcessById((int)HookInfo.Process);


            var PassData = new HookArguments();
            PassData.lParam = HookInfo.lParam;
            PassData.wParam = HookInfo.wParam;
            PassData.nCode = HookInfo.nCode;
            PassData.Process = process;
            PassData.TimeStamp = time;

            // Filter own copydata communication
            if (HookInfo.HookType == (int)HookType.WH_CALLWNDPROC)
            {
                CWPSTRUCT IsWMCOPY = MarshalHelper.GetStructFromProcess<CWPSTRUCT>(process, PassData.lParam);
                if (IsWMCOPY.message == (int)WindowsMessages.WM_COPYDATA) { return; }
            }

            if (HookInfo.HookType == (int)HookType.WH_CALLWNDPROCRET)
            {
                CWPRETSTRUCT IsWMCOPY = MarshalHelper.GetStructFromProcess<CWPRETSTRUCT>(process, PassData.lParam);
                if (IsWMCOPY.message == (int)WindowsMessages.WM_COPYDATA) { return; }
            }

            HookTriggered(PassData, ref Intercept);

            if (Intercept == true && HookType == System.Hooks.HookType.WH_GETMESSAGE)
            {
                var Returner = new WH_GETMESSAGE(PassData);
                Returner.Message = Message.Create(Returner.Caller.MainWindowHandle, 0, IntPtr.Zero, IntPtr.Zero);
            }
        }

        /// <summary>
        /// Returns completely translated hook callback messages
        /// </summary>
        public event HookProcCallback HookTriggered;

        /// <summary>
        /// Returns completely translated hook callback messages
        /// </summary>
        /// <param name="Message">Message Argument. Can be supplied to the constructor of right WM_... classes </param>
        /// <param name="Intercept">Specifies if the next hook in the queue should be called or not,may cause system instability! </param>
        public delegate void HookProcCallback(HookArguments Msg, ref bool Intercept);

        /// <summary>
        /// Unhooks the hook and disposes the messageloop
        /// </summary>
        public void Dispose()
        {
            HookDll.SetHook(0, false, 0, IntPtr.Zero, IntPtr.Zero);
            MessageHandler.MessageCallback -= MessageHandler_WndProc;
            MessageHandler.Close();
            MessageHandler.Dispose();
        }
    }

    /// <summary>
    /// A hook is a mechanism by which an application can intercept events, such as messages, mouse actions, and keystrokes. This is a generic windows hook wrapper for all windows hooks. T is a member of WH_?
    /// </summary>
    public class Hook<T> where T : IHook
    {
        MessageLoop MessageHandler;

        /// <summary>
        /// Inserts a low level hook to the specified process
        /// </summary>
        /// <param name="ToWatch">Process to intercept messages</param>
        public Hook(Process ToWatch, IntPtr hWndTarget)
        {
            MessageHandler = new MessageLoop(WindowsMessages.WM_COPYDATA);
            MessageHandler.MessageCallback += MessageHandler_WndProc;

            var hookType = (HookType)Enum.Parse(typeof(HookType), typeof(T).Name);


            uint TID = (uint)ToWatch.Threads[0].Id;

            if (hookType == HookType.WH_SYSMSGFILTER) { TID = 0; }

            uint HookEnabled = HookDll.SetHook((int)hookType, true, TID, MessageHandler.Handle, hWndTarget);
            if (HookEnabled != 0) { throw new Win32Exception((int)HookEnabled); }
        }


        bool Global = false;

        /// <summary>
        /// Insert a low level hook to the current window, or hook to all windows in the system.
        /// </summary>
        /// <param name="Global">Specifies if HookDll is injected to all processes</param>
        public Hook(bool Global, IntPtr hWndTarget)
        {
            MessageHandler = new MessageLoop(WindowsMessages.WM_COPYDATA);
            MessageHandler.MessageCallback += MessageHandler_WndProc;

            this.Global = Global;
            var hookType = (HookType)Enum.Parse(typeof(HookType), typeof(T).Name);
            if (hookType == HookType.WH_SYSMSGFILTER) { Global = true; }


            //Set Hook
            uint HookEnabled = 0;

            if (Global) { HookEnabled = HookDll.SetHook((int)hookType, true, (uint)0, MessageHandler.Handle, hWndTarget); }
            else { HookEnabled = HookDll.SetHook((int)hookType, true, HookDll.GetCurrentThreadId(), MessageHandler.Handle, hWndTarget); }
            if (HookEnabled != 0) { throw new Win32Exception((int)HookEnabled); }
        }

        int CurrentProcessID = Process.GetCurrentProcess().Id;

        void MessageHandler_WndProc(ref Message m, ref bool Intercept)
        {
            if (HookTriggered == null) return;


            var InfoBoat = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT));
            var HookInfo = (AllHookMSG)Marshal.PtrToStructure(InfoBoat.lpData, typeof(AllHookMSG));

            var time = new System.DateTime(1970, 1, 1).AddSeconds(HookInfo.Time).ToLocalTime().AddMilliseconds(HookInfo.MilliSecond);
            var process = Process.GetProcessById((int)HookInfo.Process);


            var PassData = new HookArguments();
            PassData.lParam = HookInfo.lParam;
            PassData.wParam = HookInfo.wParam;
            PassData.nCode = HookInfo.nCode;
            PassData.Process = process;
            PassData.TimeStamp = time;

            // Filter own copydata communication
            if (HookInfo.HookType == (int)HookType.WH_CALLWNDPROC)
            {
                CWPSTRUCT IsWMCOPY = MarshalHelper.GetStructFromProcess<CWPSTRUCT>(process, PassData.lParam);
                if (IsWMCOPY.message == (int)WindowsMessages.WM_COPYDATA) { return; }
            }

            if (HookInfo.HookType == (int)HookType.WH_CALLWNDPROCRET)
            {
                CWPRETSTRUCT IsWMCOPY = MarshalHelper.GetStructFromProcess<CWPRETSTRUCT>(process, PassData.lParam);
                if (IsWMCOPY.message == (int)WindowsMessages.WM_COPYDATA) { return; }
            }

            //Create translated Hook
            T ret = (T)Activator.CreateInstance(typeof(T), new object[] { PassData });

            HookTriggered(ret, ref Intercept);

            if (Intercept == true && (ret as WH_GETMESSAGE) != null)
            {
                var Returner = (ret as WH_GETMESSAGE);
                Returner.Message = Message.Create(Returner.Caller.MainWindowHandle, 0, IntPtr.Zero, IntPtr.Zero);
            }
        }

        /// <summary>
        /// Returns completely translated hook callback messages
        /// </summary>
        public event HookProcCallback HookTriggered;

        /// <summary>
        /// Returns completely translated hook callback messages
        /// </summary>
        /// <param name="Message">All readable information in a specific hook</param>
        /// <param name="Intercept">Specifies if the next hook in the queue should be called or not,may cause system instability! </param>
        public delegate void HookProcCallback(T Message, ref bool Intercept);

        /// <summary>
        /// Unhooks the hook and disposes the messageloop
        /// </summary>
        public void Dispose()
        {
            HookDll.SetHook(0, false, 0, IntPtr.Zero, IntPtr.Zero);
            MessageHandler.Dispose();
        }

    }
}
