using System.Drawing;
using System.Drawing.Drawing2D;

namespace Copi2Ctrl.UI;

public static class IconHelper
{
    private static Icon? _cachedExeIcon;

    public static Icon GetAppIcon(bool enabled = true)
    {
        if (enabled)
        {
            if (_cachedExeIcon != null) return (Icon)_cachedExeIcon.Clone();

            try
            {
                var processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
                {
                    var icon = Icon.ExtractAssociatedIcon(processPath);
                    if (icon != null)
                    {
                        _cachedExeIcon = icon;
                        return (Icon)icon.Clone();
                    }
                }
            }
            catch
            {
                // フォールバック
            }
        }

        return CreateDynamicIcon(enabled);
    }

    public static Icon CreateDynamicIcon(bool enabled = true)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // 背景の角丸四角形
            var bgColor = enabled ? Color.FromArgb(14, 116, 219) : Color.FromArgb(100, 116, 139);
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillEllipse(brush, 1, 1, 30, 30);
            }

            // 外枠のハイライト
            using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.5f))
            {
                g.DrawEllipse(pen, 2, 2, 28, 28);
            }

            // "Ctrl" の文字
            using var font = new Font("Segoe UI", 11, FontStyle.Bold, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(Color.White);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString("Ctrl", font, textBrush, new RectangleF(0, 1, 32, 32), format);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }
}
