using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace System.Hooks
{


    public enum SHELL_Messages:int
    {
        HSHELL_ACCESSIBILITYSTATE=11,
        HSHELL_ACTIVATESHELLWINDOW=3,
        HSHELL_APPCOMMAND=12,
        HSHELL_GETMINRECT=5,
        HSHELL_LANGUAGE=8,
        HSHELL_REDRAW=6,
        HSHELL_TASKMAN = 7,
        HSHELL_WINDOWACTIVATED=4,
        HSHELL_WINDOWCREATED=1,
        HSHELL_WINDOWDESTROYED=2,
        HSHELL_WINDOWREPLACED=13,
    }

    public enum WM_APPCOMMANDS:int
    {
        None,
        APPCOMMAND_BROWSER_BACKWARD,
        APPCOMMAND_BROWSER_FORWARD,
        APPCOMMAND_BROWSER_REFRESH,
        APPCOMMAND_BROWSER_STOP,
        APPCOMMAND_BROWSER_SEARCH,
        APPCOMMAND_BROWSER_FAVORITES,
        APPCOMMAND_BROWSER_HOME,
        APPCOMMAND_VOLUME_MUTE,
        APPCOMMAND_VOLUME_DOWN,
        APPCOMMAND_VOLUME_UP,
        APPCOMMAND_MEDIA_NEXTTRACK,
        APPCOMMAND_MEDIA_PREVIOUSTRACK,
        APPCOMMAND_MEDIA_STOP,
        APPCOMMAND_MEDIA_PLAY_PAUSE,
        APPCOMMAND_LAUNCH_MAIL,
        APPCOMMAND_LAUNCH_MEDIA_SELECT,
        APPCOMMAND_LAUNCH_APP1,
        APPCOMMAND_LAUNCH_APP2,
        APPCOMMAND_BASS_DOWN,
        APPCOMMAND_BASS_BOOST,
        APPCOMMAND_BASS_UP,
        APPCOMMAND_TREBLE_DOWN,
        APPCOMMAND_TREBLE_UP,
        APPCOMMAND_MICROPHONE_VOLUME_MUTE,
        APPCOMMAND_MICROPHONE_VOLUME_DOWN,
        APPCOMMAND_MICROPHONE_VOLUME_UP,
        APPCOMMAND_HELP,
        APPCOMMAND_FIND,
        APPCOMMAND_NEW,
        APPCOMMAND_OPEN,
        APPCOMMAND_CLOSE,
        APPCOMMAND_SAVE,
        APPCOMMAND_PRINT,
        APPCOMMAND_UNDO,
        APPCOMMAND_REDO,
        APPCOMMAND_COPY,
        APPCOMMAND_CUT,
        APPCOMMAND_PASTE,
        APPCOMMAND_REPLY_TO_MAIL,
        APPCOMMAND_FORWARD_MAIL,
        APPCOMMAND_SEND_MAIL,
        APPCOMMAND_SPELL_CHECK,
        APPCOMMAND_DICTATE_OR_COMMAND_CONTROL_TOGGLE,
        APPCOMMAND_MIC_ON_OFF_TOGGLE,
        APPCOMMAND_CORRECTION_LIST,
        APPCOMMAND_MEDIA_PLAY,
        APPCOMMAND_MEDIA_PAUSE,
        APPCOMMAND_MEDIA_RECORD,
        APPCOMMAND_MEDIA_FAST_FORWARD,
        APPCOMMAND_MEDIA_REWIND,
        APPCOMMAND_MEDIA_CHANNEL_UP,
        APPCOMMAND_MEDIA_CHANNEL_DOWN
    }

    /// <summary>
    /// The system calls a WH_SHELL hook procedure when the shell application is about to be activated and when a top-level window is created or destroyed. 
    /// </summary>
    public class WH_SHELL : IHook
    {

        public int Code { get; private set; }
        public IntPtr wParam { get; private set; }
        public IntPtr lParam { get; private set; }
        public Process Caller { get; private set; }
        public DateTime Time { get; private set; }


        /// <summary>
        /// The system calls a WH_SHELL hook procedure when the shell application is about to be activated and when a top-level window is created or destroyed.
        /// </summary>
        new public const string Description = "The system calls a WH_SHELL hook procedure when the shell application is about to be activated and when a top-level window is created or destroyed.";

        /// <summary>
        /// Specifies whether the hook procedure can be intercepted
        /// </summary>
        public override bool InterceptEffective
        {
            get { return false; }
        }

        public SHELL_Messages Message
        {
            get
            {
                return (SHELL_Messages)Code;
            }
            private set { }
        }


        public Win32Window Attachment1
        {
            get
            {
                switch (Message)
                {
                    case SHELL_Messages.HSHELL_ACCESSIBILITYSTATE: return null;
                    default: return new Win32Window(wParam);
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
                    case SHELL_Messages.HSHELL_APPCOMMAND: return (WM_APPCOMMANDS)lParam;
                    case SHELL_Messages.HSHELL_GETMINRECT: return  MarshalHelper.GetStructFromProcess<RECT>(Caller, lParam);
                    case SHELL_Messages.HSHELL_LANGUAGE: return null;
                    case SHELL_Messages.HSHELL_REDRAW: return Convert.ToBoolean(lParam.ToInt32());
                    case SHELL_Messages.HSHELL_WINDOWACTIVATED: return Convert.ToBoolean(lParam.ToInt32());
                    case SHELL_Messages.HSHELL_WINDOWREPLACED: return new Win32Window(lParam);
                    default: return null;
                }
            }
            private set { }
        }


        public override string ToString()
        {
            string retval = Message + ": ";

            if (Attachment2 != null)
            {
                retval += Attachment2;
            }
            if (Attachment1!=null)
            {
                retval += " on " + Attachment1;
            }
            
            return retval;
        }



        /// <summary>
        /// Translates Winows message into usable format and extracts all information
        /// </summary>
        public WH_SHELL(HookArguments Msg) : base(Msg)
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
