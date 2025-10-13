using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfProject.Models;

namespace WpfProject.Views
{
    // 変更要求（今までのをそのまま使ってOK）
    public sealed record PointChangedMessage(int Index, double X, double Y);
    // 実適用値の通知（返信）
    public sealed record PointAppliedMessage(int Index, double X, double Y);

    public static class MouseWheelNudgeBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(MouseWheelNudgeBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));
        public static void SetIsEnabled(DependencyObject o, bool v) => o.SetValue(IsEnabledProperty, v);
        public static bool GetIsEnabled(DependencyObject o) => (bool)o.GetValue(IsEnabledProperty);

        public static readonly DependencyProperty AxisProperty =
            DependencyProperty.RegisterAttached("Axis", typeof(string), typeof(MouseWheelNudgeBehavior),
                new PropertyMetadata("X"));
        public static void SetAxis(DependencyObject o, string v) => o.SetValue(AxisProperty, v);
        public static string GetAxis(DependencyObject o) => (string)o.GetValue(AxisProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement el) return;
            if ((bool)e.NewValue) el.PreviewMouseWheel += OnWheel;
            else el.PreviewMouseWheel -= OnWheel;
        }

        private static void OnWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not FrameworkElement fe) return;
            if (fe.DataContext is not PointItem item) return;

            double step = item.Step * (e.Delta > 0 ? 1 : -1);
            if (GetAxis(fe) == "X") item.X += step;
            else item.Y += step;

            e.Handled = true; // 背景ズーム防止
        }
    }

    // 部分クラス + ソースジェネレータ
    public partial class PointItem : ObservableObject
    {
        public int Index { get; }
        public double Step { get; set; } = 0.1; // ホイール/ボタンの刻み

        [ObservableProperty] private double x;
        [ObservableProperty] private double y;

        public PointItem(int index, double x, double y)
        {   Index = index; 
            this.x = x; 
            this.y = y; 
        }

        // --- 値が変わったら Messenger 送信（抑止中は送らない） ---
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

        // ▲/▼ ボタン用
        [RelayCommand] private void IncX() => X += Step;
        [RelayCommand] private void DecX() => X -= Step;
        [RelayCommand] private void IncY() => Y += Step;
        [RelayCommand] private void DecY() => Y -= Step;

        // === 再通知抑止の仕組み ===
        [ThreadStatic] private static int _suppressDepth;

        // 👇 これが抜けてた！（IsSuppressed プロパティ）
        private static bool IsSuppressed => _suppressDepth > 0;

        public static IDisposable SuppressBroadcast() => new Suppressor();

        private readonly struct Suppressor : IDisposable
        {
            public Suppressor() { _suppressDepth++; }
            public void Dispose() { _suppressDepth--; }
        }
    }


    public class ScotPlot : ScottPlot.WPF.WpfPlot
    {
        private bool IsReady => _scatter is not null && _xs is not null && _ys is not null;

        private Scatter? _scatter;
        private double[]? _xs, _ys;
        private int _dragIndex = -1;

        public bool DragEnabled { get; set; } = true;
        public int HitTestPixelRadius { get; set; } = 12;

        //public event Action<int, double, double>? PointMoved;

        // ダイアログで編集中のインデックス（疎結合でもどの点かはここで保持）
        private int _editingIndex = -1;

        // （ロック関連は省略/残してOK）
        private const double LockHysteresisPx = 3.0; // 誤判定防止（数pxの遊び）
        private enum LockMode { None, LockX, LockY }
        private LockMode _lockMode = LockMode.None;
        private double _startPx, _startPy;
        private double _startX, _startY;

        public ScotPlot()
        {
            Background = Brushes.Transparent;
            Focusable = true;

            Loaded += OnLoaded;

            PreviewMouseDown += OnPreviewMouseDown;
            PreviewMouseMove += OnPreviewMouseMove;
            PreviewMouseUp += OnPreviewMouseUp;

            MouseLeave += (_, __) =>
            {
                _dragIndex = -1;
                _lockMode = LockMode.None;
                Cursor = Cursors.Arrow;
                ReleaseMouseCapture();
            };

            WeakReferenceMessenger.Default.Register<PointChangedMessage>(this, (_, m) =>
            {
                if (!IsReady) return;
                UpdatePoint(m.Index, m.X, m.Y); // ここで前後点の境界でクランプ

                // 実際に適用された値を返信（ダイアログや一覧に正を伝える）
                var appliedX = _xs![m.Index];
                var appliedY = _ys![m.Index];
                WeakReferenceMessenger.Default.Send(new PointAppliedMessage(m.Index, appliedX, appliedY));
            });
        }

        public ObservableCollection<PointItem> Points { get; } = new();

        private void RebuildPoints()
        {
            Points.Clear();
            if (_xs is null || _ys is null) return;
            for (int i = 0; i < _xs.Length; i++)
                Points.Add(new PointItem(i, _xs[i], _ys[i]));
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            if (_xs is null || _ys is null)
            {
                var rnd = new Random(0);
                var xs = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
                var ys = xs.Select(_ => rnd.NextDouble() * 10).ToArray();
                SetData(xs, ys, "Double-Click to Edit (Wheel updates plot)");
            }
        }

        public void SetData(double[] xs, double[] ys, string? title = null)
        {
            _xs = xs ?? throw new ArgumentNullException(nameof(xs));
            _ys = ys ?? throw new ArgumentNullException(nameof(ys));
            if (xs is null || ys is null) throw new ArgumentNullException();
            if (xs.Length != ys.Length) throw new ArgumentException("xs and ys must have same length");
            _xs = xs; _ys = ys;

            Plot.Clear();
            _scatter = Plot.Add.Scatter(_xs, _ys);
            _scatter.MarkerSize = 6;
            _scatter.LineWidth = 1.5F;
            if (!string.IsNullOrWhiteSpace(title)) _scatter.Label = title;

            Plot.Title(title ?? "ScotPlot");
            Plot.XLabel("X");
            Plot.YLabel("Y");

            RebuildPoints();   // ← これを必ず呼ぶ
            Refresh();
        }

        private const double MinDelta = 1e-4; // お好みで 1e-6〜0.01
        private bool _suppressPointSync;      // 再帰防止


        private (double min, double max) GetBoundsForIndex(int i)
        {
            if (_xs is null) return (double.NegativeInfinity, double.PositiveInfinity); // ★

            double min = double.NegativeInfinity;
            double max = double.PositiveInfinity;
            if (_xs is null) return (min, max);

            if (i > 0) min = _xs[i - 1] + MinDelta;
            if (i < _xs.Length - 1) max = _xs[i + 1] - MinDelta;

            // もし min > max（隣が詰み）なら少しだけ幅を確保
            if (min > max) { double mid = (min + max) / 2; min = mid - MinDelta / 2; max = mid + MinDelta / 2; }
            return (min, max);
        }


        // ScotPlot.cs 内（_xs/_ys 配列と Plot を持っている前提）
        private void UpdatePoint(int index, double x, double y)
        {
            if (_xs is null || _ys is null || _scatter is null) return; // ★
            if (_xs is null || _ys is null) return;
            if (index < 0 || index >= _xs.Length) return;

            var (min, max) = GetBoundsForIndex(index);
            double clampedX = Math.Max(min, Math.Min(max, x));


            // ① 元データ（配列）を更新
            _xs[index] = clampedX;
            _ys[index] = y;

            // ② 右側の一覧（Points: ObservableCollection<PointItem>）にも反映
            //    PointItem は ObservableObject（Toolkit）で X/Y をプロパティにしている前提
            // Points へ反映（INPC → 一覧更新）。再帰防止付き
            if (!_suppressPointSync && index < Points.Count)
            {
                try
                {
                    _suppressPointSync = true;
                    Points[index].X = clampedX;
                    Points[index].Y = y;
                }
                finally { _suppressPointSync = false; }
            }

            // ③ グラフを再描画
            Refresh();

            // ④（任意）外へ通知したいならイベントを投げる
            //PointMoved?.Invoke(index, x, y);
        }


        public (double[] xs, double[] ys) GetData()
        {
            if (_xs is null || _ys is null) throw new InvalidOperationException("No data");
            return (_xs, _ys);
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_scatter is null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            // ダブルクリック → 編集ダイアログ（弱イベントで購読）
            if (e.ClickCount == 2)
            {
                TryOpenEditDialogAt(e);
                e.Handled = true;
                return;
            }

            if (!DragEnabled) return;

            Focus(); Keyboard.Focus(this);

            var pos = e.GetPosition(this);
            _startPx = pos.X * DisplayScale;
            _startPy = pos.Y * DisplayScale;

            Coordinates mouse = Plot.GetCoordinates(new Pixel(_startPx, _startPy));
            var nearest = _scatter.Data.GetNearest(mouse, Plot.LastRender, HitTestPixelRadius);
            _dragIndex = nearest.IsReal ? nearest.Index : -1;

            if (_dragIndex >= 0 && _xs is not null && _ys is not null)
            {
                _startX = _xs[_dragIndex];
                _startY = _ys[_dragIndex];

                _lockMode = LockMode.None;
                Cursor = Cursors.SizeAll;
                CaptureMouse();
                e.Handled = true;
            }
        }

        private void TryOpenEditDialogAt(MouseEventArgs e)
        {
            if (_scatter is null || _xs is null || _ys is null) return;

            var pos = e.GetPosition(this);
            double px = pos.X * DisplayScale;
            double py = pos.Y * DisplayScale;

            Coordinates mouse = Plot.GetCoordinates(new Pixel(px, py));
            var nearest = _scatter.Data.GetNearest(mouse, Plot.LastRender, HitTestPixelRadius);
            int idx = nearest.IsReal ? nearest.Index : -1;
            if (idx < 0) return;

            _editingIndex = idx;

            double curX = _xs[idx];
            double curY = _ys[idx];

            Point screenDevice = PointToScreen(pos);

            var dlg = new PointEditWindow(_editingIndex,curX, curY, screenDevice)
            {
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.Manual,
                Topmost = true
            };

            dlg.Closed += (_, __) =>
            {
                _editingIndex = -1;
            };

            dlg.ShowDialog();
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_scatter is null || _dragIndex < 0 || _xs is null || _ys is null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var pos = e.GetPosition(this);
            double px = pos.X * DisplayScale;
            double py = pos.Y * DisplayScale;

            // Shiftロックの判定
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (!shift)
            {
                // Shiftを離したらロック解除
                _lockMode = LockMode.None;
            }
            else if (_lockMode == LockMode.None)
            {
                // まだロック未決定 → どちらの移動が大きいかで決める（ヒステリシスあり）
                double dxPx = Math.Abs(px - _startPx);
                double dyPx = Math.Abs(py - _startPy);

                if (dxPx > dyPx + LockHysteresisPx)
                    _lockMode = LockMode.LockY; // 水平ドラッグ優勢 → Yのみ動かす（X固定）
                else if (dyPx > dxPx + LockHysteresisPx)
                    _lockMode = LockMode.LockX; // 垂直ドラッグ優勢 → Xのみ動かす（Y固定）
                                                // ほぼ同じならまだ決めない（次のMoveで決まる）
            }

            // ピクセル→データ座標
            Coordinates mouse = Plot.GetCoordinates(new Pixel(px, py));
            double newX = mouse.X;
            double newY = mouse.Y;

            // ロック適用（Shift押下中のみ）
            if (shift)
            {
                if (_lockMode == LockMode.LockX)      // 垂直ドラッグ → X固定
                    newX = _startX;
                else if (_lockMode == LockMode.LockY) // 水平ドラッグ → Y固定
                    newY = _startY;
            }

            // 中央ゲートで制約＆反映
            UpdatePoint(_dragIndex, newX, newY);
            e.Handled = true;
        }
        private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragIndex >= 0)
            {
                _dragIndex = -1;
                _lockMode = LockMode.None;
                Cursor = Cursors.Arrow;
                ReleaseMouseCapture();
                Refresh();
                e.Handled = true;
            }
        }
    }
}