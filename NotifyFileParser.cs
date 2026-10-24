using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace UriNotifyPlugin;

public static class NotifyFileParser
{
    public static bool LooksLikeGroupFormat(string content)
    {
        return Regex.IsMatch(content, @"text\{\d+\}");
    }

    public static List<NotifyGroup> Parse(string content)
    {
        var groups = new List<NotifyGroup>();

        var parts = content.Split(new[] { "text{" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            int openIdx = part.IndexOf('(');
            if (openIdx < 0) continue;

            string header = part.Substring(0, openIdx).Trim().TrimEnd('}').Trim();
            if (!int.TryParse(header, out int index)) continue;

            int closeIdx = part.LastIndexOf(')');
            if (closeIdx < openIdx) continue;

            string body = part.Substring(openIdx + 1, closeIdx - openIdx - 1);
            var group = ParseBody(index, body);

            if (!string.IsNullOrEmpty(group.Msg) || !string.IsNullOrEmpty(group.MaskMsg))
                groups.Add(group);
        }

        groups.Sort((a, b) => a.Index.CompareTo(b.Index));
        return groups;
    }

    private static NotifyGroup ParseBody(int index, string body)
    {
        var g = new NotifyGroup { Index = index };

        // 按行拆分，逐行解析
        var lines = body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // ---------- 全局参数行：mode ----------
            var m = Regex.Match(line, @"^mode:\{(text|rolling)\}$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                g.Mode = m.Groups[1].Value.ToLowerInvariant();
                continue;
            }

            // ---------- 全局参数行：speech ----------
            m = Regex.Match(line, @"^speech:\{(true|false|1|0)\}$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var v = m.Groups[1].Value.ToLowerInvariant();
                g.Speech = v == "true" || v == "1";
                continue;
            }

            // ---------- 判断该行的类型 ----------
            bool hasMaskDuration = line.Contains("maskDuration"); // 保持大小写敏感，用户按规范写即可
            bool hasUpperMsg = Regex.IsMatch(line, @"(?<![A-Za-z])Msg:\{");
            bool hasLowerMsg = Regex.IsMatch(line, @"(?<![A-Za-z])msg:\{");
            bool hasDuration = line.Contains("duration");
            
            // 【修改点】只识别全小写的 scrollspeed
            bool hasScrollSpeed = line.Contains("scrollspeed"); 
            
            bool hasTime = Regex.IsMatch(line, @"(?<![A-Za-z])time\b");

            // ---------- 遮罩行：必须同时包含 maskDuration 和 Msg ----------
            if (hasUpperMsg)
            {
                if (!hasMaskDuration) continue;

                var mdMatch = Regex.Match(line, @"maskDuration\s*:?\s*\{([0-9.]+)\}");
                if (mdMatch.Success && double.TryParse(mdMatch.Groups[1].Value,
                    NumberStyles.Float, CultureInfo.InvariantCulture, out double md))
                {
                    g.MaskDuration = md;
                }

                var msgMatch = Regex.Match(line, @"(?<![A-Za-z])Msg:\{""([^""]*?)""\}");
                if (msgMatch.Success)
                {
                    g.MaskMsg = msgMatch.Groups[1].Value;
                }
                continue;
            }

            if (hasMaskDuration) continue;

            // ---------- 正文行：必须包含 msg（小写） ----------
            if (hasLowerMsg)
            {
                var msgMatch = Regex.Match(line, @"(?<![A-Za-z])msg:\{""([^""]*?)""\}");
                if (msgMatch.Success)
                {
                    g.Msg = msgMatch.Groups[1].Value;
                }

                if (hasDuration)
                {
                    var dMatch = Regex.Match(line, @"duration\s*:?\s*\{([0-9.]+)\}");
                    if (dMatch.Success && double.TryParse(dMatch.Groups[1].Value,
                        NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    {
                        g.Duration = d;
                    }
                }

                // 【修改点】提取参数时，正则也只匹配全小写的 scrollspeed
                if (hasScrollSpeed)
                {
                    var ssMatch = Regex.Match(line, @"scrollspeed\s*:?\s*\{([0-9.]+)\}");
                    if (ssMatch.Success && double.TryParse(ssMatch.Groups[1].Value,
                        NumberStyles.Float, CultureInfo.InvariantCulture, out double ss))
                    {
                        g.ScrollSpeed = ss;
                    }
                }

                if (hasTime)
                {
                    var tMatch = Regex.Match(line, @"(?<![A-Za-z])time\s*:?\s*\{(\d+)\}");
                    if (tMatch.Success && int.TryParse(tMatch.Groups[1].Value, out int t))
                    {
                        g.Time = t;
                    }
                }
                continue;
            }
        }

        return g;
    }
}