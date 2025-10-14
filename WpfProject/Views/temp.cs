using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfProject.Views
{
    public partial class NumericStepper : UserControl
    {
        public NumericStepper()
        {
            InitializeComponent();
        }

        // ---- 内部カウンタ（整数で保持）----
        private int _ticks;

        // ---- 刻み幅（1tickの値）----
        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step),
                typeof(double),
                typeof(NumericStepper),
                new PropertyMetadata(0.1, OnStepChanged));

        public double Step
        {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        private static void OnStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (NumericStepper)d;
            c.CoerceValue(ValueProperty);
        }

        // ---- 表示/バインディング用のValue（double）----
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

            // DPのValueが直接変更されたら内部ticksに反映
            if (c.Step != 0)
                c._ticks = (int)Math.Round((double)e.NewValue / c.Step);

            var args = new RoutedPropertyChangedEventArgs<double>(
                (double)e.OldValue, (double)e.NewValue)
            {
                RoutedEvent = ValueChangedEvent
            };
            c.RaiseEvent(args);
        }

        // ---- 範囲 ----
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericStepper),
                new PropertyMetadata(double.NegativeInfinity, OnRangeChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericStepper),
                new PropertyMetadata(double.PositiveInfinity, OnRangeChanged));

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

        private static object CoerceValueInternal(DependencyObject d, object baseValue)
        {
            var c = (NumericStepper)d;
            double v = (double)baseValue;
            if (v < c.Minimum) v = c.Minimum;
            if (v > c.Maximum) v = c.Maximum;
            return v;
        }

        // ---- イベント ----
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

        // ---- UI操作 ----
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
            double newVal = _ticks * Step;
            newVal = Math.Max(Minimum, Math.Min(Maximum, newVal));

            // DPに反映（＝INPC＋バインド更新＋テキスト表示も自動更新）
            Value = newVal;
        }
    }
}