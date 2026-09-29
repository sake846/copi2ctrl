using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Copi2Ctrl.Core;
using Copi2Ctrl.Native;

namespace Copi2Ctrl.UI;

/// <summary>
/// UI 状態モデル (idle / editing / processing / success / error)
/// </summary>
public enum MonitorUiState
{
    Idle,
    Editing,
    Processing,
    Success,
    Error
}

/// <summary>
/// キー監視・診断画面
/// 「入力 → 変換 → 結果 → コピー」の流れを明確にした再設計画面
/// </summary>
public class MonitorForm : Form
{
    private readonly AppSettings _settings;
    private readonly CopilotKeyRemapper _remapper;

    // UI コントロール
    private Label _lblStatusIcon = null!;
    private Label _lblStatusText = null!;
    private Panel _pnlStatusBar = null!;

    private TextBox _tbTestInput = null!;
    private Button _btnCopy = null!;
    private Button _btnClear = null!;
    private CheckBox _chkPauseLog = null!;
    private Button _btnOpenSettings = null!;
    private ListView _lvLog = null!;
    private ToolTip _toolTip = null!;

    private System.Windows.Forms.Timer _statusResetTimer = null!;
    private MonitorUiState _currentState = MonitorUiState.Idle;
    private bool _paused = false;

    public MonitorForm(AppSettings settings, CopilotKeyRemapper remapper)
    {
        _settings = settings;
        _remapper = remapper;

        InitializeComponent();
        _remapper.OnKeyLogged += HandleKeyLogged;
        UpdateUiState(MonitorUiState.Idle);
    }

    private void InitializeComponent()
    {
        Text = "Copi2Ctrl — キー監視・診断";
        Size = new Size(840, 620);
        MinimumSize = new Size(680, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiTokens.FontBody;
        BackColor = UiTokens.Surface;
        ForeColor = UiTokens.Text;
        Icon = IconHelper.GetAppIcon(true);

        _toolTip = new ToolTip();

        _statusResetTimer = new System.Windows.Forms.Timer
        {
            Interval = 3500
        };
        _statusResetTimer.Tick += (s, e) =>
        {
            _statusResetTimer.Stop();
            if (_currentState == MonitorUiState.Success || _currentState == MonitorUiState.Error)
            {
                UpdateUiState(_tbTestInput.Focused ? MonitorUiState.Editing : MonitorUiState.Idle);
            }
        };

        // ===== 1. ヘッダーパネル =====
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = UiTokens.SurfaceAlt,
            Padding = new Padding(UiTokens.Space4, UiTokens.Space2, UiTokens.Space4, UiTokens.Space2)
        };

        var lblHeaderTitle = new Label
        {
            Text = "Copi2Ctrl 診断",
            Font = UiTokens.FontTitle,
            ForeColor = UiTokens.Text,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, UiTokens.Space2)
        };

        var lblHeaderDesc = new Label
        {
            Text = "Copilotキー (Win+Shift+F23) の押下を検知し、Ctrlキーへの変換動作をリアルタイムに診断・テストします。",
            Font = UiTokens.FontCaption,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, 34)
        };

        pnlHeader.Controls.Add(lblHeaderTitle);
        pnlHeader.Controls.Add(lblHeaderDesc);

        // ===== 2. 状態バー (Status Banner) =====
        _pnlStatusBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(235, 243, 250),
            Padding = new Padding(UiTokens.Space4, 6, UiTokens.Space4, 6)
        };

        _lblStatusIcon = new Label
        {
            Text = "●",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = UiTokens.Accent,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, 8)
        };

        _lblStatusText = new Label
        {
            Text = "待機中: リマップ有効",
            Font = UiTokens.FontSectionHeader,
            ForeColor = UiTokens.Text,
            AutoSize = true,
            Location = new Point(UiTokens.Space4 + 20, 9),
            AccessibleName = "動作状態",
            AccessibleRole = AccessibleRole.StaticText
        };

        _pnlStatusBar.Controls.Add(_lblStatusIcon);
        _pnlStatusBar.Controls.Add(_lblStatusText);

        // ===== 3. 操作・入力パネル (Top Panel) =====
        var pnlControls = new Panel
        {
            Dock = DockStyle.Top,
            Height = 120,
            BackColor = UiTokens.Surface,
            Padding = new Padding(UiTokens.Space4, UiTokens.Space3, UiTokens.Space4, UiTokens.Space3)
        };

        // --- セクション 1: 入力 (テスト欄) ---
        var lblInputHeader = new Label
        {
            Text = "1. 入力 (キー入力テスト)",
            Font = UiTokens.FontSectionHeader,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, UiTokens.Space2)
        };

        var lblInputHelp = new Label
        {
            Text = "テスト欄にフォーカスを当てて Copilot+A, C, V などのショートカット動作を確認できます:",
            Font = UiTokens.FontCaption,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4 + 160, UiTokens.Space2)
        };

        _tbTestInput = new TextBox
        {
            Location = new Point(UiTokens.Space4, 26),
            Size = new Size(pnlControls.ClientSize.Width - (UiTokens.Space4 * 2), UiTokens.ControlHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = UiTokens.FontBody,
            PlaceholderText = "ここに入力してキー動作をテスト (例: Copilotキーを押しながら C や V を入力)",
            TabIndex = 1,
            AccessibleName = "テスト入力欄",
            AccessibleDescription = "Copilotキーと組み合わせてショートカット動作をテストする入力ボックスです"
        };
        _tbTestInput.GotFocus += (s, e) => UpdateUiState(MonitorUiState.Editing);
        _tbTestInput.LostFocus += (s, e) =>
        {
            if (_currentState == MonitorUiState.Editing)
            {
                UpdateUiState(MonitorUiState.Idle);
            }
        };

        // --- セクション 2: 主操作 & 補助操作バー ---
        int actionY = 68;

        // 主操作: ログをコピー (Primary action)
        _btnCopy = new Button
        {
            Text = "ログをコピー (&C)",
            Location = new Point(UiTokens.Space4, actionY),
            Size = new Size(130, UiTokens.ButtonHeight),
            TabIndex = 2,
            Enabled = false,
            AccessibleName = "ログをクリップボードにコピー",
            AccessibleRole = AccessibleRole.PushButton,
            AccessibleDescription = "表示されているキーイベントログをすべてクリップボードにコピーします"
        };
        UiTokens.ApplyPrimaryButtonStyle(_btnCopy);
        _btnCopy.Click += BtnCopy_Click;
        _toolTip.SetToolTip(_btnCopy, "表示中のキーログをクリップボードにコピーします (Ctrl+C)");

        // 補助操作: クリア (用語統一: ログ消去 -> クリア)
        _btnClear = new Button
        {
            Text = "クリア (&L)",
            Location = new Point(UiTokens.Space4 + 138, actionY),
            Size = new Size(UiTokens.ButtonMinWidth, UiTokens.ButtonHeight),
            TabIndex = 3,
            Enabled = false,
            AccessibleName = "ログをクリア",
            AccessibleRole = AccessibleRole.PushButton,
            AccessibleDescription = "記録されたキーイベントログを消去して一覧を空にします"
        };
        UiTokens.ApplySecondaryButtonStyle(_btnClear);
        _btnClear.Click += (s, e) =>
        {
            _lvLog.Items.Clear();
            UpdateLogActionsState();
            UpdateUiState(MonitorUiState.Idle, "ログをクリアしました。");
        };
        _toolTip.SetToolTip(_btnClear, "ログ一覧をクリアします");

        // 補助操作: ログ表示を一時停止
        _chkPauseLog = new CheckBox
        {
            Text = "ログ更新を一時停止 (&P)",
            Location = new Point(UiTokens.Space4 + 230, actionY + 4),
            AutoSize = true,
            Font = UiTokens.FontBody,
            TabIndex = 4,
            AccessibleName = "ログ更新を一時停止",
            AccessibleRole = AccessibleRole.CheckButton
        };
        _chkPauseLog.CheckedChanged += (s, e) =>
        {
            _paused = _chkPauseLog.Checked;
            UpdateUiState(_paused ? MonitorUiState.Idle : _currentState);
        };

        // 補助操作: 設定ボタン
        _btnOpenSettings = new Button
        {
            Text = "設定 (&S)...",
            Location = new Point(pnlControls.ClientSize.Width - UiTokens.Space4 - 100, actionY),
            Size = new Size(100, UiTokens.ButtonHeight),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TabIndex = 5,
            AccessibleName = "設定画面を開く",
            AccessibleRole = AccessibleRole.PushButton
        };
        UiTokens.ApplySecondaryButtonStyle(_btnOpenSettings);
        _btnOpenSettings.Click += (s, e) =>
        {
            using var dlg = new SettingsForm(_settings);
            dlg.ShowDialog(this);
            UpdateUiState(_currentState);
        };

        pnlControls.Controls.Add(lblInputHeader);
        pnlControls.Controls.Add(lblInputHelp);
        pnlControls.Controls.Add(_tbTestInput);
        pnlControls.Controls.Add(_btnCopy);
        pnlControls.Controls.Add(_btnClear);
        pnlControls.Controls.Add(_chkPauseLog);
        pnlControls.Controls.Add(_btnOpenSettings);

        // ===== 4. 結果領域 (キーイベントログ) =====
        var pnlResultHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = UiTokens.SurfaceAlt,
            Padding = new Padding(UiTokens.Space4, 6, UiTokens.Space4, 2)
        };

        var lblResultTitle = new Label
        {
            Text = "2. 結果 (キーイベントログ)",
            Font = UiTokens.FontSectionHeader,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, 6)
        };
        pnlResultHeader.Controls.Add(lblResultTitle);

        _lvLog = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            BorderStyle = BorderStyle.FixedSingle,
            Font = UiTokens.FontCode,
            TabIndex = 6,
            AccessibleName = "キーイベントログ一覧",
            AccessibleRole = AccessibleRole.Table,
            AccessibleDescription = "検知されたキーイベントと変換処理の詳細一覧です"
        };

        _lvLog.Columns.Add("時刻", 95);
        _lvLog.Columns.Add("イベント", 110);
        _lvLog.Columns.Add("キー名", 110);
        _lvLog.Columns.Add("VKコード", 80);
        _lvLog.Columns.Add("Scan", 60);
        _lvLog.Columns.Add("Injected", 65);
        _lvLog.Columns.Add("処理内容", 280);

        _lvLog.KeyDown += LvLog_KeyDown;

        // コントロール配置
        Controls.Add(_lvLog);
        Controls.Add(pnlResultHeader);
        Controls.Add(pnlControls);
        Controls.Add(_pnlStatusBar);
        Controls.Add(pnlHeader);

        KeyPreview = true;
        KeyDown += MonitorForm_KeyDown;
    }

    private void MonitorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void LvLog_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.C)
        {
            BtnCopy_Click(sender, EventArgs.Empty);
            e.Handled = true;
        }
    }

    /// <summary>
    /// UI の状態遷移を更新し、色とテキストの両方でユーザーにフィードバックします。
    /// </summary>
    public void UpdateUiState(MonitorUiState newState, string? customMessage = null)
    {
        _currentState = newState;

        var targetText = _settings.TargetKey == TargetControlKey.LeftControl ? "左 Ctrl" : "右 Ctrl";
        var isRemapActive = _settings.Enabled;

        switch (newState)
        {
            case MonitorUiState.Idle:
                if (!isRemapActive)
                {
                    _pnlStatusBar.BackColor = Color.FromArgb(245, 245, 245);
                    _lblStatusIcon.Text = "■";
                    _lblStatusIcon.ForeColor = UiTokens.TextMuted;
                    _lblStatusText.Text = customMessage ?? "一時停止中: リマップは無効化されています (設定から有効化できます)";
                }
                else if (_paused)
                {
                    _pnlStatusBar.BackColor = Color.FromArgb(255, 248, 225);
                    _lblStatusIcon.Text = "⏸";
                    _lblStatusIcon.ForeColor = Color.FromArgb(230, 130, 0);
                    _lblStatusText.Text = customMessage ?? $"監視中 (ログ更新停止中): リマップ有効 ({targetText})";
                }
                else
                {
                    _pnlStatusBar.BackColor = Color.FromArgb(235, 243, 250);
                    _lblStatusIcon.Text = "●";
                    _lblStatusIcon.ForeColor = UiTokens.Accent;
                    _lblStatusText.Text = customMessage ?? $"監視中 (正常): Copilotキーを {targetText} にリアルタイム変換しています";
                }
                break;

            case MonitorUiState.Editing:
                _pnlStatusBar.BackColor = Color.FromArgb(238, 245, 255);
                _lblStatusIcon.Text = "✎";
                _lblStatusIcon.ForeColor = UiTokens.Accent;
                _lblStatusText.Text = customMessage ?? "入力中: テスト欄を編集中です。Copilot+C, Copilot+V などの動作を確認してください。";
                break;

            case MonitorUiState.Processing:
                _pnlStatusBar.BackColor = Color.FromArgb(230, 245, 255);
                _lblStatusIcon.Text = "⚡";
                _lblStatusIcon.ForeColor = Color.FromArgb(0, 102, 204);
                _lblStatusText.Text = customMessage ?? $"変換実行中: Copilotキー押下を検知し {targetText} に変換しました";
                break;

            case MonitorUiState.Success:
                _pnlStatusBar.BackColor = Color.FromArgb(232, 245, 233);
                _lblStatusIcon.Text = "✓";
                _lblStatusIcon.ForeColor = UiTokens.Success;
                _lblStatusText.Text = customMessage ?? "成功: ログをクリップボードにコピーしました。";
                _statusResetTimer.Stop();
                _statusResetTimer.Start();
                break;

            case MonitorUiState.Error:
                _pnlStatusBar.BackColor = Color.FromArgb(255, 235, 238);
                _lblStatusIcon.Text = "⚠";
                _lblStatusIcon.ForeColor = UiTokens.Danger;
                _lblStatusText.Text = customMessage ?? "エラーが発生しました。もう一度試してください。";
                _statusResetTimer.Stop();
                _statusResetTimer.Start();
                break;
        }
    }

    private void UpdateLogActionsState()
    {
        bool hasLogs = _lvLog.Items.Count > 0;
        _btnCopy.Enabled = hasLogs;
        _btnClear.Enabled = hasLogs;

        if (!hasLogs)
        {
            _toolTip.SetToolTip(_btnCopy, "コピーするログがありません (キーを入力するとログが蓄積されます)");
        }
        else
        {
            _toolTip.SetToolTip(_btnCopy, "表示中のキーログをクリップボードにコピーします (Ctrl+C)");
        }
    }

    private void HandleKeyLogged(KeyLogEntry entry)
    {
        if (IsDisposed) return;

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

        if (entry.VkCode == NativeMethods.VK_F23)
        {
            UpdateUiState(MonitorUiState.Processing);
        }

        if (_paused) return;

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
            item.ForeColor = UiTokens.Success;
        }

        _lvLog.Items.Add(item);

        // 最大行数制限
        if (_lvLog.Items.Count > 1000)
        {
            _lvLog.Items.RemoveAt(0);
        }

        item.EnsureVisible();
        UpdateLogActionsState();
    }

    /// <summary>
    /// 主操作: ログのクリップボードコピー
    /// モーダルダイアログを使わず、インラインのステータスバナーで通知してフォーカスを維持します。
    /// </summary>
    private void BtnCopy_Click(object? sender, EventArgs e)
    {
        if (_lvLog.Items.Count == 0)
        {
            UpdateUiState(MonitorUiState.Error, "コピー失敗: ログが 0 件です。");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("時刻\tイベント\tキー名\tVKコード\tScan\tInjected\t処理内容");
        foreach (ListViewItem item in _lvLog.Items)
        {
            var line = string.Join("\t", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(s => s.Text));
            sb.AppendLine(line);
        }

        try
        {
            Clipboard.SetText(sb.ToString());
            UpdateUiState(MonitorUiState.Success, $"✓ ログをクリップボードにコピーしました ({_lvLog.Items.Count} 件)");
        }
        catch (Exception ex)
        {
            UpdateUiState(MonitorUiState.Error, $"コピー失敗: {ex.Message} (再試行してください)");
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        _remapper.OnKeyLogged -= HandleKeyLogged;
        _statusResetTimer.Dispose();
    }
}
