using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Drawing;


namespace System.Hooks
{
    [StructLayout(LayoutKind.Sequential)]
    public struct  DEBUGHOOKINFO
    {
        public int idThread;
        public int idThreadInstaller;
        public IntPtr lParam;
        public IntPtr wParam;
        public int code;
    }


    /// <summary>
    /// The system calls this function before calling the hook procedures associated with any type of hook.
    /// </summary>
    public class WH_DEBUG : IHook
    {
        public int Code { get; private set; }
        public IntPtr wParam { get; private set; }
        public IntPtr lParam { get; private set; }
        public Process Caller { get; private set; }
        public DateTime Time { get; private set; }

        HookArguments msg;

        /// <summary>
        /// Description of this hook
        /// </summary>
        new public const string Description="The system calls this function before calling the hook procedures associated with any type of hook. It can intercept all hooks.";


        /// <summary>
        /// Specifies whether the hook procedure must process the message.
        /// </summary>
        public override bool InterceptEffective
        {
            get
            {
                return true;
            }
        }

        public HookType HookType
        {
            get { return (Hooks.HookType)wParam; }
        }

        object hook;
        public object Hook
        {
            get { return hook; }
        }

        /// <summary>
        /// Message attached to debug hook
        /// </summary>
        public DEBUGHOOKINFO AttachedHook
        {
            get 
            { 
                return MarshalHelper.GetStructFromProcess<DEBUGHOOKINFO>(Caller, lParam);
            }
        }

        public override string ToString()
        {
            return HookType+": "+hook.ToString();
        }


        /// <summary>
        /// Translates Winows message into usable format and extracts all information
        /// </summary>
        public WH_DEBUG(HookArguments Msg) : base (Msg)
        {
            this.Code =   Msg.nCode;
            this.wParam = Msg.wParam;
            this.lParam = Msg.lParam;
            this.Caller = Msg.Process;
            this.Time = Msg.TimeStamp;

            HookArguments RealArg = new HookArguments();
            RealArg.Process = Caller;
            RealArg.TimeStamp = Time;
            RealArg.lParam = AttachedHook.lParam;
            RealArg.wParam = AttachedHook.wParam;
            RealArg.nCode = AttachedHook.code; 

            Type typ=Type.GetType("System.Hooks." + HookType.ToString());
            if (typ!=null)
            {
                hook = Activator.CreateInstance(typ, new object[] { RealArg });
            }
        }

    }
}
