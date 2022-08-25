using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;

public class KeyboardHook : IDisposable
{
    public bool IsStarted { get { return hookID != IntPtr.Zero;  } }

    internal delegate IntPtr LowLevelKeyboardProcDelegate(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);
    private LowLevelKeyboardProcDelegate proc;
    private IntPtr hookID = IntPtr.Zero;

    public event KeyboardHookEventHandler KeysChanged;

    public KeyboardHook()
    {
        proc = new LowLevelKeyboardProcDelegate(LowLevelKeyboardProc);
    }

    public void Start()
    {
        if (IsStarted) return;
        keysCurrentlyDown.Clear();
        using (Process curProcess = Process.GetCurrentProcess())
        {
            using (ProcessModule curModule = curProcess.MainModule)
            {
                hookID = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }
    }

    public void Stop()
    {
        if (!IsStarted) return;
        NativeMethods.UnhookWindowsHookEx(hookID);
        hookID = IntPtr.Zero;
        keysCurrentlyDown.Clear();
    }

    private List<Keys> keysCurrentlyDown = new List<Keys>();

    public enum KeyDirection
    {
        Down,
        Up
    }

    private IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam)
    {
        if (nCode >= 0)
        {
            //Console.WriteLine($"LowLevelKeyboardProc: lParam={lParam}");
            Keys key = (Keys)lParam.vkCode;
            //Console.WriteLine($"LowLevelKeyboardProc: key={key}");
            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                if (!keysCurrentlyDown.Contains(key))
                {
                    keysCurrentlyDown.Add(key);
                    OnKeysChanged(new KeysChangedEventArgs(KeyDirection.Down, keysCurrentlyDown));
                }
            }
            else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
            {
                if (keysCurrentlyDown.Remove(key))
                {
                    OnKeysChanged(new KeysChangedEventArgs(KeyDirection.Up, keysCurrentlyDown));
                }
            }
        }
        return NativeMethods.CallNextHookEx(hookID, nCode, wParam, ref lParam);

    }

    public void OnKeysChanged(KeysChangedEventArgs e)
    {
        KeysChanged?.Invoke(e);
    }

    public delegate void KeyboardHookEventHandler(KeysChangedEventArgs e);

    public class KeysChangedEventArgs : EventArgs
    {
        public KeyDirection MostRecentKeyDirection { get; private set; }
        public Keys[] KeysCurrentlyDown { get; private set; }
        public bool IsCapsLockOn { get { return (NativeMethods.GetKeyState((int)Keys.Capital) & 0x0001) == 0x0001; } }

        public KeysChangedEventArgs(KeyDirection mostRecentKeyDirection, List<Keys> keysCurrentlyDown)
        {
            MostRecentKeyDirection = mostRecentKeyDirection;
            KeysCurrentlyDown = keysCurrentlyDown.ToArray();
        }

        public override string ToString()
        {
            return $"{{ MostRecentKeyDirection={MostRecentKeyDirection}, KeysCurrentlyDown=[{KeysCurrentlyDown}] }}";
        }
    }

    #region IDisposable Members
    /// <summary>
    /// Releases the keyboard hook.
    /// </summary>
    public void Dispose()
    {
        Stop();
    }
    #endregion

    #region Native methods

    private const int WH_KEYBOARD_LL = 0x0D;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    internal struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        int scanCode;
        public int flags;
        int time;
        int dwExtraInfo;

        public override string ToString()
        {
            return $"{{ vkCode={vkCode}, scanCode={scanCode}, flags={flags}, time={time}, dwExtraInfo={dwExtraInfo} }}";
        }
    }

    [ComVisibleAttribute(false),
     System.Security.SuppressUnmanagedCodeSecurity()]
    internal class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProcDelegate lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

        /// <summary>
        /// Return value
        /// Type: SHORT
        /// The return value specifies the status of the specified virtual key, as follows:
        /// * If the high-order bit is 1, the key is down; otherwise, it is up.
        /// * If the low-order bit is 1, the key is toggled. A key, such as the CAPS LOCK key, is toggled if it is turned on. The key is off and untoggled if the low-order bit is 0. A toggle key's indicator light (if any) on the keyboard will be on when the key is toggled, and off when the key is untoggled.
        /// </summary>
        /// <param name="keyCode"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);
    } 
 
    #endregion
}


