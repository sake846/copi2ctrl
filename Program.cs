using Copi2Ctrl.Core;
using Copi2Ctrl.Native;
using Copi2Ctrl.UI;

namespace Copi2Ctrl;

internal static class Program
{
    private const string AppMutexName = "Global\\Copi2Ctrl_Unique_Mutex_2026";

    [STAThread]
    static void Main(string[] args)
    {
        // 多重起動のチェック
        using var mutex = new Mutex(true, AppMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                "Copi2Ctrl は既に起動しています。タスクトレイを確認してください。",
                "Copi2Ctrl",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        bool showHelp = args.Contains("--help") || args.Contains("-h");
        bool consoleMode = args.Contains("--console") || args.Contains("--debug");
        bool openMonitor = args.Contains("--monitor") || args.Contains("-m");

        if (showHelp)
        {
            bool attached = InitConsoleOutput();
            string helpText = "Copi2Ctrl - Copilotキー to Ctrlキー 置き換えツール (.NET 10)\n\n" +
                              "使用方法: Copi2Ctrl.exe [オプション]\n" +
                              "  (引数なし)    タスクトレイに常駐してバックグラウンド実行\n" +
                              "  --monitor, -m 起動時にキー監視・診断ウィンドウを表示\n" +
                              "  --console     コンソールにキーログを出力するデバッグモード\n" +
                              "  --help, -h    このヘルプを表示";

            if (attached)
            {
                Console.WriteLine(helpText);
            }
            else
            {
                MessageBox.Show(helpText, "Copi2Ctrl ヘルプ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return;
        }

        ApplicationConfiguration.Initialize();

        var settings = AppSettings.Load();
        if (consoleMode)
        {
            settings.LogToConsole = true;
            InitConsoleOutput();
            Console.WriteLine("=== Copi2Ctrl デバッグコンソールモード ===");
            Console.WriteLine($"状態: {(settings.Enabled ? "有効" : "無効")}");
            Console.WriteLine($"リマップ先: {settings.TargetKey}");
            Console.WriteLine("Copilotキー (Win+Shift+F23) を押してテストしてください。");
            Console.WriteLine("終了するにはタスクトレイから終了するか、コンソールで Ctrl+C を押してください。");
            Console.WriteLine("========================================");
        }

        using var remapper = new CopilotKeyRemapper(settings);
        try
        {
            remapper.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"キーフックの開始中にエラーが発生しました:\n{ex.Message}",
                "Copi2Ctrl エラー",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var trayContext = new TrayAppContext(settings, remapper);

        if (openMonitor)
        {
            trayContext.ShowMonitorWindow();
        }

        Application.Run(trayContext);
    }

    private static bool InitConsoleOutput()
    {
        try
        {
            if (NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS) || NativeMethods.AllocConsole())
            {
                var stdOut = NativeMethods.GetStdHandle(NativeMethods.STD_OUTPUT_HANDLE);
                if (stdOut != IntPtr.Zero && stdOut != new IntPtr(-1))
                {
                    var safeHandle = new Microsoft.Win32.SafeHandles.SafeFileHandle(stdOut, ownsHandle: false);
                    var fs = new FileStream(safeHandle, FileAccess.Write);
                    var writer = new StreamWriter(fs, System.Text.Encoding.UTF8) { AutoFlush = true };
                    Console.SetOut(writer);
                    Console.SetError(writer);
                    return true;
                }
            }
        }
        catch
        {
            // アタッチ失敗時は無視
        }
        return false;
    }
}