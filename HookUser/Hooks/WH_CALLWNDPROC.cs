using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace System.Hooks
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CWPSTRUCT
    {
        public IntPtr lParam;
        public IntPtr wParam;
        public int message;
        public IntPtr hwnd;
    }

    /// <summary>
    /// The system calls this function before calling the window procedure to process a message sent to the thread.
    /// </summary>
    public class WH_CALLWNDPROC : IHook
    {
        public int Code         { get; private set; }
        public IntPtr wParam    { get; private set; }
        public IntPtr lParam    { get; private set; }
        public Process Caller   { get; private set; }
        public DateTime Time    { get; private set; }

        public CWPSTRUCT Attachment { get; private set; }



        /// <summary>
        /// Description of WH_CALLWNDPROC
        /// </summary>
        new public const string Description = "The system calls this function before calling the window procedure to process a message sent to the thread.";

        /// <summary>
        /// Remarks of WH_CALLWNDPROC
        /// </summary>
        public string Remark="The CallWndProc hook procedure can examine the message, but it cannot modify it, use WH_GETMESSAGE to modify";


        /// <summary>
        /// Specifies whether the hook intercept can intercept
        /// </summary>
        public override bool InterceptEffective
        {
            get
            {
                return false;
            }
        }
        

        public Message Message
        {
            get { return Message.Create(Attachment.hwnd, Attachment.message, Attachment.wParam, Attachment.lParam); }
            private set { }
        }


        public override string ToString()
        {
            string MSG = Enum.GetName(typeof(WindowsMessages), Message.Msg);

            return Caller.ProcessName + " is about to recieve " + Message + " @ " + Time.ToString("dd/MM/yyyy hh:mm:ss.fff");
        }
        

        /// <summary>
        /// Translates Winows message into usable format and extracts all information
        /// </summary>
        public WH_CALLWNDPROC(HookArguments Msg) : base(Msg)
        {
            if (Msg == null) { return; }

            this.Code = Msg.nCode;
            this.wParam = Msg.wParam;
            this.lParam = Msg.lParam;
            this.Caller = Msg.Process;
            this.Time = Msg.TimeStamp;

            Attachment = MarshalHelper.GetStructFromProcess<CWPSTRUCT>(Caller, lParam);
        }

    }
}
