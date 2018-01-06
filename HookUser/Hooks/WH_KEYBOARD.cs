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
    public enum INPUT_Messages : int
    {
        HC_ACTION = 0x0000,
        HC_NOREMOVE = 0x0003
    }

    /// <summary>
    /// The system calls this function whenever an application calls the GetMessage or PeekMessage function and there is a keyboard message (WM_KEYUP or WM_KEYDOWN) to be processed.
    /// </summary>
    public class WH_KEYBOARD : IHook
    {
        public int Code { get; private set; }
        public IntPtr wParam { get; private set; }
        public IntPtr lParam { get; private set; }
        public Process Caller { get; private set; }
        public DateTime Time { get; private set; }

        /// <summary>
        /// The system calls this function whenever an application calls the GetMessage or PeekMessage function and there is a keyboard message (WM_KEYUP or WM_KEYDOWN) to be processed.
        /// </summary>
        new public const string Description = "The system calls this function whenever an application calls the GetMessage or PeekMessage function and there is a keyboard message (WM_KEYUP or WM_KEYDOWN) to be processed. ";

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
        public Key Key
        {
            get { return KeyInterop.KeyFromVirtualKey(wParam.ToInt32()); }
        }

        /// <summary>
        /// Specifies if the key goes up or down
        /// </summary>
        public bool KeyIsDown
        {
            get { return !Convert.ToBoolean(lParam.ToInt32() & (1 << 30));}
        }


        public override string ToString()
        {
            if (KeyIsDown == true) { return "User Pressed: " + Key + " @ " + Time.ToString("hh:mm:ss.fff") + " in " + Caller.ProcessName; }
            else { return "User Released: " + Key + " @ " + Time.ToString("hh:mm:ss.fff") + " in " + Caller.ProcessName; }
        }



        /// <summary>
        /// Translates Winows message into usable format and extracts all information
        /// </summary>
        public WH_KEYBOARD(HookArguments Msg)
            : base(Msg)
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
