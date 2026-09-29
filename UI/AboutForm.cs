using System.Drawing;
using System.Windows.Forms;

namespace Copi2Ctrl.UI;

/// <summary>
/// "Copi2Ctrl について" ダイアログ: 製品名と version.txt から読み込んだバージョンを表示します。
/// </summary>
public sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "Copi2Ctrl — このアプリについて";
        ClientSize = new Size(380, 200);
        MinimumSize = new Size(320, 180);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        Font = UiTokens.FontBody;
        BackColor = UiTokens.Surface;

        // ===== Header panel =====
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = UiTokens.SurfaceAlt,
            Padding = new Padding(UiTokens.Space4, UiTokens.Space3, UiTokens.Space4, UiTokens.Space3)
        };
        var lblTitle = new Label
        {
            Text = "Copi2Ctrl",
            Font = UiTokens.FontTitle,
            ForeColor = UiTokens.Text,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, UiTokens.Space3)
        };
        var lblDesc = new Label
        {
            Text = "Copilotキーを Ctrlキーに変換する常駐ツール",
            Font = UiTokens.FontCaption,
            ForeColor = UiTokens.TextMuted,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, 38)
        };
        header.Controls.Add(lblTitle);
        header.Controls.Add(lblDesc);

        // ===== Content =====
        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiTokens.Space4, UiTokens.Space4, UiTokens.Space4, UiTokens.Space2)
        };
        var version = UiTokens.ReadVersion();
        var lblVersion = new Label
        {
            Text = $"バージョン / Version:  {version}",
            Font = UiTokens.FontBody,
            ForeColor = UiTokens.Text,
            AutoSize = true,
            Location = new Point(UiTokens.Space4, UiTokens.Space4)
        };
        content.Controls.Add(lblVersion);

        // ===== Footer =====
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = UiTokens.SurfaceAlt,
            Padding = new Padding(UiTokens.Space2)
        };
        var btnOk = new Button
        {
            Text = "閉じる",
            DialogResult = DialogResult.OK,
            Size = new Size(UiTokens.ButtonMinWidth, UiTokens.ButtonHeight),
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        UiTokens.ApplyPrimaryButtonStyle(btnOk);
        btnOk.Location = new Point(footer.ClientSize.Width - UiTokens.ButtonMinWidth - UiTokens.Space4, (footer.ClientSize.Height - UiTokens.ButtonHeight) / 2);
        btnOk.AccessibleName = "閉じる";
        btnOk.AccessibleRole = AccessibleRole.PushButton;
        footer.Controls.Add(btnOk);

        Controls.Add(content);
        Controls.Add(footer);
        Controls.Add(header);

        AcceptButton = btnOk;
        CancelButton = btnOk;
    }
}
