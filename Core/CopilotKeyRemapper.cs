using System.Diagnostics;
using System.Runtime.InteropServices;
using Copi2Ctrl.Native;

namespace Copi2Ctrl.Core;

public record KeyLogEntry(
    DateTime Timestamp,
    string EventType,
    uint VkCode,
    string KeyName,
    uint ScanCode,
    bool Injected,
    string ActionTaken);

public class CopilotKeyRemapper : IDisposable
{
    private readonly AppSettings _settings;
    private readonly NativeMethods.LowLevelKeyboardProc _hookProc;
    private IntPtr _hookId = IntPtr.Zero;

    private bool _isCopilotActive = false;
    private long _suppressLShiftUpUntil = 0;
    private long _suppressLWinUpUntil = 0;

    public event Action<KeyLogEntry>? OnKeyLogged;

    public bool IsHookActive => _hookId != IntPtr.Zero;

    public CopilotKeyRemapper(AppSettings settings)
    {
        _settings = settings;
        _hookProc = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        var moduleHandle = NativeMethods.GetModuleHandle(curModule?.ModuleName);
        _hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _hookProc, moduleHandle, 0);

        if (_hookId == IntPtr.Zero)
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"キーボードフックの登録に失敗しました (エラーコード: {errorCode})");
        }
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;

            // もしCopilotキー押下状態のまま終了した場合は、Ctrlを解除
            if (_isCopilotActive)
            {
                ReleaseTargetCtrl();
                _isCopilotActive = false;
            }
        }
    }

    private uint GetTargetVk()
    {
        return _settings.TargetKey switch
        {
            TargetControlKey.RightControl => NativeMethods.VK_RCONTROL,
            _ => NativeMethods.VK_LCONTROL
        };
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            var msg = wParam.ToInt32();
            var isInjected = (kb.flags & NativeMethods.LLKHF_INJECTED) != 0 || kb.dwExtraInfo == NativeMethods.INJECTED_SIGNATURE;
            var isDown = msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN;
            var isUp = msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP;

            // 自前で注入したキーイベントはそのまま通過
            if (isInjected)
            {
                LogKey(isDown ? "INJECT_DOWN" : "INJECT_UP", kb, true, "自前インジェクション通過");
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // 無効化されている場合は全てスルー
            if (!_settings.Enabled)
            {
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // F23 キーの検出 (Copilot キーのコア)
            if (kb.vkCode == NativeMethods.VK_F23)
            {
                if (isDown)
                {
                    if (!_isCopilotActive)
                    {
                        _isCopilotActive = true;
                        LogKey("KEYDOWN", kb, false, "Copilot検出 -> Ctrl押下にリマップ");
                        HandleCopilotDown();
                    }
                    else
                    {
                        // リピート
                        LogKey("KEYDOWN (Repeat)", kb, false, "Copilotリピート抑制");
                    }
                    return (IntPtr)1; // ブロック
                }
                else if (isUp)
                {
                    if (_isCopilotActive)
                    {
                        _isCopilotActive = false;
                        LogKey("KEYUP", kb, false, "Copilot離下 -> Ctrl離下にリマップ");
                        HandleCopilotUp();
                    }
                    else
                    {
                        LogKey("KEYUP", kb, false, "F23KeyUpブロック");
                    }
                    return (IntPtr)1; // ブロック
                }
            }

            // Copilotキー連動の LWin / LShift 解除時の不要な Up イベントを抑制
            var now = Environment.TickCount64;

            if (kb.vkCode == NativeMethods.VK_LSHIFT)
            {
                if (_isCopilotActive && isUp)
                {
                    LogKey("KEYUP", kb, false, "Copilot中のLShift Upを抑制");
                    return (IntPtr)1;
                }
                if (isUp && now <= _suppressLShiftUpUntil)
                {
                    _suppressLShiftUpUntil = 0;
                    LogKey("KEYUP", kb, false, "Copilot後のLShift Upを抑制");
                    return (IntPtr)1;
                }
            }

            if (kb.vkCode == NativeMethods.VK_LWIN)
            {
                if (_isCopilotActive && isUp)
                {
                    LogKey("KEYUP", kb, false, "Copilot中のLWin Upを抑制");
                    return (IntPtr)1;
                }
                if (isUp && now <= _suppressLWinUpUntil)
                {
                    _suppressLWinUpUntil = 0;
                    LogKey("KEYUP", kb, false, "Copilot後のLWin Upを抑制");
                    return (IntPtr)1;
                }
            }

            // 通常キー
            LogKey(isDown ? "KEYDOWN" : "KEYUP", kb, false, "通常通過");
        }

        return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private void HandleCopilotDown()
    {
        var targetVk = GetTargetVk();
        var inputs = new List<NativeMethods.INPUT>();

        // スタートメニュー誤発火を確実に防止するためのマスクキー (VK 0xFF) を注入
        inputs.Add(CreateKeyInput(0xFF, isKeyUp: false));
        inputs.Add(CreateKeyInput(0xFF, isKeyUp: true));

        // ハードウェアから送られてきた LShift / LWin を明示的に解放
        inputs.Add(CreateKeyInput(NativeMethods.VK_LSHIFT, isKeyUp: true, scanCode: 0x2A));
        inputs.Add(CreateKeyInput(NativeMethods.VK_LWIN, isKeyUp: true, scanCode: 0x5B, isExtended: true));

        // 目的の Ctrl キーを押下 (Down)
        bool isExtended = targetVk == NativeMethods.VK_RCONTROL;
        inputs.Add(CreateKeyInput(targetVk, isKeyUp: false, scanCode: 0x1D, isExtended: isExtended));

        SendInputs(inputs);
    }

    private void HandleCopilotUp()
    {
        var targetVk = GetTargetVk();
        var inputs = new List<NativeMethods.INPUT>();

        // 目的の Ctrl キーを解放 (Up)
        bool isExtended = targetVk == NativeMethods.VK_RCONTROL;
        inputs.Add(CreateKeyInput(targetVk, isKeyUp: true, scanCode: 0x1D, isExtended: isExtended));

        SendInputs(inputs);

        // この直後にハードウェアから送られてくる LShift Up, LWin Up を最大400ms抑制
        _suppressLShiftUpUntil = Environment.TickCount64 + 400;
        _suppressLWinUpUntil = Environment.TickCount64 + 400;
    }

    private void ReleaseTargetCtrl()
    {
        var targetVk = GetTargetVk();
        bool isExtended = targetVk == NativeMethods.VK_RCONTROL;
        var input = CreateKeyInput(targetVk, isKeyUp: true, scanCode: 0x1D, isExtended: isExtended);
        SendInputs(new[] { input });
    }

    private static NativeMethods.INPUT CreateKeyInput(uint vk, bool isKeyUp, ushort scanCode = 0, bool isExtended = false)
    {
        uint flags = 0;
        if (isKeyUp) flags |= NativeMethods.KEYEVENTF_KEYUP;
        if (isExtended) flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
        if (scanCode != 0) flags |= NativeMethods.KEYEVENTF_SCANCODE;

        return new NativeMethods.INPUT
        {
            type = NativeMethods.INPUT_KEYBOARD,
            u = new NativeMethods.InputUnion
            {
                ki = new NativeMethods.KEYBDINPUT
                {
                    wVk = (ushort)vk,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = NativeMethods.INJECTED_SIGNATURE
                }
            }
        };
    }

    private static void SendInputs(IReadOnlyList<NativeMethods.INPUT> inputs)
    {
        if (inputs.Count == 0) return;
        var arr = inputs.ToArray();
        NativeMethods.SendInput((uint)arr.Length, arr, Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private void LogKey(string eventType, NativeMethods.KBDLLHOOKSTRUCT kb, bool injected, string action)
    {
        if (OnKeyLogged == null && !_settings.LogToConsole) return;

        var keyName = ((Keys)kb.vkCode).ToString();
        var entry = new KeyLogEntry(
            DateTime.Now,
            eventType,
            kb.vkCode,
            keyName,
            kb.scanCode,
            injected,
            action);

        if (_settings.LogToConsole)
        {
            Console.WriteLine($"[{entry.Timestamp:HH:mm:ss.fff}] {entry.EventType,-14} VK:0x{entry.VkCode:X2} ({entry.KeyName,-10}) Scan:0x{entry.ScanCode:X2} Injected:{entry.Injected,-5} -> {entry.ActionTaken}");
        }

        OnKeyLogged?.Invoke(entry);
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
