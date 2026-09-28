using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Copi2Ctrl.UI;

/// <summary>
/// "About Copi2Ctrl" dialog: shows product name and version read from version.txt.
/// </summary>
internal sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "Copi2Ctrl — About";
        ClientSize = new System.Drawing.Size(360, 180);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        Font = new System.Drawing.Font("Segoe UI", 9F);
        BackColor = System.Drawing.SystemColors.Window;

        // Header panel
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = System.Drawing.SystemColors.Control,
            Padding = new Padding(16, 10, 16, 10)
        };
        var lblTitle = new Label
        {
            Text = "Copi2Ctrl",
            Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
            AutoSize = true,
            Location = new System.Drawing.Point(16, 12)
        };
        var lblDesc = new Label
        {
            Text = "Copilot key → Ctrl key remapper",
            Font = new System.Drawing.Font("Segoe UI", 8.5F),
            ForeColor = System.Drawing.SystemColors.GrayText,
            AutoSize = true,
            Location = new System.Drawing.Point(16, 38)
        };
        header.Controls.Add(lblTitle);
        header.Controls.Add(lblDesc);

        // Content
        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 0)
        };
        var version = ReadVersion();
        var lblVersion = new Label
        {
            Text = $"バージョン / Version:  {version}",
            Font = new System.Drawing.Font("Segoe UI", 9F),
            AutoSize = true,
            Location = new System.Drawing.Point(16, 16)
        };
        content.Controls.Add(lblVersion);

        // Footer
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            BackColor = System.Drawing.SystemColors.Control,
            Padding = new Padding(8)
        };
        var btnOk = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new System.Drawing.Size(80, 30),
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        btnOk.Location = new System.Drawing.Point(footer.ClientSize.Width - 96, 9);
        btnOk.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        footer.Controls.Add(btnOk);

        Controls.Add(content);
        Controls.Add(footer);
        Controls.Add(header);

        AcceptButton = btnOk;
        CancelButton = btnOk;
    }

    private static string ReadVersion()
    {
        try
        {
            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (dir == null) return "不明 / Unknown";
            string file = Path.Combine(dir, "version.txt");
            if (!File.Exists(file)) return "不明 / Unknown";
            string raw = File.ReadAllText(file).Trim();
            return string.IsNullOrEmpty(raw) ? "不明 / Unknown" : raw;
        }
        catch
        {
            return "不明 / Unknown";
        }
    }
}
