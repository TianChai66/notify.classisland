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
            string mode = query["mode"] ?? "text";
            string? id = query["id"];
            string? customPath = query["path"];
            string? directMsg = query["msg"];
            string? maskMsg = query["maskMsg"];

            double? userDuration = ParseNullableDouble(query["duration"]);
            // 【修改点】只识别全小写的 scrollspeed
            double? userScrollSpeed = ParseNullableDouble(query["scrollspeed"]);
            
            double maskDuration = ParseDouble(query["maskDuration"], 5.0);
            int time = ParseInt(query["time"], 2);
            bool speech = ParseBool(query["speech"], false);

            var provider = MyNotificationProvider.Current;
            if (provider == null) return;

            // 优先级：path > id > msg/maskMsg

            // 情形 1：path 指向文件
            if (!string.IsNullOrEmpty(customPath))
            {
                string? fileContent = LoadTextFromPath(customPath);
                if (fileContent == null)
                {
                    // 读取失败时用 maskMsg 兜底
                    if (!string.IsNullOrEmpty(maskMsg) || !string.IsNullOrEmpty(directMsg))
                        HandleDirect(provider, mode, maskMsg, directMsg, maskDuration, userDuration, userScrollSpeed, time, speech);
                    else
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

                // 普通单条文本文件（整段内容作为一条通知处理）
                provider.ShowGroup(new NotifyGroup
                {
                    Mode = mode,
                    Msg = fileContent,
                    MaskMsg = maskMsg,
                    MaskDuration = maskDuration,
                    Duration = userDuration,
                    ScrollSpeed = userScrollSpeed,
                    Time = time,
                    Speech = speech,
                });
                return;
            }

            // 情形 2：id 引用临时文件
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
                    ScrollSpeed = userScrollSpeed,
                    Time = time,
                    Speech = speech,
                });
                return;
            }

            // 情形 3：直接 URI 参数
            HandleDirect(provider, mode, maskMsg, directMsg, maskDuration, userDuration, userScrollSpeed, time, speech);
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