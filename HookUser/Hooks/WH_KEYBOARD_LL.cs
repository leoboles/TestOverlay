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
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public KBDLLHOOKSTRUCTFlags flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [Flags]
    public enum KBDLLHOOKSTRUCTFlags : uint
    {
        LLKHF_EXTENDED = 0x01,
        LLKHF_INJECTED = 0x10,
        LLKHF_ALTDOWN = 0x20,
        LLKHF_UP = 0x80,
    }

    /// <summary>
    /// The system calls this function every time a new keyboard input event is about to be posted into a thread input queue.
    /// </summary>
    public class WH_KEYBOARD_LL : IHook
    {
        public int Code { get; private set; }
        public IntPtr wParam { get; private set; }
        public IntPtr lParam { get; private set; }
        public Process Caller { get; private set; }
        public DateTime Time { get; private set; }

        /// <summary>
        /// The system calls this function every time a new keyboard input event is about to be posted into a thread input queue.
        /// </summary>
        new public const string Description = "The system calls this function every time a new keyboard input event is about to be posted into a thread input queue.";

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

        /// <summary>
        /// Message attached to this event
        /// </summary>
        public WindowsMessages Attachment
        {
            get
            {
                return (WindowsMessages)Code;
            }

        }


        /// <summary>
        /// The key currently pressed
        /// </summary>
        public Key Key
        {
            get { return KeyInterop.KeyFromVirtualKey((int)KeyBoardData.vkCode); }
        }

        /// <summary>
        /// The key currently pressed
        /// </summary>
        public KBDLLHOOKSTRUCT KeyBoardData
        {
            get { return MarshalHelper.GetStructFromProcess<KBDLLHOOKSTRUCT>(Caller, lParam); }
        }

        /// <summary>
        /// Specifies if the key goes up or down
        /// </summary>
        public bool KeyIsDown
        {
            get { return !Convert.ToBoolean(KeyBoardData.flags.HasFlag(KBDLLHOOKSTRUCTFlags.LLKHF_UP));}
        }


        public override string ToString()
        {
            if (KeyIsDown == true) { return "User Pressed: " + Key + " @ " + Time.ToString("hh:mm:ss.fff"); }
            else { return "User Released: " + Key + " @ " + Time.ToString("hh:mm:ss.fff"); }
        }



        /// <summary>
        /// Translates Winows message into usable format and extracts all information
        /// </summary>
        public WH_KEYBOARD_LL(HookArguments Msg)
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
