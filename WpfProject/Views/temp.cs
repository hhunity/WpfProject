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


using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;

namespace WpfProject.Views
{
    public partial class PointEditWindow : Window
    {
        // どの点を編集しているか
        public int Index { get; }

        // バインド用 DP（NumericStepper の Value と TwoWay）
        public static readonly DependencyProperty ValueXProperty =
            DependencyProperty.Register(nameof(ValueX), typeof(double),
                typeof(PointEditWindow), new PropertyMetadata(0.0));
        public static readonly DependencyProperty ValueYProperty =
            DependencyProperty.Register(nameof(ValueY), typeof(double),
                typeof(PointEditWindow), new PropertyMetadata(0.0));

        public double ValueX { get => (double)GetValue(ValueXProperty); set => SetValue(ValueXProperty, value); }
        public double ValueY { get => (double)GetValue(ValueYProperty); set => SetValue(ValueYProperty, value); }

        // クリック位置（デバイス座標）— 近くに出すため
        private readonly Point _anchorScreenDevice;

        public PointEditWindow(int index, double currentX, double currentY, Point anchorScreenDevice)
        {
            InitializeComponent();
            DataContext = this;

            Index = index;
            ValueX = currentX;
            ValueY = currentY;
            _anchorScreenDevice = anchorScreenDevice;

            // ScotPlot からの適用済み値（クランプ後）を受け取り、表示を整合
            WeakReferenceMessenger.Default.Register<PointAppliedMessage>(this, (_, m) =>
            {
                if (m.Index != Index) return;
                // ここで再入ループは発生しない（NumericStepper は Value をそのまま描画）
                SetCurrentValue(ValueXProperty, m.X);
                SetCurrentValue(ValueYProperty, m.Y);
            });

            Loaded += (_, __) =>
            {
                // DPI変換して「クリック地点のすぐ横」に出す
                var src = PresentationSource.FromVisual(this);
                var m = src?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
                var dip = m.Transform(_anchorScreenDevice);

                const double offset = 10; // ほぼ隣
                Left = dip.X + offset;
                Top = dip.Y - (ActualHeight / 2);

                // 画面内クランプ
                var wa = SystemParameters.WorkArea;
                if (Left + ActualWidth > wa.Right) Left = wa.Right - ActualWidth - 4;
                if (Top + ActualHeight > wa.Bottom) Top = wa.Bottom - ActualHeight - 4;
                if (Left < wa.Left) Left = wa.Left + 4;
                if (Top < wa.Top) Top = wa.Top + 4;
            };

            // Esc で閉じる
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };
            // 背景ドラッグで位置移動
            MouseLeftButtonDown += (_, e) =>
            {
                if (e.OriginalSource is not System.Windows.Controls.Primitives.ButtonBase)
                    DragMove();
            };
        }

        // NumericStepper の ValueChanged（X/Yどちらも同じハンドラでOK）
        private void OnStepperValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 変更が入るたびに ScotPlot へ変更要求を送る（クランプは ScotPlot 側）
            WeakReferenceMessenger.Default.Send(new PointChangedMessage(Index, ValueX, ValueY));
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}

<Window x:Class="WpfProject.Views.PointEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:WpfProject.Views"
        Title="点の編集" Width="240" Height="160"
        WindowStyle="None" ResizeMode="NoResize"
        AllowsTransparency="True" Background="Transparent"
        ShowInTaskbar="False" Topmost="True">
    <Window.Resources>
        <!-- 1tick=0.1、tick範囲の例（必要に応じて変えてOK） -->
        <local:LinearTransform x:Key="Linear01"
                               Scale="0.1" Offset="0"
                               DefaultMinTick="0" DefaultMaxTick="1000"/>
        <Style TargetType="TextBlock">
            <Setter Property="VerticalAlignment" Value="Center"/>
            <Setter Property="Margin" Value="0,0,6,0"/>
            <Setter Property="Foreground" Value="Black"/>
        </Style>
    </Window.Resources>

    <!-- 半透明の角丸パネル（ウィンドウ本体は透明） -->
    <Border CornerRadius="12"
            Background="#33000000"
            BorderBrush="#88FFFFFF"
            BorderThickness="0.5"
            Padding="10">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="8"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="12"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>

            <!-- X -->
            <StackPanel Orientation="Horizontal" Grid.Row="0">
                <TextBlock Text="X:" Width="18"/>
                <local:NumericStepper Width="140"
                                      Value="{Binding ValueX, Mode=TwoWay}"
                                      Transform="{StaticResource Linear01}"
                                      ValueChanged="OnStepperValueChanged"/>
            </StackPanel>

            <!-- Y -->
            <StackPanel Orientation="Horizontal" Grid.Row="2">
                <TextBlock Text="Y:" Width="18"/>
                <local:NumericStepper Width="140"
                                      Value="{Binding ValueY, Mode=TwoWay}"
                                      Transform="{StaticResource Linear01}"
                                      ValueChanged="OnStepperValueChanged"/>
            </StackPanel>

            <!-- Close -->
            <Button Grid.Row="4" Content="Close" Width="70" HorizontalAlignment="Right"
                    Click="OnCloseClick"/>
        </Grid>
    </Border>
</Window>



