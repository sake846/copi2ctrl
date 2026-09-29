using System.Drawing;
using System.IO;
using System.Reflection;

namespace Copi2Ctrl.UI;

/// <summary>
/// 共通デザイントークン定義
/// ScaleSwitcher / CtrlHanabi / copi2ctrl で統一された基準値
/// </summary>
public static class UiTokens
{
    // ===== 余白 (Spacing) =====
    public const int Space1 = 4;
    public const int Space2 = 8;
    public const int Space3 = 12;
    public const int Space4 = 16;
    public const int Space6 = 24;

    // ===== コントロール寸法 =====
    public const int ControlHeight = 32;
    public const int ButtonHeight = 32;
    public const int ButtonMinWidth = 80;

    // ===== 角丸 =====
    public const int RadiusControl = 4;
    public const int RadiusCard = 8;

    // ===== タイポグラフィ =====
    public static readonly Font FontTitle = new Font("Segoe UI", 13.5F, FontStyle.Bold);
    public static readonly Font FontSectionHeader = new Font("Segoe UI", 9F, FontStyle.Bold);
    public static readonly Font FontBody = new Font("Segoe UI", 9F, FontStyle.Regular);
    public static readonly Font FontCaption = new Font("Segoe UI", 8.5F, FontStyle.Regular);
    public static readonly Font FontCode = new Font("Consolas", 9F, FontStyle.Regular);

    // ===== 色トークン (OSシステムテーマ優先) =====
    public static Color Surface => SystemColors.Window;
    public static Color SurfaceAlt => SystemColors.Control;
    public static Color Text => SystemColors.WindowText;
    public static Color TextMuted => SystemColors.GrayText;
    public static Color Border => SystemColors.ActiveBorder;
    public static Color Accent => SystemColors.Highlight;
    public static Color AccentText => SystemColors.HighlightText;
    public static Color Danger => Color.Firebrick;
    public static Color Success => Color.FromArgb(46, 125, 50);

    /// <summary>
    /// アプリに同梱された version.txt を読み込み、バージョン文字列を返します。
    /// ファイルがない、空、または読み込めない場合は「不明 / Unknown」を返します。
    /// </summary>
    public static string ReadVersion()
    {
        try
        {
            // 実行中アセンブリのディレクトリ（通常ビルドやテスト時）
            string? assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(assemblyDir))
            {
                string file = Path.Combine(assemblyDir, "version.txt");
                if (File.Exists(file))
                {
                    string raw = File.ReadAllText(file).Trim();
                    if (!string.IsNullOrEmpty(raw))
                    {
                        return raw;
                    }
                }
            }

            // AppContext.BaseDirectory（単一ファイル発行時など）
            string baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                string file = Path.Combine(baseDir, "version.txt");
                if (File.Exists(file))
                {
                    string raw = File.ReadAllText(file).Trim();
                    if (!string.IsNullOrEmpty(raw))
                    {
                        return raw;
                    }
                }
            }

            return "不明 / Unknown";
        }
        catch
        {
            return "不明 / Unknown";
        }
    }

    /// <summary>
    /// プライマリボタンスタイル（強調表示）を適用します。
    /// </summary>
    public static void ApplyPrimaryButtonStyle(Button button)
    {
        button.Height = ButtonHeight;
        button.MinimumSize = new Size(ButtonMinWidth, ButtonHeight);
        button.Font = new Font(FontBody.FontFamily, FontBody.Size, FontStyle.Bold);
        button.BackColor = Accent;
        button.ForeColor = AccentText;
        button.FlatStyle = FlatStyle.System;
        button.UseVisualStyleBackColor = false;
    }

    /// <summary>
    /// セカンダリボタンスタイル（標準表示）を適用します。
    /// </summary>
    public static void ApplySecondaryButtonStyle(Button button)
    {
        button.Height = ButtonHeight;
        button.MinimumSize = new Size(ButtonMinWidth, ButtonHeight);
        button.Font = FontBody;
        button.FlatStyle = FlatStyle.System;
        button.UseVisualStyleBackColor = true;
    }
}
