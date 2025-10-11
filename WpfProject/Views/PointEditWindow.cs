using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfProject.Views
{
    public class PointValueChangedEventArgs : EventArgs
    {
        public double X { get; }
        public double Y { get; }

        public PointValueChangedEventArgs(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
    public class PointEditWindow : Window
    {
        private readonly TextBox _tbX = new TextBox { MinWidth = 120, Margin = new Thickness(8) };
        private readonly TextBox _tbY = new TextBox { MinWidth = 120, Margin = new Thickness(8) };
        private readonly Button _btnOk = new Button { Content = "OK", IsDefault = true, Margin = new Thickness(8), MinWidth = 70 };
        private readonly Button _btnCancel = new Button { Content = "Cancel", IsCancel = true, Margin = new Thickness(8), MinWidth = 70 };

        public double ValueX { get; private set; }
        public double ValueY { get; private set; }

        private readonly Point _anchorScreenDevice; // クリック位置（デバイスピクセル）
        private const double WheelStep = 0.1;

        // ★ 疎結合イベント（WeakEventManagerで購読させる）
        public event EventHandler<PointValueChangedEventArgs>? ValueChanged;

        public PointEditWindow(double currentX, double currentY, Point anchorScreenDevice)
        {
            Title = "点の編集";
            WindowStyle = WindowStyle.ToolWindow;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.WidthAndHeight;
            ShowInTaskbar = false;

            _anchorScreenDevice = anchorScreenDevice;

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var pnlX = new StackPanel { Orientation = Orientation.Horizontal };
            pnlX.Children.Add(new Label { Content = "X:", Margin = new Thickness(0, 8, 0, 8) });
            pnlX.Children.Add(_tbX);

            var pnlY = new StackPanel { Orientation = Orientation.Horizontal };
            pnlY.Children.Add(new Label { Content = "Y:", Margin = new Thickness(0, 8, 0, 8) });
            pnlY.Children.Add(_tbY);

            var pnlBtn = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            pnlBtn.Children.Add(_btnOk);
            pnlBtn.Children.Add(_btnCancel);

            Grid.SetRow(pnlX, 0);
            Grid.SetRow(pnlY, 1);
            Grid.SetRow(pnlBtn, 2);

            grid.Children.Add(pnlX);
            grid.Children.Add(pnlY);
            grid.Children.Add(pnlBtn);
            Content = grid;

            _tbX.Text = currentX.ToString("G", CultureInfo.InvariantCulture);
            _tbY.Text = currentY.ToString("G", CultureInfo.InvariantCulture);

            // 位置は Loaded で（DPI補正して“すぐ横”に）
            Loaded += (_, __) =>
            {
                var src = PresentationSource.FromVisual(this);
                var m = src?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
                var anchorDip = m.Transform(_anchorScreenDevice);

                const double offset = 2;
                double desiredLeft = anchorDip.X + offset;
                double desiredTop = anchorDip.Y - ActualHeight / 2;

                var wa = SystemParameters.WorkArea; // DIP
                if (desiredLeft + ActualWidth > wa.Right)
                    desiredLeft = anchorDip.X - ActualWidth - offset;
                if (desiredTop < wa.Top) desiredTop = wa.Top + 2;
                if (desiredTop + ActualHeight > wa.Bottom) desiredTop = wa.Bottom - ActualHeight - 2;

                Left = desiredLeft;
                Top = desiredTop;

                _tbX.SelectAll();
                _tbX.Focus();
            };

            _btnOk.Click += (_, __) =>
            {
                if (TryParseText(out double x, out double y))
                {
                    ValueX = x; ValueY = y;
                    // OK時にも最終値で通知（リアルタイムを取り逃した場合の保険）
                    ValueChanged?.Invoke(this, new PointValueChangedEventArgs(x, y));
                    DialogResult = true;
                    Close();
                }
            };

            _tbX.KeyDown += OnTextKeyDown;
            _tbY.KeyDown += OnTextKeyDown;

            // ★ ホイールで ±0.1 → その都度イベント発火（リアルタイム更新）
            _tbX.PreviewMouseWheel += OnTextWheel;
            _tbY.PreviewMouseWheel += OnTextWheel;
        }

        private void OnTextKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (TryParseText(out double x, out double y))
                {
                    ValueX = x; ValueY = y;
                    ValueChanged?.Invoke(this, new PointValueChangedEventArgs(x, y));
                    DialogResult = true;
                    Close();
                }
                e.Handled = true;
            }
        }

        private void OnTextWheel(object? sender, MouseWheelEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (!double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                val = 0;

            val += (e.Delta > 0 ? WheelStep : -WheelStep);
            tb.Text = val.ToString("G", CultureInfo.InvariantCulture);
            tb.CaretIndex = tb.Text.Length;

            // 両方の現在値を読んで通知（X/Yのどちらを回しても両方を渡す）
            if (TryParseText(out double x, out double y))
                ValueChanged?.Invoke(this, new PointValueChangedEventArgs(x, y));

            e.Handled = true; // 背景ズーム抑止
        }

        private bool TryParseText(out double x, out double y)
        {
            bool okX = double.TryParse(_tbX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out x);
            bool okY = double.TryParse(_tbY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out y);
            return okX && okY;
        }
    }
}