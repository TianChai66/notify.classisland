using System;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;

namespace UriNotifyPlugin;

[NotificationProviderInfo(
    "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
    "Uri 通知提供方",
    "Bell",
    "接收来自 URI 的提醒请求。"
)]
public class MyNotificationProvider : NotificationProviderBase
{
    private const double EstimatedCharPixels = 22.0;
    private const double EstimatedContainerPixels = 400.0;
    private const double BasePixelsPerSecond = 80.0;
    private const double DefaultMaskDuration = 5.0;
    private const double DefaultRollingDuration = 20.0;
    private const int DefaultTime = 2;
    private const double DefaultSimpleDuration = 5.0;

    /// <summary>无遮罩时使用的占位遮罩时长（秒），几乎不可见。</summary>
    private const double PlaceholderMaskSeconds = 0.01;

    public static MyNotificationProvider? Current { get; private set; }

    public MyNotificationProvider() : base()
    {
        Current = this;
    }

    public void ShowGroup(NotifyGroup group)
    {
        bool hasMask = !string.IsNullOrEmpty(group.MaskMsg);
        bool hasMsg = !string.IsNullOrEmpty(group.Msg);

        if (!hasMask && !hasMsg) return;

        if (hasMsg && group.Mode == "rolling")
        {
            string text = group.Msg!;
            string? maskText = hasMask ? group.MaskMsg : null;

            var (onePass, times) = CalculateRollingParams(text, group.Duration, group.ScrollSpeed, group.Time);

            double maskDuration = group.MaskDuration > 0 ? group.MaskDuration : DefaultMaskDuration;

            SendRolling(
                text: text,
                maskText: maskText,
                maskDurationSeconds: maskDuration,
                onePassSeconds: onePass,
                iterations: times,
                speechEnabled: group.Speech);
        }
        else
        {
            string? text = hasMsg ? group.Msg : null;
            string? maskText = hasMask ? group.MaskMsg : null;

            double maskDuration = group.MaskDuration > 0 ? group.MaskDuration : DefaultMaskDuration;
            double overlayDuration = group.Duration ?? DefaultSimpleDuration;

            SendSimple(
                text: text,
                maskText: maskText,
                maskDurationSeconds: maskDuration,
                overlayDurationSeconds: overlayDuration,
                speechEnabled: group.Speech);
        }
    }

    public static double GetGroupTotalSeconds(NotifyGroup group)
    {
        bool hasMask = !string.IsNullOrEmpty(group.MaskMsg);
        bool hasMsg = !string.IsNullOrEmpty(group.Msg);

        if (!hasMask && !hasMsg) return 0;

        double total = 0;
        if (hasMask)
            total += group.MaskDuration > 0 ? group.MaskDuration : DefaultMaskDuration;
        else
            total += PlaceholderMaskSeconds;

        if (hasMsg)
        {
            string text = group.Msg!;
            if (group.Mode == "rolling")
            {
                var (onePass, times) = CalculateRollingParams(text, group.Duration, group.ScrollSpeed, group.Time);
                total += onePass * times;
            }
            else
            {
                total += group.Duration ?? DefaultSimpleDuration;
            }
        }

        return total;
    }

    private static (double onePassSeconds, int times) CalculateRollingParams(
        string text, double? duration, double? scrollSpeed, int? time)
    {
        int times = time ?? DefaultTime;
        if (times < 1) times = 1;

        if (duration.HasValue && duration.Value > 0)
        {
            return (duration.Value / times, times);
        }

        if (scrollSpeed.HasValue && scrollSpeed.Value > 0)
        {
            double speed = scrollSpeed.Value;
            double pixelsPerSecond = BasePixelsPerSecond * speed;
            if (pixelsPerSecond < 10) pixelsPerSecond = 10;

            double totalPixels = text.Length * EstimatedCharPixels + EstimatedContainerPixels;
            double onePass = totalPixels / pixelsPerSecond;
            if (onePass < 3.0) onePass = 3.0;

            return (onePass, times);
        }

        return (DefaultRollingDuration / times, times);
    }

    /// <summary>
    /// 构造遮罩内容。传 null 时返回一个几乎不可见的占位遮罩，
    /// 因为 ClassIsland 的 MainWindowLine 不接受 MaskContent 为 null。
    /// </summary>
    private static NotificationContent BuildMask(string? maskText, double maskDurationSeconds)
    {
        if (!string.IsNullOrEmpty(maskText))
        {
            var content = NotificationContent.CreateTwoIconsMask(maskText);
            content.Duration = TimeSpan.FromSeconds(maskDurationSeconds);
            return content;
        }

        // 占位遮罩：单个空格，极短时长
        var placeholder = NotificationContent.CreateTwoIconsMask(" ");
        placeholder.Duration = TimeSpan.FromSeconds(PlaceholderMaskSeconds);
        return placeholder;
    }

    private void SendSimple(
        string? text,
        string? maskText,
        double maskDurationSeconds,
        double overlayDurationSeconds,
        bool speechEnabled)
    {
        // 遮罩始终非 null
        var maskContent = BuildMask(maskText, maskDurationSeconds);

        NotificationContent? overlayContent = null;
        if (!string.IsNullOrEmpty(text))
        {
            string safeText = text!;
            overlayContent = NotificationContent.CreateSimpleTextContent(safeText);
            overlayContent.Duration = TimeSpan.FromSeconds(overlayDurationSeconds);
            overlayContent.IsSpeechEnabled = speechEnabled;
            if (speechEnabled)
                overlayContent.SpeechContent = safeText;
        }

        ShowNotification(new NotificationRequest
        {
            MaskContent = maskContent,
            OverlayContent = overlayContent
        });
    }

    private void SendRolling(
        string text,
        string? maskText,
        double maskDurationSeconds,
        double onePassSeconds,
        int iterations,
        bool speechEnabled)
    {
        // 遮罩始终非 null
        var maskContent = BuildMask(maskText, maskDurationSeconds);

        var control = new SpeedRollingControl(text, onePassSeconds, iterations);
        var overlayContent = new NotificationContent(control)
        {
            Duration = TimeSpan.FromSeconds(onePassSeconds * iterations),
            IsSpeechEnabled = speechEnabled,
        };
        if (speechEnabled)
            overlayContent.SpeechContent = text;

        ShowNotification(new NotificationRequest
        {
            MaskContent = maskContent,
            OverlayContent = overlayContent
        });
    }
}