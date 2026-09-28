using System.Drawing;
using System.Drawing.Drawing2D;

namespace Copi2Ctrl.UI;

public static class IconHelper
{
    public static Icon CreateAppIcon(bool enabled = true)
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // 背景の円
            var bgColor = enabled ? Color.FromArgb(0, 120, 215) : Color.FromArgb(128, 128, 128);
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillEllipse(brush, 1, 1, 30, 30);
            }

            // 外枠のハイライト
            using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.5f))
            {
                g.DrawEllipse(pen, 2, 2, 28, 28);
            }

            // "Ctrl" または "C" の文字
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
