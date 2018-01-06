using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;

namespace System.Hooks
{

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public Point pt;
        public int mouseData;
        public int flags;
        public int time;
        public UIntPtr dwExtraInfo;
    }


    /// <summary>
    /// The system calls this function every time a new mouse input event is about to be posted into a thread input queue. It is always global
    /// </summary>
    public class WH_MOUSE_LL : IHook
    {
        public int Code { get; private set; }
        public IntPtr wParam { get; private set; }
        public IntPtr lParam { get; private set; }
        public Process Caller { get; private set; }
        public DateTime Time { get; private set; }

        /// <summary>
        /// The system calls this function every time a new mouse input event is about to be posted into a thread input queue. 
        /// </summary>
        new public const string Description = "The system calls this function every time a new mouse input event is about to be posted into a thread input queue. ";

        /// <summary>
        /// Specifies whether the hook procedure can be intercepted
        /// </summary>
        public override bool InterceptEffective
        {
            get
            {
                return true;
            }
        }


        public INPUT_Messages Attachment
        {
            get
            {
                return (INPUT_Messages)Code;
            }
        }

        /// <summary>
        /// The key currently pressed
        /// </summary>
        public MouseMessages MouseMessage
        {
            get { return (MouseMessages)wParam; }
        }


        /// <summary>
        /// Specifies if this event didnt come from the user
        /// </summary>
        public bool Injected
        {
            get { if (MouseData.flags == 1) { return true; } return false; }
        }

        public MSLLHOOKSTRUCT MouseData
        {
            get { return MarshalHelper.GetStructFromProcess<MSLLHOOKSTRUCT>(Caller, lParam); }
        }

        public override string ToString()
        {
            if (Injected) { return "Injected Message: " + MouseMessage + " @ " + MouseData.pt; }
            return MouseMessage + " @ " + MouseData.pt;
        }

        /// <summary>
        /// Translates Winows message into usable format and extracts all information
        /// </summary>
        public WH_MOUSE_LL(HookArguments Msg) : base(Msg)
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
