using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfProject.Views
{
    public class ScotPlot : ScottPlot.WPF.WpfPlot
    {
        private Scatter? _scatter;
        private double[]? _xs, _ys;
        private int _dragIndex = -1;

        public bool DragEnabled { get; set; } = true;
        public int HitTestPixelRadius { get; set; } = 12;

        public event Action<int, double, double>? PointMoved;

        // ダイアログで編集中のインデックス（疎結合でもどの点かはここで保持）
        private int _editingIndex = -1;

        // （ロック関連は省略/残してOK）
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
            Refresh();
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

            var dlg = new PointEditWindow(curX, curY, screenDevice)
            {
                Owner = Window.GetWindow(this),
                WindowStartupLocation = WindowStartupLocation.Manual,
                Topmost = true
            };

            // ★ 疎結合: 弱イベント購読
            System.Windows.WeakEventManager<PointEditWindow, PointValueChangedEventArgs>
                .AddHandler(dlg, nameof(PointEditWindow.ValueChanged), OnDialogValueChanged);

            dlg.Closed += (_, __) =>
            {
                // 購読解除 & 編集状態クリア
                System.Windows.WeakEventManager<PointEditWindow, PointValueChangedEventArgs>
                    .RemoveHandler(dlg, nameof(PointEditWindow.ValueChanged), OnDialogValueChanged);
                _editingIndex = -1;
            };

            dlg.ShowDialog();
        }

        // ★ ここでダイアログのリアルタイム値変更を受けてプロット更新
        private void OnDialogValueChanged(object? sender, PointValueChangedEventArgs e)
        {
            if (_editingIndex < 0 || _xs is null || _ys is null) return;

            _xs[_editingIndex] = e.X;
            _ys[_editingIndex] = e.Y;

            Refresh();
            PointMoved?.Invoke(_editingIndex, e.X, e.Y);
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_scatter is null || _dragIndex < 0 || _xs is null || _ys is null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var pos = e.GetPosition(this);
            double px = pos.X * DisplayScale;
            double py = pos.Y * DisplayScale;

            Coordinates mouse = Plot.GetCoordinates(new Pixel(px, py));
            _xs[_dragIndex] = mouse.X;
            _ys[_dragIndex] = mouse.Y;

            Refresh();
            PointMoved?.Invoke(_dragIndex, _xs[_dragIndex], _ys[_dragIndex]);
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