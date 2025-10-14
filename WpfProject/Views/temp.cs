using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfProject.Views
{
    public partial class NumericStepper : UserControl
    {
        private bool _updatingFromTicks;
        private bool _updatingFromValue;

        public NumericStepper() => InitializeComponent();

        // ===== Transform（Tick→Value変換を差し替え可能） =====
        public static readonly DependencyProperty TransformProperty =
            DependencyProperty.Register(
                nameof(Transform),
                typeof(INumericTransform),
                typeof(NumericStepper),
                new PropertyMetadata(new LinearTransform(), (d, _) => ((NumericStepper)d).SyncFromTicks()));

        public INumericTransform Transform
        {
            get => (INumericTransform)GetValue(TransformProperty);
            set => SetValue(TransformProperty, value);
        }

        // ===== Ticks =====
        public static readonly DependencyProperty TicksProperty =
            DependencyProperty.Register(
                nameof(Ticks),
                typeof(int),
                typeof(NumericStepper),
                new FrameworkPropertyMetadata(0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnTicksChanged,
                    CoerceTicks));

        public int Ticks
        {
            get => (int)GetValue(TicksProperty);
            set => SetValue(TicksProperty, value);
        }

        private static object CoerceTicks(DependencyObject d, object baseValue)
        {
            var c = (NumericStepper)d;
            int t = (int)baseValue;
            if (c.Transform == null) return t;

            if (t < c.Transform.MinTick) t = c.Transform.MinTick;
            if (t > c.Transform.MaxTick) t = c.Transform.MaxTick;
            return t;
        }

        private static void OnTicksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (NumericStepper)d;
            if (c._updatingFromValue) return;

            c._updatingFromTicks = true;
            c.SyncFromTicks();
            c._updatingFromTicks = false;
        }

        // ===== Value =====
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumericStepper),
                new FrameworkPropertyMetadata(0.0,
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
            if (c.Transform == null || c._updatingFromTicks) return;

            c._updatingFromValue = true;
            c.Ticks = c.Transform.ToTicks((double)e.NewValue);
            c._updatingFromValue = false;
        }

        // ===== 操作系 =====
        private void OnUpClick(object s, RoutedEventArgs e)
            => Ticks += 1;

        private void OnDownClick(object s, RoutedEventArgs e)
            => Ticks -= 1;

        private void OnTextWheel(object s, MouseWheelEventArgs e)
        {
            Ticks += e.Delta > 0 ? 1 : -1;
            e.Handled = true;
        }

        // ===== 同期（Ticks→Value） =====
        private void SyncFromTicks()
        {
            if (Transform == null) return;
            double val = Transform.ToValue(Ticks);
            TbValue.Text = val.ToString("F3");
            SetCurrentValue(ValueProperty, val);
        }
    }

    // === 変換インタフェース ===
    public interface INumericTransform
    {
        double ToValue(int ticks);
        int ToTicks(double value);
        int MinTick { get; }
        int MaxTick { get; }
    }

    // === 線形変換実装（例：1tick=0.1） ===
    public class LinearTransform : INumericTransform
    {
        public double Scale { get; set; } = 0.1;
        public double Offset { get; set; } = 0.0;
        public int MinTick { get; set; } = 0;
        public int MaxTick { get; set; } = 100;

        public double ToValue(int ticks) => ticks * Scale + Offset;
        public int ToTicks(double value) => (int)((value - Offset) / Scale);
    }
}

public partial class PointItem : ObservableObject
{
    public int Index { get; }
    public double Step { get; set; } = 0.1;

    [ObservableProperty] private double x;
    [ObservableProperty] private double y;

    public PointItem(int index, double x, double y)
    {
        Index = index;
        this.x = x;
        this.y = y;

        WeakReferenceMessenger.Default.Register<PointAppliedMessage>(this, (_, m) =>
        {
            if (m.Index != Index) return;
            using (SuppressBroadcast())
            {
                X = m.X;
                Y = m.Y;
            }
        });
    }

    partial void OnXChanged(double value)
    {
        if (!IsSuppressed)
            WeakReferenceMessenger.Default.Send(new PointChangedMessage(Index, X, Y));
    }

    partial void OnYChanged(double value)
    {
        if (!IsSuppressed)
            WeakReferenceMessenger.Default.Send(new PointChangedMessage(Index, X, Y));
    }

    [System.ThreadStatic] private static int _suppressDepth;
    private static bool IsSuppressed => _suppressDepth > 0;
    public static System.IDisposable SuppressBroadcast() => new Suppressor();

    private readonly struct Suppressor : System.IDisposable
    {
        public Suppressor() { _suppressDepth++; }
        public void Dispose() { _suppressDepth--; }
    }
}

<ItemsControl ItemsSource="{Binding Points}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Margin="2">
                <TextBlock Text="{Binding Index}" Width="30" VerticalAlignment="Center"/>
                <local:NumericStepper Value="{Binding X, Mode=TwoWay}" Width="120"
                                      Transform="{StaticResource Linear01}" Margin="2,0"/>
                <local:NumericStepper Value="{Binding Y, Mode=TwoWay}" Width="120"
                                      Transform="{StaticResource Linear01}" Margin="2,0"/>
            </StackPanel>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
<local:NumericStepper 
    Ticks="{Binding VolumeTicks, Mode=TwoWay}" 
    Transform="{StaticResource VolumeTransform}" />


<TextBox
    x:Name="TextBox"
    PreviewMouseWheel="OnTextWheel"
    Text="{Binding Value, RelativeSource={RelativeSource AncestorType=UserControl}, Mode=TwoWay}" />




