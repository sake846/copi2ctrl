using System.Drawing;
using System.Windows.Forms;
using Copi2Ctrl.Core;

namespace Copi2Ctrl.UI;

/// <summary>
/// アプリケーション設定画面
/// 共通 UI 指針に基づき「ヘッダー → 機能別セクション → フッター」の構造を持ちます。
/// </summary>
public sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;

    private CheckBox _chkEnabled = null!;
    private RadioButton _rbLeftCtrl = null!;
    private RadioButton _rbRightCtrl = null!;
    private CheckBox _chkStartup = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public SettingsForm(AppSettings settings)
    {
        _settings = settings;

        InitializeComponent();
        LoadCurrentSettings();
    }

    private void InitializeComponent()
    {
        Text = "Copi2Ctrl — 設定";
        ClientSize = new Size(420, 480);
        MinimumSize = new Size(360, 440);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiTokens.FontBody;
        BackColor = UiTokens.Surface;
        ForeColor = UiTokens.Text;
        Icon = IconHelper.GetAppIcon(true);
        MaximizeBox = false;
        MinimizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        // ===== 1. ヘッダー =====
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = UiTokens.SurfaceAlt,
            Padding = new Padding(UiTokens.Space4, UiTokens.Space3, UiTokens.Space4, UiTokens.Space3)
        };

        var lblHeaderTitle = new Label
        {
            Text = "Copi2Ctrl",
            Font = UiTokens.FontTitle,
            ForeColor = UiTokens.Text,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, UiTokens.Space3)
        };

        var lblHeaderDesc = new Label
        {
            Text = "Copilotキーを Ctrlキーに変換する動作と常駐設定を指定します",
            Font = UiTokens.FontCaption,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, 38)
        };

        headerPanel.Controls.Add(lblHeaderTitle);
        headerPanel.Controls.Add(lblHeaderDesc);

        // ===== 2. フッター =====
        var footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = UiTokens.SurfaceAlt,
            Padding = new Padding(UiTokens.Space4, UiTokens.Space2, UiTokens.Space4, UiTokens.Space2)
        };

        _btnSave = new Button
        {
            Text = "保存",
            DialogResult = DialogResult.OK,
            Size = new Size(UiTokens.ButtonMinWidth, UiTokens.ButtonHeight),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            TabIndex = 10,
            AccessibleName = "設定を保存",
            AccessibleRole = AccessibleRole.PushButton,
            AccessibleDescription = "現在の変更を保存して設定画面を閉じます"
        };
        UiTokens.ApplyPrimaryButtonStyle(_btnSave);
        _btnSave.Location = new Point(footerPanel.ClientSize.Width - UiTokens.ButtonMinWidth - UiTokens.Space4, (footerPanel.ClientSize.Height - UiTokens.ButtonHeight) / 2);
        _btnSave.Click += BtnSave_Click;

        _btnCancel = new Button
        {
            Text = "キャンセル",
            DialogResult = DialogResult.Cancel,
            Size = new Size(UiTokens.ButtonMinWidth, UiTokens.ButtonHeight),
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            TabIndex = 11,
            AccessibleName = "キャンセル",
            AccessibleRole = AccessibleRole.PushButton,
            AccessibleDescription = "変更を破棄して設定画面を閉じます"
        };
        UiTokens.ApplySecondaryButtonStyle(_btnCancel);
        _btnCancel.Location = new Point(_btnSave.Location.X - UiTokens.ButtonMinWidth - UiTokens.Space2, _btnSave.Location.Y);

        footerPanel.Controls.Add(_btnCancel);
        footerPanel.Controls.Add(_btnSave);

        // ===== 3. コンテンツ (スクロール可能) =====
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(UiTokens.Space4)
        };

        int curY = UiTokens.Space3;

        // --- セクション 1: 変換設定 ---
        var lblSectionRemap = new Label
        {
            Text = "変換設定",
            Font = UiTokens.FontSectionHeader,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, curY)
        };
        contentPanel.Controls.Add(lblSectionRemap);
        curY += 24;

        _chkEnabled = new CheckBox
        {
            Text = "リマップを有効にする (&E)",
            Font = UiTokens.FontBody,
            AutoSize = true,
            Location = new Point(UiTokens.Space4 + UiTokens.Space2, curY),
            TabIndex = 1,
            AccessibleName = "リマップを有効にする",
            AccessibleRole = AccessibleRole.CheckButton
        };
        _chkEnabled.CheckedChanged += (s, e) =>
        {
            _rbLeftCtrl.Enabled = _chkEnabled.Checked;
            _rbRightCtrl.Enabled = _chkEnabled.Checked;
        };
        contentPanel.Controls.Add(_chkEnabled);
        curY += 32;

        var grpTarget = new GroupBox
        {
            Text = "変換先キー (&T)",
            Font = UiTokens.FontBody,
            Location = new Point(UiTokens.Space4 + UiTokens.Space2, curY),
            Size = new Size(360, 68),
            TabIndex = 2,
            AccessibleName = "変換先キーの選択",
            AccessibleRole = AccessibleRole.Grouping
        };

        _rbLeftCtrl = new RadioButton
        {
            Text = "左 Ctrl (Left Control)",
            Location = new Point(14, 26),
            AutoSize = true,
            TabIndex = 3,
            AccessibleName = "左 Ctrl に変換"
        };

        _rbRightCtrl = new RadioButton
        {
            Text = "右 Ctrl (Right Control)",
            Location = new Point(180, 26),
            AutoSize = true,
            TabIndex = 4,
            AccessibleName = "右 Ctrl に変換"
        };

        grpTarget.Controls.Add(_rbLeftCtrl);
        grpTarget.Controls.Add(_rbRightCtrl);
        contentPanel.Controls.Add(grpTarget);
        curY += 80;

        // --- セクション 2: 動作設定 ---
        var lblSectionBehavior = new Label
        {
            Text = "動作設定",
            Font = UiTokens.FontSectionHeader,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, curY)
        };
        contentPanel.Controls.Add(lblSectionBehavior);
        curY += 24;

        _chkStartup = new CheckBox
        {
            Text = "Windows 起動時に実行 (&S)",
            Font = UiTokens.FontBody,
            AutoSize = true,
            Location = new Point(UiTokens.Space4 + UiTokens.Space2, curY),
            TabIndex = 5,
            AccessibleName = "Windows 起動時に実行",
            AccessibleRole = AccessibleRole.CheckButton,
            AccessibleDescription = "PC起動時にアプリを自動起動してタスクトレイに常駐させます"
        };
        contentPanel.Controls.Add(_chkStartup);
        curY += 40;

        // 区切り線
        var sep = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Height = 2,
            Width = 360,
            Location = new Point(UiTokens.Space4, curY)
        };
        contentPanel.Controls.Add(sep);
        curY += 16;

        // --- セクション 3: このアプリについて ---
        var lblSectionAbout = new Label
        {
            Text = "このアプリについて",
            Font = UiTokens.FontSectionHeader,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, curY)
        };
        contentPanel.Controls.Add(lblSectionAbout);
        curY += 24;

        var lblAboutProduct = new Label
        {
            Text = "Copi2Ctrl",
            Font = UiTokens.FontBody,
            ForeColor = UiTokens.Text,
            AutoSize = true,
            Location = new Point(UiTokens.Space4 + UiTokens.Space2, curY)
        };

        var versionString = UiTokens.ReadVersion();
        var lblAboutVersion = new Label
        {
            Text = $"バージョン: {versionString}",
            Font = UiTokens.FontBody,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4 + 100, curY),
            AccessibleName = $"プログラムバージョン {versionString}"
        };

        contentPanel.Controls.Add(lblAboutProduct);
        contentPanel.Controls.Add(lblAboutVersion);

        // ===== コントロール追加 =====
        Controls.Add(contentPanel);
        Controls.Add(footerPanel);
        Controls.Add(headerPanel);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;
    }

    private void LoadCurrentSettings()
    {
        _chkEnabled.Checked = _settings.Enabled;
        _rbLeftCtrl.Checked = _settings.TargetKey == TargetControlKey.LeftControl;
        _rbRightCtrl.Checked = _settings.TargetKey == TargetControlKey.RightControl;
        _chkStartup.Checked = _settings.RunAtStartup;

        _rbLeftCtrl.Enabled = _chkEnabled.Checked;
        _rbRightCtrl.Enabled = _chkEnabled.Checked;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        _settings.Enabled = _chkEnabled.Checked;
        _settings.TargetKey = _rbRightCtrl.Checked ? TargetControlKey.RightControl : TargetControlKey.LeftControl;
        _settings.SetStartup(_chkStartup.Checked);
        _settings.Save();
        Close();
    }
}
