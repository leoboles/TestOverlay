using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;


namespace System.Hooks
{
    public static class MarshalHelper
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory( IntPtr hProcess,IntPtr lpBaseAddress,byte[] lpBuffer,int nSize,out IntPtr lpNumberOfBytesWritten);

        const int PROCESS_VM_READ = 0x0010;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;



        public static T WriteStructToProcess<T>(Process Process, IntPtr Address,T Data) where T : struct
        {
            IntPtr ProcessHandle = OpenProcess(PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, Process.Id);

            IntPtr byteswritten = IntPtr.Zero;
            byte[] buffer = SerializeMessage<T>(Data);
            bool Ok = WriteProcessMemory(ProcessHandle, Address, buffer, buffer.Length, out byteswritten);
            if (!Ok) { throw new Win32Exception(Marshal.GetLastWin32Error()); }

            return MarshalHelper.DeserializeMsg<T>(buffer);
        }

        static Byte[] SerializeMessage<T>(T msg) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            Byte[] ret = new Byte[objsize];
            IntPtr buff = Marshal.AllocHGlobal(objsize);
            Marshal.StructureToPtr(msg, buff, true);
            Marshal.Copy(buff, ret, 0, objsize);
            Marshal.FreeHGlobal(buff);
            return ret;
        }

        public static T GetStructFromProcess<T>(Process Process,IntPtr Address) where T:struct
        {
            IntPtr ProcessHandle = OpenProcess(PROCESS_VM_READ, false, Process.Id);

            int bytesrecieved = 0;
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            bool Ok=ReadProcessMemory(ProcessHandle.ToInt32(), Address.ToInt32(), buffer, buffer.Length, ref bytesrecieved);
            if (!Ok) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
            
            return MarshalHelper.DeserializeMsg<T>(buffer);
        }

        static T DeserializeMsg<T>(Byte[] data) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            IntPtr buff = Marshal.AllocHGlobal(objsize);
            Marshal.Copy(data, 0, buff, objsize);
            T retStruct = (T)Marshal.PtrToStructure(buff, typeof(T));
            Marshal.FreeHGlobal(buff);
            return retStruct;
        }


    }


}
