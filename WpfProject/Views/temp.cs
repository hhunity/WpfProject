
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfProject.Views
{
    public partial class NumericStepper : UserControl
    {
        private int _ticks;

        public NumericStepper()
        {
            InitializeComponent();
        }

        //================= Transform =================
        public static readonly DependencyProperty TransformProperty =
            DependencyProperty.Register(
                nameof(Transform),
                typeof(INumericTransform),
                typeof(NumericStepper),
                new PropertyMetadata(
                    new LinearTransform { Scale = 0.1, Offset = 0.0 },
                    (d, _) => ((NumericStepper)d).SyncFromTicks()));

        public INumericTransform Transform
        {
            get => (INumericTransform)GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        //================= Value =================
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumericStepper),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (NumericStepper)d;
            if (c.Transform == null) return;

            c._ticks = c.Transform.ToTicks((double)e.NewValue);
            c._ticks = c.ClampTicks(c._ticks); // ✅ tick範囲で制限

            var args = new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue, (double)e.NewValue)
            { RoutedEvent = ValueChangedEvent };
            c.RaiseEvent(args);
        }

        //================= イベント =================
        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ValueChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>),
                typeof(NumericStepper));

        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        //================= 操作 =================
        private void OnUpClick(object sender, RoutedEventArgs e)
        {
            _ticks = ClampTicks(_ticks + 1);
            SyncFromTicks();
        }

        private void OnDownClick(object sender, RoutedEventArgs e)
        {
            _ticks = ClampTicks(_ticks - 1);
            SyncFromTicks();
        }

        private void OnTextWheel(object sender, MouseWheelEventArgs e)
        {
            _ticks += (e.Delta > 0 ? 1 : -1);
            _ticks = ClampTicks(_ticks);
            SyncFromTicks();
            e.Handled = true;
        }

        private void SyncFromTicks()
        {
            if (Transform == null) return;

            double newVal = Transform.ToValue(_ticks);
            Value = newVal;
        }

        //================= 範囲クランプ =================
        private int ClampTicks(int ticks)
        {
            if (Transform?.DefaultMinTick is int min && ticks < min) ticks = min;
            if (Transform?.DefaultMaxTick is int max && ticks > max) ticks = max;
            return ticks;
        }
    }
}
using System;

namespace WpfProject.Views
{
    public class LinearTransform : INumericTransform
    {
        public double Scale { get; set; } = 0.1;
        public double Offset { get; set; } = 0.0;

        // tick範囲指定（任意）
        public int? DefaultMinTick { get; set; } = 0;
        public int? DefaultMaxTick { get; set; } = 100;

        public double ToValue(int ticks) => ticks * Scale + Offset;
        public int ToTicks(double value) => (int)Math.Round((value - Offset) / Scale);
    }
}
namespace WpfProject.Views
{
    public interface INumericTransform
    {
        double ToValue(int ticks);
        int ToTicks(double value);

        // tick範囲（nullなら制限なし）
        int? DefaultMinTick { get; }
        int? DefaultMaxTick { get; }
    }
}






using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfProject.Views
{
    public partial class NumericStepper : UserControl
    {
        private int _ticks;

        public NumericStepper()
        {
            InitializeComponent();
        }

        //================= Transform =================
        public static readonly DependencyProperty TransformProperty =
            DependencyProperty.Register(
                nameof(Transform),
                typeof(INumericTransform),
                typeof(NumericStepper),
                new PropertyMetadata(new LinearTransform { Scale = 0.1, Offset = 0.0 },
                    (d, _) => ((NumericStepper)d).SyncFromTicks()));

        public INumericTransform Transform
        {
            get => (INumericTransform)GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        //================= Tick Range =================
        public static readonly DependencyProperty MinTickProperty =
            DependencyProperty.Register(nameof(MinTick), typeof(int), typeof(NumericStepper),
                new PropertyMetadata(int.MinValue));

        public static readonly DependencyProperty MaxTickProperty =
            DependencyProperty.Register(nameof(MaxTick), typeof(int), typeof(NumericStepper),
                new PropertyMetadata(int.MaxValue));

        public int MinTick
        {
            get => (int)GetValue(MinTickProperty);
            set => SetValue(MinTickProperty, value);
        }

        public int MaxTick
        {
            get => (int)GetValue(MaxTickProperty);
            set => SetValue(MaxTickProperty, value);
        }

        //================= Value (double) =================
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumericStepper),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (NumericStepper)d;
            if (c.Transform == null) return;

            c._ticks = c.Transform.ToTicks((double)e.NewValue);
            c._ticks = Math.Clamp(c._ticks, c.MinTick, c.MaxTick); // ← tick範囲でクランプ

            var args = new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue, (double)e.NewValue)
            { RoutedEvent = ValueChangedEvent };
            c.RaiseEvent(args);
        }

        //================= イベント =================
        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ValueChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>),
                typeof(NumericStepper));

        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        //================= 操作 =================
        private void OnUpClick(object sender, RoutedEventArgs e)
        {
            _ticks = Math.Min(_ticks + 1, MaxTick);
            SyncFromTicks();
        }

        private void OnDownClick(object sender, RoutedEventArgs e)
        {
            _ticks = Math.Max(_ticks - 1, MinTick);
            SyncFromTicks();
        }

        private void OnTextWheel(object sender, MouseWheelEventArgs e)
        {
            _ticks += (e.Delta > 0 ? 1 : -1);
            _ticks = Math.Clamp(_ticks, MinTick, MaxTick);
            SyncFromTicks();
            e.Handled = true;
        }

        private void SyncFromTicks()
        {
            if (Transform == null) return;

            double newVal = Transform.ToValue(_ticks);
            Value = newVal;
        }
    }
}


using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfProject.Views
{
    public partial class NumericStepper : UserControl
    {
        private int _ticks;

        public NumericStepper()
        {
            InitializeComponent();
        }

        //================= 変換 =================
        public static readonly DependencyProperty TransformProperty =
            DependencyProperty.Register(
                nameof(Transform),
                typeof(INumericTransform),
                typeof(NumericStepper),
                new PropertyMetadata(
                    new LinearTransform { Scale = 0.1, Offset = 0.0 }, // 既定の線形
                    (d, _) => ((NumericStepper)d).OnTransformChanged()));

        public INumericTransform Transform
        {
            get => (INumericTransform)GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        private void OnTransformChanged()
        {
            // 変換差し替え時にレンジや値を再評価
            CoerceValue(ValueProperty);
            SyncFromTicks();
        }

        //================= Value =================
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumericStepper),
                new FrameworkPropertyMetadata(
                    0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged,
                    CoerceValueInternal));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (NumericStepper)d;
            if (c.Transform == null) return;

            c._ticks = c.Transform.ToTicks((double)e.NewValue);

            var args = new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue, (double)e.NewValue)
            {
                RoutedEvent = ValueChangedEvent
            };
            c.RaiseEvent(args);
        }

        //================= 最小/最大 =================
        // ★ ポイント：既定値は NaN にして「未指定」を表現
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(NumericStepper),
                new PropertyMetadata(double.NaN, OnRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(NumericStepper),
                new PropertyMetadata(double.NaN, OnRangeChanged));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((NumericStepper)d).CoerceValue(ValueProperty);
        }

        // Transform 既定と DP の明示指定をマージした“実効レンジ”
        private (double min, double max) GetEffectiveRange()
        {
            double min =
                double.IsNaN(Minimum)
                ? (Transform?.DefaultMin ?? double.NegativeInfinity)
                : Minimum;

            double max =
                double.IsNaN(Maximum)
                ? (Transform?.DefaultMax ?? double.PositiveInfinity)
                : Maximum;

            if (min > max) (min, max) = (max, min); // 万一の入れ替え
            return (min, max);
        }

        private static object CoerceValueInternal(DependencyObject d, object baseValue)
        {
            var c = (NumericStepper)d;
            var (min, max) = c.GetEffectiveRange();

            double v = (double)baseValue;
            if (v < min) v = min;
            if (v > max) v = max;
            return v;
        }

        //================= イベント =================
        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(ValueChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>),
                typeof(NumericStepper));

        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        //================= UI操作 =================
        private void OnUpClick(object sender, RoutedEventArgs e)
        {
            _ticks++;
            SyncFromTicks();
        }

        private void OnDownClick(object sender, RoutedEventArgs e)
        {
            _ticks--;
            SyncFromTicks();
        }

        private void OnTextWheel(object sender, MouseWheelEventArgs e)
        {
            _ticks += (e.Delta > 0 ? 1 : -1);
            SyncFromTicks();
            e.Handled = true;
        }

        private void SyncFromTicks()
        {
            if (Transform == null) return;

            double newVal = Transform.ToValue(_ticks);

            // 実効レンジでクランプ（Transform 既定 or DP指定）
            var (min, max) = GetEffectiveRange();
            if (newVal < min) newVal = min;
            if (newVal > max) newVal = max;

            Value = newVal;
        }
    }
}


namespace WpfProject.Views
{
    public interface INumericTransform
    {
        double ToValue(int ticks);      // ticks → 表示値
        int    ToTicks(double value);   // 表示値 → ticks

        // 変換プロファイルに紐づく「表示値」の推奨最小/最大（未指定は null）
        double? DefaultMin { get; }
        double? DefaultMax { get; }
    }
}

using System;

namespace WpfProject.Views
{
    public class LinearTransform : INumericTransform
    {
        public double Scale  { get; set; } = 0.1;
        public double Offset { get; set; } = 0.0;

        // ここに“変換とセット”のレンジを持たせる
        public double? DefaultMin { get; set; }  // 例: 0
        public double? DefaultMax { get; set; }  // 例: 10

        public double ToValue(int ticks) => ticks * Scale + Offset;
        public int ToTicks(double value) => (int)Math.Round((value - Offset) / Scale);
    }
}

<Window ...
        xmlns:local="clr-namespace:WpfProject.Views">
    <Window.Resources>
        <local:LinearTransform x:Key="Volt10V" Scale="0.1" Offset="0"
                               DefaultMin="0" DefaultMax="10"/>
        <local:LinearTransform x:Key="TempC" Scale="0.5" Offset="-50"
                               DefaultMin="-50" DefaultMax="150"/>
    </Window.Resources>

    <StackPanel Margin="16">
        <!-- 0〜10V、0.1刻み -->
        <local:NumericStepper Value="{Binding Voltage}"
                              Transform="{StaticResource Volt10V}"/>

        <!-- -50〜150℃、0.5刻み（Offset込み） -->
        <local:NumericStepper Value="{Binding Temperature}"
                              Transform="{StaticResource TempC}"
                              Margin="0,12,0,0"/>
    </StackPanel>
</Window>

<local:NumericStepper Value="{Binding Voltage}"
                      Transform="{StaticResource Volt10V}"
                      Maximum="8.5"/> <!-- ここだけ上書き -->
