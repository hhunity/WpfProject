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

        // -------- Dependency Properties --------

        // 値（双方向既定）
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

        // 刻み幅
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
            // 0 や負数は使いづらいのでガード（必要なければ外してください）
            if ((double)e.NewValue <= 0)
                c.Step = 0.1;
        }

        // 最小/最大
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(NumericStepper),
                new PropertyMetadata(double.NegativeInfinity, OnRangeChanged));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(NumericStepper),
                new PropertyMetadata(double.PositiveInfinity, OnRangeChanged));

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 範囲が変わったら Value をクランプ
            var c = (NumericStepper)d;
            c.CoerceValue(ValueProperty);
        }

        // 値変更イベント（必要なら外部で購読可能）
        public static readonly RoutedEvent ValueChangedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(ValueChanged),
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<double>),
                typeof(NumericStepper));

        public event RoutedPropertyChangedEventHandler<double> ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (NumericStepper)d;
            var args = new RoutedPropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue)
            {
                RoutedEvent = ValueChangedEvent
            };
            c.RaiseEvent(args);
        }

        private static object CoerceValueInternal(DependencyObject d, object baseValue)
        {
            var c = (NumericStepper)d;
            double v = (double)baseValue;
            if (v < c.Minimum) v = c.Minimum;
            if (v > c.Maximum) v = c.Maximum;
            return v;
        }

        // -------- UI 操作 --------

        private void OnUpClick(object sender, RoutedEventArgs e)
        {
            Value = CoerceValueInternal(this, Value + Step) is double v ? v : Value;
        }

        private void OnDownClick(object sender, RoutedEventArgs e)
        {
            Value = CoerceValueInternal(this, Value - Step) is double v ? v : Value;
        }

        private void OnTextWheel(object sender, MouseWheelEventArgs e)
        {
            Value = CoerceValueInternal(this, Value + (e.Delta > 0 ? Step : -Step)) is double v ? v : Value;
            e.Handled = true; // 背景のスクロールやズームを抑止
        }
    }
}