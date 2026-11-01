using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace UriNotifyPlugin;

public static class NotifyFileParser
{
    public static bool LooksLikeGroupFormat(string content)
    {
        // 👑 核心修复 1：干掉可能存在的 UTF-8 BOM 头
        content = content.TrimStart('\uFEFF');

        // 1. 匹配纯英文格式：text{1}( 或 text1:
        bool isEnglish = Regex.IsMatch(content, @"^\s*text\s*\{?\s*\d+\s*\}?\s*[:(]", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        
        // 2. 匹配中文极简格式：%通知1：
        bool isChinese = Regex.IsMatch(content, @"^\s*%\s*通知\s*\{?\s*\d+\s*\}?\s*[:：]", RegexOptions.Multiline);

        return isEnglish || isChinese;
    }

    public static List<NotifyGroup> Parse(string content)
    {
        // 👑 核心修复 2：解析前也干掉 BOM 头
        content = content.TrimStart('\uFEFF');

        // 1. 预处理：全角转半角
        content = content
            .Replace('：', ':')
            .Replace('｛', '{')
            .Replace('｝', '}')
            .Replace('（', '(')
            .Replace('）', ')')
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('，', ',');

        var groups = new List<NotifyGroup>();
        NotifyGroup? current = null;

        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            
            if (string.IsNullOrEmpty(line) || line == ")" || line == "）") continue;

            // 2. 新组开头
            var headerMatch = Regex.Match(line, @"^(?:%|％)?(?:通知|text)\s*\{?\s*(\d+)\s*\}?[\s(:]*$", RegexOptions.IgnoreCase);
            
            if (headerMatch.Success && (line.StartsWith("%") || line.StartsWith("％") || line.ToLower().StartsWith("text")))
            {
                if (current != null && (!string.IsNullOrEmpty(current.Msg) || !string.IsNullOrEmpty(current.MaskMsg)))
                {
                    groups.Add(current);
                }

                current = new NotifyGroup();
                if (int.TryParse(headerMatch.Groups[1].Value, out int idx))
                    current.Index = idx;
                continue;
            }

            if (current == null) continue;

            // 3. 模式
            var m = Regex.Match(line, @"^(?:模式|mode)\s*:\s*\{(滚动|静态|text|rolling)\}$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var modeVal = m.Groups[1].Value;
                current.Mode = (modeVal == "滚动" || modeVal.Equals("rolling", StringComparison.OrdinalIgnoreCase)) ? "rolling" : "text";
                continue;
            }

            // 4. 语音
            m = Regex.Match(line, @"^(?:语音|speech)\s*:\s*\{(否|是|0|1|true|false)\}$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var s = m.Groups[1].Value.ToLower();
                current.Speech = (s == "是" || s == "1" || s == "true");
                continue;
            }

            // 5. 遮罩行
            bool isMaskLine = line.Contains("遮罩:") || Regex.IsMatch(line, @"(?<![A-Za-z])Msg\s*:");
            if (isMaskLine)
            {
                var mdMatch = Regex.Match(line, @"(?:遮罩时长|maskDuration)\s*\{?\s*([\d.]+)\s*\}?", RegexOptions.IgnoreCase);
                if (mdMatch.Success && double.TryParse(mdMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double md))
                    current.MaskDuration = md;

                var msgMatch = Regex.Match(line, @"(?:遮罩|Msg)\s*:\s*\{([^}]*)\}");
                if (msgMatch.Success)
                {
                    string rawText = msgMatch.Groups[1].Value;
                    if (rawText.Length >= 2 && rawText.StartsWith("\"") && rawText.EndsWith("\""))
                        rawText = rawText.Substring(1, rawText.Length - 2);
                    current.MaskMsg = rawText;
                }
                continue;
            }

            // 6. 正文行
            bool isMsgLine = line.Contains("正文:") || Regex.IsMatch(line, @"(?<![A-Za-z])msg\s*:");
            if (isMsgLine)
            {
                var dMatch = Regex.Match(line, @"(?:时长|duration)\s*\{?\s*([\d.]+)\s*\}?", RegexOptions.IgnoreCase);
                if (dMatch.Success && double.TryParse(dMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    current.Duration = d;

                var ssMatch = Regex.Match(line, @"(?:速度|scrollSpeed)\s*\{?\s*([\d.]+)\s*\}?", RegexOptions.IgnoreCase);
                if (ssMatch.Success && double.TryParse(ssMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double ss))
                    current.ScrollSpeed = ss;

                var tMatch = Regex.Match(line, @"(?:次数|time)\s*\{?\s*(\d+)\s*\}?", RegexOptions.IgnoreCase);
                if (tMatch.Success && int.TryParse(tMatch.Groups[1].Value, out int t))
                    current.Time = t;

                var msgMatch = Regex.Match(line, @"(?:正文|msg)\s*:\s*\{([^}]*)\}");
                if (msgMatch.Success)
                {
                    string rawText = msgMatch.Groups[1].Value;
                    if (rawText.Length >= 2 && rawText.StartsWith("\"") && rawText.EndsWith("\""))
                        rawText = rawText.Substring(1, rawText.Length - 2);
                    current.Msg = rawText;
                }
                continue;
            }
        }

        if (current != null && (!string.IsNullOrEmpty(current.Msg) || !string.IsNullOrEmpty(current.MaskMsg)))
            groups.Add(current);

        groups.Sort((a, b) => a.Index.CompareTo(b.Index));
        return groups;
    }
}