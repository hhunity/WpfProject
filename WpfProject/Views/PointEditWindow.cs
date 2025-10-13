using CommunityToolkit.Mvvm.Messaging;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfProject.Views
{
    public class PointEditWindow : Window
    {
        // 固定サイズ（桁数で伸び縮みしない）
        private readonly TextBox _tbX = new()
        {
            Width = 120,
            Height = 32,
            Margin = new Thickness(3),
            IsReadOnly = true,
            Background = new SolidColorBrush(Color.FromArgb(190, 245, 245, 245)), // 半透明
            BorderBrush = new SolidColorBrush(Color.FromArgb(220, 90, 90, 90)),
            BorderThickness = new Thickness(1),
            Foreground = Brushes.Black,
            CaretBrush = Brushes.Black,
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalContentAlignment = HorizontalAlignment.Right
        };

        private readonly TextBox _tbY = new()
        {
            Width = 120,
            Height = 32,
            Margin = new Thickness(3),
            IsReadOnly = true,
            Background = new SolidColorBrush(Color.FromArgb(190, 245, 245, 245)), // 半透明
            BorderBrush = new SolidColorBrush(Color.FromArgb(220, 90, 90, 90)),
            BorderThickness = new Thickness(1),
            Foreground = Brushes.Black,
            CaretBrush = Brushes.Black,
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalContentAlignment = HorizontalAlignment.Right
        };

        private readonly Button _btnClose = new()
        {
            Content = "Close",
            Margin = new Thickness(5, 5, 5, 5),
            MinWidth = 50,
            Padding = new Thickness(10, 6, 10, 6),
            Background = new SolidColorBrush(Color.FromArgb(200, 235, 235, 235)), // 半透明
            BorderBrush = new SolidColorBrush(Color.FromArgb(220, 100, 100, 100)),
            Foreground = Brushes.Black
        };

        // 上下（サイズは“Stretch”でGridの半分に自動フィット）
        private readonly RepeatButton _xUp = new() { Content = "▲" };
        private readonly RepeatButton _xDown = new() { Content = "▼" };
        private readonly RepeatButton _yUp = new() { Content = "▲" };
        private readonly RepeatButton _yDown = new() { Content = "▼" };

        public double ValueX { get; private set; }
        public double ValueY { get; private set; }

        private readonly Point _anchorScreenDevice;
        private const double Step = 0.1;

        private readonly int _index;
        private bool _suppress; // 内部更新時のループ防止

        // ctor は (int index, double currentX, double currentY, Point anchorScreen)
        public PointEditWindow(int index, double currentX, double currentY, Point anchorScreenDevice)
        {
            _index = index;
            // ウィンドウ：半透明（矩形角は見せない＝内側Borderで角丸＆Clip）
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;        // 窓自体は透明
            Topmost = true;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.Manual;

            // ← “ウィンドウを半透明に見せる”のは内側のPanelで実現
            //    こうすると角丸でも四隅の黒ずみやズレが出にくいです
            Width = 210;
            Height = 150;

            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            _anchorScreenDevice = anchorScreenDevice;

            // 半透明のパネル（角丸＋枠）— 見た目の“半透明ウィンドウ”
            var chrome = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(30, 80, 80, 80)), // 半透明ダーク
                BorderBrush = new SolidColorBrush(Color.FromArgb(220, 255, 255, 255)),
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(10),
                SnapsToDevicePixels = true
            };

            // 角丸クリップ（四角に見えないように）
            Loaded += (_, __) =>
            {
                chrome.Clip = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), 14, 14);
            };

            // 中身レイアウト
            var grid = new Grid { Margin = new Thickness(2) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            StyleStepper(_xUp); StyleStepper(_xDown);
            StyleStepper(_yUp); StyleStepper(_yDown);

            var rowX = MakeRow("X:", _tbX, _xUp, _xDown);
            var rowY = MakeRow("Y:", _tbY, _yUp, _yDown);
            var rowBt = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            rowBt.Children.Add(_btnClose);

            Grid.SetRow(rowX, 0); grid.Children.Add(rowX);
            Grid.SetRow(rowY, 1); grid.Children.Add(rowY);
            Grid.SetRow(rowBt, 2); grid.Children.Add(rowBt);

            chrome.Child = grid;
            Content = chrome;

            // 値
            ValueX = currentX; ValueY = currentY;
            _tbX.Text = currentX.ToString("G", CultureInfo.InvariantCulture);
            _tbY.Text = currentY.ToString("G", CultureInfo.InvariantCulture);

            // クリック位置のすぐ横（DPI補正）
            Loaded += (_, __) =>
            {
                var src = PresentationSource.FromVisual(this);
                var m = src?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
                var anchorDip = m.Transform(_anchorScreenDevice);

                const double offset = 50;
                Left = anchorDip.X + offset;
                Top = anchorDip.Y - (Height / 2);

                _tbX.Focus();
            };

            // 入力制限：ホイール/上下のみ
            _tbX.PreviewTextInput += (s, e) => e.Handled = true;
            _tbY.PreviewTextInput += (s, e) => e.Handled = true;
            DataObject.AddPastingHandler(_tbX, (s, e) => e.CancelCommand());
            DataObject.AddPastingHandler(_tbY, (s, e) => e.CancelCommand());
            _tbX.ContextMenu = null; _tbY.ContextMenu = null;

            // ホイール
            _tbX.PreviewMouseWheel += OnWheelX;
            _tbY.PreviewMouseWheel += OnWheelY;

            // 上下（ボタンサイズは“Stretch”で自然に半分ずつ）
            _xUp.Click += (_, __) => OnXStep(+1);
            _xDown.Click += (_, __) => OnXStep(-1);
            _yUp.Click += (_, __) => OnYStep(+1);
            _yDown.Click += (_, __) => OnYStep(-1);

            _btnClose.Click += (_, __) => Close();
            PreviewKeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); };

            // 背景ドラッグで移動（TextBox/ボタン以外）
            MouseLeftButtonDown += (_, e) =>
            {
                if (e.Source is not TextBox && e.Source is not ButtonBase) DragMove();
            };
            
            WeakReferenceMessenger.Default.Register<PointAppliedMessage>(this, (_, m) =>
            {
                if (m.Index != _index) return;
                if (!IsVisible) return; // 閉じかけは無視

                Dispatcher.Invoke(() =>
                {
                    _suppress = true;
                    try
                    {
                        ValueX = m.X; ValueY = m.Y;
                        SyncText(_tbX, m.X);
                        SyncText(_tbY, m.Y);
                    }
                    finally { _suppress = false; }
                });
            });

            Closed += (_, __) => WeakReferenceMessenger.Default.Unregister<PointAppliedMessage>(this);
        }

        private static void StyleStepper(RepeatButton rb)
        {
            rb.Margin = new Thickness(0);
            rb.Padding = new Thickness(0);
            rb.Background = new SolidColorBrush(Color.FromArgb(190, 235, 235, 235)); // 半透明
            rb.BorderBrush = new SolidColorBrush(Color.FromArgb(220, 100, 100, 100));
            rb.Foreground = Brushes.Black;
            rb.HorizontalContentAlignment = HorizontalAlignment.Center;
            rb.VerticalContentAlignment = VerticalAlignment.Center;
            rb.Delay = 250;
            rb.Interval = 35;
            // サイズは指定しない（Stretch でGrid行いっぱいに広がる）
        }

        private static UIElement MakeRow(string label, TextBox tb, RepeatButton up, RepeatButton down)
        {
            var sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 2)
            };

            sp.Children.Add(new Label
            {
                Content = label,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                Width = 18
            });

            sp.Children.Add(tb);

            // ← “テキストボックスと同じ高さ”の縦2分割パネル
            var stepPanel = new Grid
            {
                Margin = new Thickness(3, 0, 0, 0),
                Width = 32,                     // 押しやすい幅
                Height = tb.Height,             // TextBox と同じ高さに固定
                VerticalAlignment = VerticalAlignment.Center
            };
            stepPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            stepPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // ボタンはStretch（行いっぱいに広がる＝高さは自動で半分ずつ）
            up.HorizontalAlignment = HorizontalAlignment.Stretch;
            up.VerticalAlignment = VerticalAlignment.Stretch;
            down.HorizontalAlignment = HorizontalAlignment.Stretch;
            down.VerticalAlignment = VerticalAlignment.Stretch;

            Grid.SetRow(up, 0);
            Grid.SetRow(down, 1);
            stepPanel.Children.Add(up);
            stepPanel.Children.Add(down);

            sp.Children.Add(stepPanel);
            return sp;
        }

        private void SyncText(TextBox? tb, double val)
        {
            if (tb is null) return;
            if (!tb.IsLoaded || !IsVisible) return; // ウィンドウ閉じ際の受信などを無視

            if (!tb.Dispatcher.CheckAccess())
            {
                tb.Dispatcher.Invoke(() => SyncText(tb, val));
                return;
            }

            _suppress = true;
            try
            {
                tb.Text = val.ToString("G", CultureInfo.InvariantCulture);
                tb.CaretIndex = tb.Text.Length;
            }
            finally { _suppress = false; }
        }
        private void OnXStep(int dir)
        {
            ValueX += dir * Step;
            SyncText(_tbX, ValueX);
            RaiseNow();   // ScotPlot へ変更要求をMessengerで送る
        }

        private void OnYStep(int dir)
        {
            ValueY += dir * Step;
            SyncText(_tbY, ValueY);
            RaiseNow();
        }

        private void OnWheelX(object? s, MouseWheelEventArgs e)
        {
            ValueX += (e.Delta > 0 ? Step : -Step);
            SyncText(_tbX, ValueX);
            RaiseNow();
            e.Handled = true;
        }

        private void OnWheelY(object? s, MouseWheelEventArgs e)
        {
            ValueY += (e.Delta > 0 ? Step : -Step);
            SyncText(_tbY, ValueY);
            RaiseNow();
            e.Handled = true;
        }

        private void RaiseNow()
        {
            if (_suppress) return;
            WeakReferenceMessenger.Default.Send(new PointChangedMessage(_index, ValueX, ValueY));
        }
    }
}