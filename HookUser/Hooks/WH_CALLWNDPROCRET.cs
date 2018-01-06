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
    public struct CWPRETSTRUCT
    {
        public IntPtr lResult;
        public IntPtr lParam;
        public IntPtr wParam;
        public uint message;
        public IntPtr hWnd;
    }


    /// <summary>
    /// The system calls this function after calling the window procedure to process a message sent to the thread.
    /// </summary>
    public class WH_CALLWNDPROCRET : IHook
    {
        public int Code         { get; private set; }
        public IntPtr wParam    { get; private set; }
        public IntPtr lParam    { get; private set; }
        public Process Caller   { get; private set; }
        public DateTime Time    { get; private set; }

        public CWPRETSTRUCT Attachment { get; private set; }



        /// <summary>
        /// Description of WH_CALLWNDPROC
        /// </summary>
        new public const string Description = "The system calls this function after calling the window procedure to process a message sent to the thread.";


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
            get { return Message.Create(Attachment.hWnd, (int)Attachment.message, Attachment.wParam, Attachment.lParam); }
            private set { }
        }


        public override string ToString()
        {
            string MSG = Enum.GetName(typeof(WindowsMessages), Message.Msg);

            return Caller.ProcessName + " recieved " + MSG + " and returned "+Attachment.lResult+" @ " + Time.ToString("dd/MM/yyyy hh:mm:ss.fff");
        }
        

        /// <summary>
        /// Translates Winows message into usable format and extracts all information
        /// </summary>
        public WH_CALLWNDPROCRET(HookArguments Msg) : base(Msg)
        {
            this.Code = Msg.nCode;
            this.wParam = Msg.wParam;
            this.lParam = Msg.lParam;
            this.Caller = Msg.Process;
            this.Time = Msg.TimeStamp;


            Attachment= MarshalHelper.GetStructFromProcess<CWPRETSTRUCT>(Caller, lParam);
        }
    }
}
