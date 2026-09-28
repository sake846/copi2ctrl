using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Copi2Ctrl.UI;

public static class IconGenerator
{
    public static void GenerateIcoFile(string outputPath = "app.ico")
    {
        int[] sizes = [256, 128, 64, 48, 32, 16];
        var pngStreams = new List<byte[]>();

        foreach (int size in sizes)
        {
            using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.Clear(Color.Transparent);

                float margin = size * 0.05f;
                float keyWidth = size - margin * 2;
                float keyHeight = size - margin * 2;
                float radius = size * 0.22f;

                // キーの影 (ドロップシャドウ)
                float shadowOffset = Math.Max(1f, size * 0.04f);
                using (var shadowPath = CreateRoundedRectangle(margin, margin + shadowOffset, keyWidth, keyHeight, radius))
                using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // キートップ本体 (外枠・側面)
                using (var basePath = CreateRoundedRectangle(margin, margin, keyWidth, keyHeight, radius))
                using (var baseBrush = new LinearGradientBrush(
                    new PointF(margin, margin),
                    new PointF(margin, margin + keyHeight),
                    Color.FromArgb(255, 30, 41, 59),   // slate-800
                    Color.FromArgb(255, 15, 23, 42)))  // slate-900
                {
                    g.FillPath(baseBrush, basePath);
                }

                // キートップ表面 (キートップのインセット面)
                float inset = Math.Max(1f, size * 0.035f);
                using (var surfacePath = CreateRoundedRectangle(margin + inset, margin + inset, keyWidth - inset * 2, keyHeight - inset * 2, radius * 0.85f))
                using (var surfaceBrush = new LinearGradientBrush(
                    new PointF(margin, margin),
                    new PointF(margin, margin + keyHeight),
                    Color.FromArgb(255, 14, 116, 219),  // primary blue
                    Color.FromArgb(255, 3, 76, 172)))   // deep blue
                {
                    g.FillPath(surfaceBrush, surfacePath);
                }

                // キートップの上部ハイライト光
                using (var highlightPath = CreateRoundedRectangle(margin + inset + 1, margin + inset + 1, keyWidth - (inset + 1) * 2, (keyHeight - inset * 2) * 0.45f, radius * 0.6f))
                using (var highlightBrush = new LinearGradientBrush(
                    new PointF(margin, margin),
                    new PointF(margin, margin + size * 0.4f),
                    Color.FromArgb(130, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255)))
                {
                    g.FillPath(highlightBrush, highlightPath);
                }

                // Copilot のリボンロゴアクセント
                DrawCopilotMark(g, size);

                // "Ctrl" の文字
                string text = size <= 20 ? "C" : "Ctrl";
                float fontSize = size switch
                {
                    >= 256 => 78f,
                    >= 128 => 40f,
                    >= 64 => 20f,
                    >= 48 => 15f,
                    >= 32 => 10f,
                    _ => 8.5f
                };

                using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                using var textBrush = new SolidBrush(Color.White);
                using var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                // 文字の影
                float textShadowOffset = Math.Max(1f, size * 0.015f);
                if (size >= 32)
                {
                    using var textShadowBrush = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
                    var shadowRect = new RectangleF(margin, margin + textShadowOffset, keyWidth, keyHeight);
                    g.DrawString(text, font, textShadowBrush, shadowRect, stringFormat);
                }

                var textRect = new RectangleF(margin, margin, keyWidth, keyHeight);
                g.DrawString(text, font, textBrush, textRect, stringFormat);
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            pngStreams.Add(ms.ToArray());
        }

        // ICO ファイルの構築
        using var icoFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(icoFs);

        // ICONHEADER
        bw.Write((ushort)0); // reserved
        bw.Write((ushort)1); // type 1 = icon
        bw.Write((ushort)sizes.Length); // count

        // 計算用: ヘッダ + 全ディレクトリエントリのサイズ
        int offset = 6 + (16 * sizes.Length);

        // ICONDIRENTRY を書き込む
        for (int i = 0; i < sizes.Length; i++)
        {
            int size = sizes[i];
            byte[] data = pngStreams[i];

            bw.Write((byte)(size >= 256 ? 0 : size)); // width
            bw.Write((byte)(size >= 256 ? 0 : size)); // height
            bw.Write((byte)0); // color count
            bw.Write((byte)0); // reserved
            bw.Write((ushort)1); // planes
            bw.Write((ushort)32); // bit count
            bw.Write((uint)data.Length); // bytes in res
            bw.Write((uint)offset); // image offset

            offset += data.Length;
        }

        // PNG データを書き込む
        for (int i = 0; i < sizes.Length; i++)
        {
            bw.Write(pngStreams[i]);
        }
    }

    private static void DrawCopilotMark(Graphics g, int size)
    {
        if (size < 32) return;

        var state = g.Save();

        // 右上に配置 (Copilot の交差リボン)
        float scale = size / 256.0f;
        float tx = size * 0.62f;
        float ty = size * 0.12f;
        g.TranslateTransform(tx, ty);
        g.ScaleTransform(scale, scale);

        // 左側ループ: シアン〜ブルー〜ティール (Copilotカラー)
        using (var pathLeft = new GraphicsPath())
        {
            pathLeft.AddBezier(
                new PointF(22, 60),
                new PointF(6, 46),
                new PointF(6, 20),
                new PointF(26, 12));
            pathLeft.AddBezier(
                new PointF(26, 12),
                new PointF(42, 6),
                new PointF(54, 20),
                new PointF(44, 38));

            using var brushLeft = new LinearGradientBrush(
                new PointF(6, 12), new PointF(54, 60),
                Color.FromArgb(255, 0, 210, 180),   // Copilot ティール/シアン
                Color.FromArgb(255, 0, 120, 215));  // Copilot ブルー

            using var penLeft = new Pen(brushLeft, 14f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawPath(penLeft, pathLeft);
        }

        // 右側ループ: コーラルピンク〜オレンジ〜アンバー (Copilotカラー)
        using (var pathRight = new GraphicsPath())
        {
            pathRight.AddBezier(
                new PointF(46, 8),
                new PointF(62, 22),
                new PointF(62, 48),
                new PointF(42, 56));
            pathRight.AddBezier(
                new PointF(42, 56),
                new PointF(26, 62),
                new PointF(14, 48),
                new PointF(24, 30));

            using var brushRight = new LinearGradientBrush(
                new PointF(14, 8), new PointF(62, 56),
                Color.FromArgb(255, 255, 140, 0),   // Copilot オレンジ/アンバー
                Color.FromArgb(255, 240, 60, 110)); // Copilot コーラルピンク

            using var penRight = new Pen(brushRight, 14f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            g.DrawPath(penRight, pathRight);
        }

        g.Restore(state);
    }

    private static GraphicsPath CreateRoundedRectangle(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
