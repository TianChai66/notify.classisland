using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace UriNotifyPlugin;

/// <summary>
/// 速度可控的滚动文本控件。
/// 起始位置使用真实容器宽度，避免滚动前出现空白等待。
/// </summary>
public class SpeedRollingControl : UserControl
{
    private readonly TextBlock _textBlock;
    private readonly double _onePassSeconds;
    private readonly int _iterations;
    private bool _started;

    public SpeedRollingControl(string text, double onePassSeconds, int iterations = 1)
    {
        _onePassSeconds = onePassSeconds <= 0.1 ? 0.1 : onePassSeconds;
        _iterations = iterations < 1 ? 1 : iterations;

        _textBlock = new TextBlock
        {
            Text = text,
            FontSize = 18,
            FontWeight = FontWeight.Normal,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        };

        var canvas = new Canvas();
        canvas.Children.Add(_textBlock);

        Content = canvas;
        ClipToBounds = true;
        Background = null;

        // 用 SizeChanged 等控件拿到真实尺寸后再启动动画
        this.SizeChanged += (_, e) =>
        {
            if (_started) return;
            if (e.NewSize.Width > 10 && e.NewSize.Height > 5)
            {
                _started = true;
                StartScroll(e.NewSize.Width, e.NewSize.Height);
            }
        };
    }

    private void StartScroll(double containerWidth, double containerHeight)
    {
        _textBlock.Measure(Size.Infinity);
        double textWidth = _textBlock.DesiredSize.Width;
        double textHeight = _textBlock.DesiredSize.Height;

        // 关键：起始位置用真实容器宽度（不再硬编码 900）
        Canvas.SetLeft(_textBlock, containerWidth);
        Canvas.SetTop(_textBlock, Math.Max(0, (containerHeight - textHeight) / 2));

        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(_onePassSeconds),
            IterationCount = new IterationCount((ulong)_iterations),
            Easing = new LinearEasing(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(Canvas.LeftProperty, containerWidth) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(Canvas.LeftProperty, -textWidth) }
                }
            }
        };

        _ = animation.RunAsync(_textBlock);
    }
}