using System.Drawing;
using Copi2Ctrl.Core;
using Copi2Ctrl.Native;

namespace Copi2Ctrl.UI;

public class MonitorForm : Form
{
    private readonly AppSettings _settings;
    private readonly CopilotKeyRemapper _remapper;

    private ListView _lvLog = null!;
    private TextBox _tbTestInput = null!;
    private CheckBox _chkEnabled = null!;
    private RadioButton _rbLeftCtrl = null!;
    private RadioButton _rbRightCtrl = null!;
    private CheckBox _chkPauseLog = null!;
    private Button _btnClear = null!;
    private Button _btnCopy = null!;
    private Label _lblStatus = null!;

    private bool _paused = false;

    public MonitorForm(AppSettings settings, CopilotKeyRemapper remapper)
    {
        _settings = settings;
        _remapper = remapper;

        InitializeComponent();
        _remapper.OnKeyLogged += HandleKeyLogged;
    }

    private void InitializeComponent()
    {
        Text = "Copi2Ctrl - Copilotキー監視・診断";
        Size = new Size(820, 560);
        MinimumSize = new Size(650, 400);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Yu Gothic UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(245, 246, 248);

        // トップパネル (設定コントロール)
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            Padding = new Padding(12),
            BackColor = Color.White
        };

        _lblStatus = new Label
        {
            Text = "【状態】Copilotキー (Win+Shift+F23) を監視中... キーを押すとイベントが表示されます。",
            Font = new Font("Yu Gothic UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 102, 204),
            AutoSize = true,
            Location = new Point(14, 12)
        };

        _chkEnabled = new CheckBox
        {
            Text = "リマップを有効にする",
            Checked = _settings.Enabled,
            AutoSize = true,
            Location = new Point(16, 40),
            Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold)
        };
        _chkEnabled.CheckedChanged += (s, e) =>
        {
            _settings.Enabled = _chkEnabled.Checked;
            _settings.Save();
            UpdateStatusLabel();
        };

        var grpTarget = new GroupBox
        {
            Text = "置き換え先キー",
            Location = new Point(200, 32),
            Size = new Size(240, 48),
            Font = new Font("Yu Gothic UI", 8.5F)
        };

        _rbLeftCtrl = new RadioButton
        {
            Text = "左Ctrl (LControl)",
            Checked = _settings.TargetKey == TargetControlKey.LeftControl,
            Location = new Point(10, 18),
            AutoSize = true
        };
        _rbRightCtrl = new RadioButton
        {
            Text = "右Ctrl (RControl)",
            Checked = _settings.TargetKey == TargetControlKey.RightControl,
            Location = new Point(125, 18),
            AutoSize = true
        };

        _rbLeftCtrl.CheckedChanged += (s, e) =>
        {
            if (_rbLeftCtrl.Checked)
            {
                _settings.TargetKey = TargetControlKey.LeftControl;
                _settings.Save();
            }
        };
        _rbRightCtrl.CheckedChanged += (s, e) =>
        {
            if (_rbRightCtrl.Checked)
            {
                _settings.TargetKey = TargetControlKey.RightControl;
                _settings.Save();
            }
        };
        grpTarget.Controls.Add(_rbLeftCtrl);
        grpTarget.Controls.Add(_rbRightCtrl);

        // テスト入力欄
        var lblTest = new Label
        {
            Text = "動作テスト欄 (Copilot+C, Copilot+V など):",
            Location = new Point(455, 16),
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 80, 80)
        };

        _tbTestInput = new TextBox
        {
            Location = new Point(455, 38),
            Size = new Size(330, 25),
            PlaceholderText = "ここに入力して Copilot+A, C, V などをテスト"
        };

        // 操作ボタン行
        _btnClear = new Button
        {
            Text = "ログ消去",
            Location = new Point(14, 76),
            Size = new Size(75, 26),
            UseVisualStyleBackColor = true
        };
        _btnClear.Click += (s, e) => _lvLog.Items.Clear();

        _btnCopy = new Button
        {
            Text = "ログをコピー",
            Location = new Point(95, 76),
            Size = new Size(95, 26),
            UseVisualStyleBackColor = true
        };
        _btnCopy.Click += BtnCopy_Click;

        _chkPauseLog = new CheckBox
        {
            Text = "ログ表示を一時停止",
            Location = new Point(205, 78),
            AutoSize = true
        };
        _chkPauseLog.CheckedChanged += (s, e) => _paused = _chkPauseLog.Checked;

        topPanel.Controls.Add(_lblStatus);
        topPanel.Controls.Add(_chkEnabled);
        topPanel.Controls.Add(grpTarget);
        topPanel.Controls.Add(lblTest);
        topPanel.Controls.Add(_tbTestInput);
        topPanel.Controls.Add(_btnClear);
        topPanel.Controls.Add(_btnCopy);
        topPanel.Controls.Add(_chkPauseLog);

        // ログ ListView
        _lvLog = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 9F)
        };

        _lvLog.Columns.Add("時刻", 95);
        _lvLog.Columns.Add("イベント", 110);
        _lvLog.Columns.Add("キー名", 110);
        _lvLog.Columns.Add("VKコード", 80);
        _lvLog.Columns.Add("Scan", 60);
        _lvLog.Columns.Add("Injected", 65);
        _lvLog.Columns.Add("処理内容", 250);

        Controls.Add(_lvLog);
        Controls.Add(topPanel);

        UpdateStatusLabel();
    }

    private void UpdateStatusLabel()
    {
        if (_settings.Enabled)
        {
            _lblStatus.Text = "【状態: 有効】Copilotキー を Ctrlキー にリアルタイム置換しています。";
            _lblStatus.ForeColor = Color.FromArgb(0, 120, 215);
        }
        else
        {
            _lblStatus.Text = "【状態: 無効】リマップは一時停止中です。";
            _lblStatus.ForeColor = Color.Gray;
        }
    }

    private void HandleKeyLogged(KeyLogEntry entry)
    {
        if (_paused || IsDisposed) return;

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(() => HandleKeyLogged(entry));
            }
            catch
            {
                // ウィンドウ破棄中の例外を無視
            }
            return;
        }

        var item = new ListViewItem(entry.Timestamp.ToString("HH:mm:ss.fff"));
        item.SubItems.Add(entry.EventType);
        item.SubItems.Add(entry.KeyName);
        item.SubItems.Add($"0x{entry.VkCode:X2}");
        item.SubItems.Add($"0x{entry.ScanCode:X2}");
        item.SubItems.Add(entry.Injected ? "Yes" : "No");
        item.SubItems.Add(entry.ActionTaken);

        // Copilot関連行の色付け
        if (entry.VkCode == NativeMethods.VK_F23)
        {
            item.BackColor = Color.FromArgb(230, 245, 255);
            item.ForeColor = Color.FromArgb(0, 102, 204);
            item.Font = new Font(_lvLog.Font, FontStyle.Bold);
        }
        else if (entry.Injected)
        {
            item.ForeColor = Color.FromArgb(46, 125, 50);
        }

        _lvLog.Items.Add(item);

        // 最大行数制限
        if (_lvLog.Items.Count > 1000)
        {
            _lvLog.Items.RemoveAt(0);
        }

        item.EnsureVisible();
    }

    private void BtnCopy_Click(object? sender, EventArgs e)
    {
        if (_lvLog.Items.Count == 0) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("時刻\tイベント\tキー名\tVKコード\tScan\tInjected\t処理内容");
        foreach (ListViewItem item in _lvLog.Items)
        {
            var line = string.Join("\t", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text));
            sb.AppendLine(line);
        }

        try
        {
            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, "ログをクリップボードにコピーしました。", "コピー完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"コピーに失敗しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // 閉じるボタンが押されたときは画面を閉じる（タスクトレイから再表示可能）
        base.OnFormClosing(e);
        _remapper.OnKeyLogged -= HandleKeyLogged;
    }
}
