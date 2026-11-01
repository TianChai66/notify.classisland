using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Services;
using Microsoft.Extensions.Hosting;

namespace UriNotifyPlugin;

public class UriNavigationHandler : IHostedService
{
    private readonly IUriNavigationService _uriNavigationService;
    private readonly IHostApplicationLifetime _lifetime;

    public UriNavigationHandler(
        IUriNavigationService uriNavigationService,
        IHostApplicationLifetime lifetime)
    {
        _uriNavigationService = uriNavigationService;
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStarted.Register(() =>
        {
            _uriNavigationService.HandlePluginsNavigation("notify", args =>
            {
                HandleNotifyUri(args.Uri);
            });
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void HandleNotifyUri(Uri uri)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(uri.Query);

            // ==========================================
            // 中文兼容解析（优先读取英文，若没有则读取中文）
            // ==========================================
            string mode = query["mode"] ?? query["模式"] ?? "text";
            if (mode == "滚动") mode = "rolling";
            if (mode == "静态") mode = "text";

            string? id = query["id"] ?? query["编号"];
            string? customPath = query["path"] ?? query["路径"];
            string? directMsg = query["msg"] ?? query["正文"];
            string? maskMsg = query["maskMsg"] ?? query["遮罩"];

            double? userDuration = ParseNullableDouble(query["duration"] ?? query["时长"]);
            double maskDuration = ParseDouble(query["maskDuration"] ?? query["遮罩时长"], 5.0);
            double scrollSpeed = ParseDouble(query["scrollSpeed"] ?? query["速度"], 1.0);
            int time = ParseInt(query["time"] ?? query["次数"], 2);

            string? speechStr = query["speech"] ?? query["语音"];
            if (speechStr == "是") speechStr = "true";
            if (speechStr == "否") speechStr = "false";
            bool speech = ParseBool(speechStr, false);

            var provider = MyNotificationProvider.Current;
            if (provider == null) return;

            // 优先级：path > id > msg
            if (!string.IsNullOrEmpty(customPath))
            {
                string? fileContent = LoadTextFromPath(customPath);
                if (fileContent == null)
                {
                    provider.ShowGroup(new NotifyGroup { MaskMsg = "无法读取指定路径文件。", MaskDuration = 2 });
                    return;
                }

                if (NotifyFileParser.LooksLikeGroupFormat(fileContent))
                {
                    var groups = NotifyFileParser.Parse(fileContent);
                    if (groups.Count == 0)
                    {
                        provider.ShowGroup(new NotifyGroup { MaskMsg = "文件格式解析失败。", MaskDuration = 2 });
                        return;
                    }
                    ShowGroupsSequentially(provider, groups);
                    return;
                }

                provider.ShowGroup(new NotifyGroup
                {
                    Mode = mode,
                    Msg = fileContent,
                    MaskMsg = maskMsg,
                    MaskDuration = maskDuration,
                    Duration = userDuration,
                    ScrollSpeed = scrollSpeed,
                    Time = time,
                    Speech = speech,
                });
                return;
            }

            if (!string.IsNullOrEmpty(id))
            {
                string text = LoadTextFromLocal(id) ?? "无法读取消息内容，请检查标识符是否有效。";
                provider.ShowGroup(new NotifyGroup
                {
                    Mode = mode,
                    Msg = text,
                    MaskMsg = maskMsg,
                    MaskDuration = maskDuration,
                    Duration = userDuration,
                    ScrollSpeed = scrollSpeed,
                    Time = time,
                    Speech = speech,
                });
                return;
            }

            // 直接 URI 参数
            HandleDirect(provider, mode, maskMsg, directMsg, maskDuration, userDuration, scrollSpeed, time, speech);
        }
        catch
        {
            // 静默失败
        }
    }

    private void HandleDirect(
        MyNotificationProvider provider,
        string mode,
        string? maskMsg,
        string? msg,
        double maskDuration,
        double? duration,
        double? scrollSpeed,
        int time,
        bool speech)
    {
        bool hasMask = !string.IsNullOrEmpty(maskMsg);
        bool hasMsg = !string.IsNullOrEmpty(msg);

        if (!hasMask && !hasMsg) return;

        provider.ShowGroup(new NotifyGroup
        {
            Mode = mode,
            MaskMsg = maskMsg,
            Msg = msg,
            MaskDuration = maskDuration,
            Duration = duration,
            ScrollSpeed = scrollSpeed,
            Time = time,
            Speech = speech,
        });
    }

    private void ShowGroupsSequentially(MyNotificationProvider provider, List<NotifyGroup> groups)
    {
        int index = 0;

        Action showNext = null!;
        showNext = () =>
        {
            if (index >= groups.Count) return;

            var group = groups[index];
            index++;

            try
            {
                provider.ShowGroup(group);
            }
            catch { }

            if (index < groups.Count)
            {
                double waitSeconds = MyNotificationProvider.GetGroupTotalSeconds(group) + 0.5;
                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(waitSeconds)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    showNext();
                };
                timer.Start();
            }
        };

        Dispatcher.UIThread.Post(showNext);
    }

    private static string? LoadTextFromPath(string path)
    {
        try
        {
            string normalized = path.Replace('/', Path.DirectorySeparatorChar);
            if (!File.Exists(normalized)) return null;
            return File.ReadAllText(normalized, System.Text.Encoding.UTF8);
        }
        catch
        {
            return null;
        }
    }

    private static string? LoadTextFromLocal(string id)
    {
        if (!Regex.IsMatch(id, @"^[a-zA-Z0-9_\-]+$"))
            return null;

        string filePath = Path.Combine(Path.GetTempPath(), $"ClassIslandNotify_{id}.txt");
        if (!File.Exists(filePath)) return null;

        try
        {
            return File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        }
        catch
        {
            return null;
        }
    }

    private static double ParseDouble(string? value, double defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            return result;
        return defaultValue;
    }

    private static double? ParseNullableDouble(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            return result;
        return null;
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;
        return defaultValue;
    }

    private static bool ParseBool(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (bool.TryParse(value, out bool result)) return result;
        if (value == "1") return true;
        if (value == "0") return false;
        return defaultValue;
    }
}