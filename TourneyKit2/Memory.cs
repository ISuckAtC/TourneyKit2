using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace TourneyKit2
{
    public struct tagPOINT
    {
        public int X;
        public int Y;
    }
    public struct WindowsMessage
    {
        public long hwnd;
        public uint message;
        public uint wParam;
        public ulong lParam;
        public ulong time;
        public tagPOINT pt;
        public ulong lPrivate;
    }
    public static class Memory
    {
        #region Dll
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
        uint processAccess, 
        bool bInheritHandle, 
        int processId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [Out] byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool GetMessage(out WindowsMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("User32.dll", SetLastError = true)]
        static extern short GetKeyState(int nVirtKey);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern long GetWindowLong(
        IntPtr handle,
        int nIndex
        );

        [DllImport("User32.dll", SetLastError = true)]
        public static extern long SetWindowLong(
        IntPtr handle,
        int nIndex,
        long dwNewLong
        );

        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
        );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(
        IntPtr hProcess,
        IntPtr lpThreadAttributes,
        uint dwStackSize,
        IntPtr lpStartAddress,
        IntPtr lpParameter,
        uint dwCreationFlags,
        out IntPtr lpThreadId
        );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect
        );
        #endregion

        #region BasePointers

        static public IntPtr GameDataMan;
        static public IntPtr WorldChrMan;
        static public IntPtr GameMan;
        static public IntPtr FieldArea;
        static public IntPtr LockTgtMan;
        #endregion

        static public IntPtr DS3Process;
        static public ProcessModule DS3Module;
        static public int Ds3ProcessId;
        static public int lastErr;

        static private IntPtr DS3BaseSetup(IntPtr start)
        {
            return new IntPtr(start.ToInt64() + BitConverter.ToInt32(ReadMem(new IntPtr(start.ToInt64() + 3), 4, 24)) + 7);
        }

        public static List<int> chilledCallers = new List<int>();

        static public void SetBases()
        {
            //GameDataMan = AOBScan("48 8B 05 ?? ?? ?? ?? 48 85 C0 ?? ?? 48 8b 40 ?? C3")[0];
            //GameDataMan = DS3BaseSetup(GameDataMan);
            //Console.WriteLine("GameDataMan: " + System.Convert.ToHexString(BitConverter.GetBytes((long)GameDataMan)));

            WorldChrMan = AOBScan("48 8B 1D ?? ?? ?? 04 48 8B F9 48 85 DB ?? ?? 8B 11 85 D2 ?? ?? 8D")[0];
            WorldChrMan = DS3BaseSetup(WorldChrMan);
            Console.WriteLine("WorldChrMan: " + System.Convert.ToHexString(BitConverter.GetBytes((long)WorldChrMan)));

            //GameMan = AOBScan("48 8B 05 ?? ?? ?? ?? 48 8B 80 60 0C 00 00")[0];
            //GameMan = DS3BaseSetup(GameMan);
            //Console.WriteLine("GameMan: " + System.Convert.ToHexString(BitConverter.GetBytes((long)GameMan)));

            //FieldArea = AOBScan("48 8B 0D ?? ?? ?? ?? 48 85 C9 74 26 44 8B")[0];
            //FieldArea = DS3BaseSetup(FieldArea);
            //Console.WriteLine("FieldArea: " + System.Convert.ToHexString(BitConverter.GetBytes((long)FieldArea)));

            //LockTgtMan = AOBScan("48 8B 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 0F 84 ?? ?? ?? ?? C7")[0];
            //LockTgtMan = DS3BaseSetup(LockTgtMan);
            //Console.WriteLine("LockTgtMan: " + System.Convert.ToHexString(BitConverter.GetBytes((long)LockTgtMan)));
        }

        static public byte[] ReadMem(IntPtr baseAdd, int size, int caller = 0)
        {
            byte[] buf = new byte[size];
            IntPtr bRead = new IntPtr();
            ReadProcessMemory(DS3Process, baseAdd, buf, size, out bRead);
            lastErr = Marshal.GetLastWin32Error();
            if (lastErr != 0)
            {
                Console.WriteLine("ERROR: " + lastErr + " | caller: " + caller);
                if (lastErr == 6 || lastErr == 299)
                {
                    //DS3Process = OpenProcess(0x001F0FFF, false, Ds3ProcessId);
                    if (!chilledCallers.Contains(caller))
                    {
                        Console.WriteLine("Entering chill zone");
                        chilledCallers.Add(caller);
                        Thread chillout = new Thread(() =>
                        {
                            Thread.Sleep(2000);
                            Console.WriteLine("Exiting chill zone");
                            chilledCallers.Remove(caller);
                        });
                        chillout.Start();
                    }

                }
            }
            return buf;
        }

        // from guidedhacking (redone from psuedocode)
        static public IntPtr[] AOBScan(string pattern)
        {
            //ProcessModule module = p.MainModule;
            //byte[] modMemory = Memory.ReadMem(module.BaseAddress, module.ModuleMemorySize, 69);
            
            //ProcessModule module = p.MainModule;

            //IntPtr proc = OpenProcess(0x001F0FFF, false, p.Id);

            //byte[] modMemory = new byte[module.ModuleMemorySize];

            //IntPtr bytesRead;

            //ReadProcessMemory(proc, module.BaseAddress, modMemory, module.ModuleMemorySize, out bytesRead);

            byte[] modMemory = ReadMem(DS3Module.BaseAddress, DS3Module.ModuleMemorySize, 58);

            string[] patternBytes = pattern.Split(' ');

            List<IntPtr> addresses = new List<IntPtr>();

            for (long i = 0; i < modMemory.Length - patternBytes.Length; ++i)
            {
                if (modMemory[i] == byte.Parse(patternBytes[0], NumberStyles.HexNumber))
                {
                    byte[] arrayToCheck = new byte[patternBytes.Length];
                    
                    for (int k = 0; k < patternBytes.Length; ++k)
                    {
                        arrayToCheck[k] = modMemory[i + k];
                    }
                    if (AOBPatternCheck(patternBytes, arrayToCheck))
                    {
                        addresses.Add(new IntPtr(((long)DS3Module.BaseAddress) + i));
                        Console.WriteLine("Found pattern at " + Convert.ToHexString(BitConverter.GetBytes(i).Reverse().ToArray()));
                    }
                }
            }

            return addresses.ToArray();
        }

        static public bool AOBPatternCheck(string[] pattern, byte[] aob)
        {
            for (int i = 0; i < aob.Length; ++i)
            {
                if (pattern[i] != "??" && byte.Parse(pattern[i], NumberStyles.HexNumber) != aob[i])
                {
                    return false;
                }
            }
            return true;
        }

        static public IntPtr PointerOffset(IntPtr ptr, long[] offsets)
        {
            foreach (long offset in offsets)
            {
                long addr = BitConverter.ToInt64(ReadMem(ptr, 8));
                if (addr == 0) 
                {
                    //Console.WriteLine("Null pointer hit");
                    return new IntPtr(0);
                }
                ptr = new IntPtr(addr + offset);
            }
            return ptr;
        }
    }
}