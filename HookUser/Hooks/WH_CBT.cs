using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Collections;
using System.Reflection;

namespace System.Hooks
{


    public enum CBT_Messages : int
    {
        HCBT_MOVESIZE = 0,
        HCBT_MINMAX = 1,
        HCBT_QS = 2,
        HCBT_CREATEWND = 3,
        HCBT_DESTROYWND = 4,
        HCBT_ACTIVATE = 5,
        HCBT_CLICKSKIPPED = 6,
        HCBT_KEYSKIPPED = 7,
        HCBT_SYSCOMMAND = 8,
        HCBT_SETFOCUS = 9
    }
    /// <summary>
    /// The system calls this function before activating, creating, destroying, minimizing, maximizing, moving, or sizing a window; 
    /// before completing a system command; before removing a mouse or keyboard event from the system message queue; 
    /// before setting the keyboard focus; or before synchronizing with the system message queue.
    /// </summary>
    public class WH_CBT : IHook
    {
        public int Code { get; private set; }
        public IntPtr wParam { get; private set; }
        public IntPtr lParam { get; private set; }
        public Process Caller { get; private set; }
        public DateTime Time { get; private set; }


        /// <summary>
        /// Description of WH_CBT
        /// </summary>
        new public const string Description = "The system calls this function before activating, creating, destroying, minimizing, maximizing, moving, or sizing a window; before completing a system command; before removing a mouse or keyboard event from the system message queue; before setting the keyboard focus; or before synchronizing with the system message queue.";


        /// <summary>
        /// The message. 
        /// </summary>
        public CBT_Messages Message
        {
            get
            {
                return (CBT_Messages)Code;
            }
            private set { }
        }


        CREATESTRUCT getCreateStruct()
        {
            var CreateWindow = MarshalHelper.GetStructFromProcess<CREATEWND>(Caller, lParam);

            return MarshalHelper.GetStructFromProcess<CREATESTRUCT>(Caller, CreateWindow.lpcs);
        }

        static Boolean[] ToBooleanArray(Int32 i)
        {
            BitArray b = new BitArray(new int[] { i });

            bool[] bits = new bool[b.Count];
            b.CopyTo(bits, 0);

            return bits;
        }

        MOUSEPARAMSTRUCT getMouseBitParams()
        {
            MOUSEPARAMSTRUCT kmap = new MOUSEPARAMSTRUCT();

            int x = lParam.ToInt32();
            var bits = ToBooleanArray(x).ToList();

            int[] Nr = new Int32[] { 0, 0, 0 };

            new BitArray(bits.GetRange(0, 15).ToArray()).CopyTo(Nr, 0);
            new BitArray(bits.GetRange(24, 1).ToArray()).CopyTo(Nr, 1);
            new BitArray(bits.GetRange(16, 7).ToArray()).CopyTo(Nr, 2);


            kmap.RepeatCount = Nr[0];
            kmap.ExtendedKey = Convert.ToBoolean(Nr[1]);
            kmap.Key = (Keys)wParam;


            return kmap;
        }


        /// <summary>
        /// Specifies whether the hook procedure can be intercepted
        /// </summary>
        public override bool InterceptEffective
        {
            get
            {
                if (Code == 7 || Code == 6 || Code == 2)
                {
                    return false;
                }
                return true;
            }
        }

        public object Attachment1
        {
            get
            {
                switch(Message)
                {
                    case CBT_Messages.HCBT_ACTIVATE:        return new Win32Window(wParam);
                    case CBT_Messages.HCBT_CLICKSKIPPED:    return (WindowsMessages)wParam;
                    case CBT_Messages.HCBT_CREATEWND:       return new Win32Window(wParam);
                    case CBT_Messages.HCBT_DESTROYWND:      return new Win32Window(wParam);
                    case CBT_Messages.HCBT_KEYSKIPPED:      return (Keys)wParam;
                    case CBT_Messages.HCBT_MINMAX:          return new Win32Window(wParam);
                    case CBT_Messages.HCBT_MOVESIZE:        return new Win32Window(wParam);
                    case CBT_Messages.HCBT_SETFOCUS:        return new Win32Window(wParam);
                    case CBT_Messages.HCBT_SYSCOMMAND:      return (SysCommands)wParam;
                    default:                                return null;
                }

            }
            private set { }
        }

        



        public object Attachment2
        {
            get
            {
                switch (Message)
                {
                    case CBT_Messages.HCBT_ACTIVATE:            return MarshalHelper.GetStructFromProcess<CBTACTIVATESTRUCT>(Caller, lParam);
                    case CBT_Messages.HCBT_CLICKSKIPPED:        return MarshalHelper.GetStructFromProcess<MOUSEHOOKSTRUCT>(Caller, lParam);
                    case CBT_Messages.HCBT_CREATEWND:           return getCreateStruct();
                    case CBT_Messages.HCBT_DESTROYWND:          return 0;
                    case CBT_Messages.HCBT_KEYSKIPPED:          return getMouseBitParams();
                    case CBT_Messages.HCBT_MINMAX:              return (WindowShowStyle)BitConverter.ToInt16(BitConverter.GetBytes(lParam.ToInt32()), 0);
                    case CBT_Messages.HCBT_MOVESIZE:            return MarshalHelper.GetStructFromProcess<RECT>(Caller, lParam);
                    case CBT_Messages.HCBT_SETFOCUS:            return new Win32Window(wParam);
                    case CBT_Messages.HCBT_SYSCOMMAND:          return 0;
                    default: return null;
                }

            }
            private set { }
        }


        public override string ToString()
        {
            switch (Message)
            {
                case CBT_Messages.HCBT_ACTIVATE: return Message+ ": Window : " + Attachment1 + " gets activated. Mause caused this="+((CBTACTIVATESTRUCT)Attachment2).fMouse;
                case CBT_Messages.HCBT_CLICKSKIPPED: return Message + ": " + Attachment1 + " event at: " + ((MOUSEHOOKSTRUCT)Attachment2).pt + " above: " + (HitTest)((MOUSEHOOKSTRUCT)Attachment2).wHitTestCode;
                case CBT_Messages.HCBT_CREATEWND: return Message + ": Creates a new window at: " + Attachment2.ToString();
                case CBT_Messages.HCBT_DESTROYWND: return Message + ": Window : " + Attachment1 + " is about to get destroyed";
                case CBT_Messages.HCBT_KEYSKIPPED: return Message + ": Pressed " + Attachment1;
                case CBT_Messages.HCBT_MINMAX: return Message + ": Window : " + Attachment1 + "is about to : " + (WindowShowStyle)(Attachment2);
                case CBT_Messages.HCBT_MOVESIZE: return Message + ": Window : " + Attachment1 + " RECT will change: " + Attachment2;
                case CBT_Messages.HCBT_SETFOCUS: return Message + ": Window : " + Attachment1 + " will gain focus, " + Attachment2 + " window looses it";
                case CBT_Messages.HCBT_SYSCOMMAND: return Message + ": " + Attachment1;
            }

            return "Unnokn Message";
        }


        /// <summary>
        /// Translates Winows message into usable format and extracts all information
        /// </summary>
        public WH_CBT(HookArguments Msg) : base(Msg)
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
