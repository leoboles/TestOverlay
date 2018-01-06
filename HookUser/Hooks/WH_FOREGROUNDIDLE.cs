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

    /// <summary>
    /// The system calls this function before calling the window procedure to process a message sent to the thread.
    /// </summary>
    public class WH_FOREGROUNDIDLE : IHook
    {
        public int Code { get; private set; }
        public IntPtr wParam { get; private set; }
        public IntPtr lParam { get; private set; }
        public Process Caller { get; private set; }
        public DateTime Time { get; private set; }


        /// <summary>
        /// The system calls this function whenever the foreground thread is about to become idle. 
        /// </summary>
        new public const string Description = "The system calls this function whenever the foreground thread is about to become idle. ";

        /// <summary>
        /// Message.
        /// </summary>
        public Message Message
        {
            get { return Message.Create(IntPtr.Zero,Code,wParam,lParam); }
            private set { }
        }

        public override string ToString()
        {
            return "Gui Thread " + new Win32Window(Caller.MainWindowHandle).GUIthread.Id + " in "+Caller.ProcessName+" is about to go Idle";
        }

        /// <summary>
        /// Specifies whether the hook procedure can be intercepted
        /// </summary>
        public override bool InterceptEffective
        {
            get
            {
                return false;
            }
        }


        /// <summary>
        /// Translates Winows message into usable format and extracts all information
        /// </summary>
        public WH_FOREGROUNDIDLE(HookArguments Msg) : base (Msg)
        {
            if (Msg == null) { return; }

            this.Code = Msg.nCode;
            this.wParam = Msg.wParam;
            this.lParam = Msg.lParam;
            this.Caller = Msg.Process;
            this.Time = Msg.TimeStamp;
        }

    }
}
