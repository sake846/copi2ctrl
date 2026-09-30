using Copi2Ctrl.Core;

namespace Copi2Ctrl.UI;

public class TrayAppContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly CopilotKeyRemapper _remapper;
    private readonly NotifyIcon _trayIcon;

    private MonitorForm? _monitorForm;
    private SettingsForm? _settingsForm;

    private ToolStripMenuItem _menuEnabled = null!;
    private ToolStripMenuItem _menuLeftCtrl = null!;
    private ToolStripMenuItem _menuRightCtrl = null!;
    private ToolStripMenuItem _menuStartup = null!;

    public TrayAppContext(AppSettings settings, CopilotKeyRemapper remapper)
    {
        _settings = settings;
        _remapper = remapper;

        _trayIcon = new NotifyIcon
        {
            Text = "Copi2Ctrl - Copilot → Ctrl 変換",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };

        _trayIcon.DoubleClick += (s, e) => ShowMonitorWindow();

        UpdateIconAndState();
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        // --- Group 1: 変換動作と監視 ---
        _menuEnabled = new ToolStripMenuItem("有効 (&E)", null, (s, e) =>
        {
            _settings.Enabled = !_settings.Enabled;
            _settings.Save();
            UpdateIconAndState();
        })
        {
            Checked = _settings.Enabled
        };
        menu.Items.Add(_menuEnabled);

        var targetMenu = new ToolStripMenuItem("変換先キー (&T)");
        _menuLeftCtrl = new ToolStripMenuItem("左 Ctrl (Left Control)", null, (s, e) =>
        {
            SetTargetKey(TargetControlKey.LeftControl);
        });
        _menuRightCtrl = new ToolStripMenuItem("右 Ctrl (Right Control)", null, (s, e) =>
        {
            SetTargetKey(TargetControlKey.RightControl);
        });
        targetMenu.DropDownItems.Add(_menuLeftCtrl);
        targetMenu.DropDownItems.Add(_menuRightCtrl);
        menu.Items.Add(targetMenu);

        var menuMonitor = new ToolStripMenuItem("キー監視・診断を開く (&M)...", null, (s, e) =>
        {
            ShowMonitorWindow();
        });
        menu.Items.Add(menuMonitor);

        _menuStartup = new ToolStripMenuItem("Windows 起動時に実行", null, (s, e) =>
        {
            var newState = !_menuStartup.Checked;
            _settings.SetStartup(newState);
            _menuStartup.Checked = newState;
        })
        {
            Checked = _settings.RunAtStartup
        };
        menu.Items.Add(_menuStartup);

        menu.Items.Add(new ToolStripSeparator());

        // --- Group 2: 設定とアプリ情報 ---
        var menuSettings = new ToolStripMenuItem("設定 (&S)...", null, (s, e) =>
        {
            ShowSettingsWindow();
        });
        menu.Items.Add(menuSettings);

        var menuAbout = new ToolStripMenuItem("Copi2Ctrl について (&A)...", null, (s, e) =>
        {
            using var dlg = new AboutForm();
            dlg.ShowDialog();
        });
        menu.Items.Add(menuAbout);

        menu.Items.Add(new ToolStripSeparator());

        var menuExit = new ToolStripMenuItem("終了 (&X)", null, (s, e) =>
        {
            ExitThread();
        });
        menu.Items.Add(menuExit);

        return menu;
    }

    private void SetTargetKey(TargetControlKey target)
    {
        _settings.TargetKey = target;
        _settings.Save();
        UpdateIconAndState();
    }

    public void UpdateIconAndState()
    {
        _menuEnabled.Checked = _settings.Enabled;
        _menuLeftCtrl.Checked = _settings.TargetKey == TargetControlKey.LeftControl;
        _menuRightCtrl.Checked = _settings.TargetKey == TargetControlKey.RightControl;
        _menuStartup.Checked = _settings.RunAtStartup;

        var targetText = _settings.TargetKey == TargetControlKey.LeftControl ? "左 Ctrl" : "右 Ctrl";
        var statusText = _settings.Enabled ? $"有効 ({targetText})" : "一時停止中";
        _trayIcon.Text = $"Copi2Ctrl: {statusText}";

        var oldIcon = _trayIcon.Icon;
        _trayIcon.Icon = IconHelper.GetAppIcon(_settings.Enabled);
        oldIcon?.Dispose();

        if (_monitorForm != null && !_monitorForm.IsDisposed)
        {
            _monitorForm.UpdateUiState(MonitorUiState.Idle);
        }
    }

    public void ShowSettingsWindow()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_settings);
            _settingsForm.FormClosed += (s, e) => UpdateIconAndState();
            _settingsForm.Show();
        }
        else
        {
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.BringToFront();
            _settingsForm.Activate();
        }
    }

    public void ShowMonitorWindow()
    {
        if (_monitorForm == null || _monitorForm.IsDisposed)
        {
            _monitorForm = new MonitorForm(_settings, _remapper);
            _monitorForm.FormClosed += (s, e) => UpdateIconAndState();
            _monitorForm.Show();
        }
        else
        {
            _monitorForm.WindowState = FormWindowState.Normal;
            _monitorForm.BringToFront();
            _monitorForm.Activate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Visible = false;
            _trayIcon.Icon?.Dispose();
            _trayIcon.Dispose();
            _monitorForm?.Dispose();
            _settingsForm?.Dispose();
            _remapper.Dispose();
        }
        base.Dispose(disposing);
    }
}
