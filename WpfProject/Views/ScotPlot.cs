using ScottPlot;
using ScottPlot;
using ScottPlot.Plottables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WpfProject.Views
{
    public class ScotPlot : ScottPlot.WPF.WpfPlot
    {
        private Scatter? _scatter;
        private double[]? _xs, _ys;
        private int _dragIndex = -1;

        public bool DragEnabled { get; set; } = true;

        /// <summary>ドラッグで X を固定したいとき true</summary>
        public bool LockX { get; set; } = false;

        /// <summary>ドラッグで Y を固定したいとき true</summary>
        public bool LockY { get; set; } = false;

        /// <summary>ドラッグ対象探索の許容ピクセル半径</summary>
        public int HitTestPixelRadius { get; set; } = 12;

        /// <summary>点が動いたときに通知（インデックスと新座標）</summary>
        public event Action<int, double, double>? PointMoved;


        ////////////////////////////////////////////////////////////////////////////////////////
        // Shiftロックの状態
        private enum LockMode { None, LockX, LockY }
        private LockMode _lockMode = LockMode.None;
        private double _startPx, _startPy;       // 物理解像度のピクセル
        private double _startX, _startY;         // データ座標
        // Shfitロックここまで
        ////////////////////////////////////////////////////////////////////////////////////////

        public ScotPlot()
        {
            Loaded += OnLoaded;

            // 既定のパン/ズームよりも先に受け取って止める
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
                SetData(xs, ys, "Draggable Points (Shiftで水平/垂直ロック)");
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
            if (!string.IsNullOrWhiteSpace(title)) _scatter.LegendText = title;

            Plot.Title(title ?? "Draggable Scatter");
            Plot.XLabel("X"); Plot.YLabel("Y");
            Refresh();
        }

        public (double[] xs, double[] ys) GetData()
        {
            if (_xs is null || _ys is null) throw new InvalidOperationException("No data");
            return (_xs, _ys);
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!DragEnabled || _scatter is null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var pos = e.GetPosition(this);

            ////////////////////////////////////////////////////////////////////////////////////////
            //startPx/_startPy はマウスのピクセル位置（実際の画面上の距離を比較用）。
            _startPx = pos.X * DisplayScale;
            _startPy = pos.Y * DisplayScale;
            // どの点をドラッグするか決定    
            ////////////////////////////////////////////////////////////////////////////////////////

            Coordinates mouse = Plot.GetCoordinates(new Pixel(_startPx, _startPy));
            var nearest = _scatter.Data.GetNearest(mouse, Plot.LastRender, HitTestPixelRadius);
            _dragIndex = nearest.IsReal ? nearest.Index : -1;

            if (_dragIndex >= 0 && _xs is not null && _ys is not null)
            {
                ////////////////////////////////////////////////////////////////////////////////////////
                //_startX/_startY はそのときのデータ座標（ロック時に固定値として使う）。
                _startX = _xs[_dragIndex];
                _startY = _ys[_dragIndex];
                ////////////////////////////////////////////////////////////////////////////////////////

                _lockMode = LockMode.None; // 毎回リセット
                Cursor = Cursors.SizeAll;
                CaptureMouse();
                e.Handled = true; // 既定のパン/ズームをブロック
            }
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_scatter is null || _dragIndex < 0 || _xs is null || _ys is null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var pos = e.GetPosition(this);
            double px = pos.X * DisplayScale;
            double py = pos.Y * DisplayScale;

            ////////////////////////////////////////////////////////////////////////////////////////
            // Shiftロック判定（Excel風）: 押してる間はどちらかの軸に拘束
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (!shift)
            {
                // Shiftを離したらロック解除
                _lockMode = LockMode.None;
            }
            else if (_lockMode == LockMode.None)
            {
                // まだロック未決定 → ドラッグ方向の大きい方に決める（数ピクセルのヒステリシス推奨）
                double dxPx = Math.Abs(px - _startPx);
                double dyPx = Math.Abs(py - _startPy);

                const double hysteresisPx = 3.0; // 小さい揺れで誤判定しない
                if (dxPx > dyPx + hysteresisPx)
                    _lockMode = LockMode.LockY; // 水平ドラッグ → Yのみ変化（X固定）
                else if (dyPx > dxPx + hysteresisPx)
                    _lockMode = LockMode.LockX; // 垂直ドラッグ → Xのみ変化（Y固定）
                // ほぼ同じならまだ決めない（次のMoveで決まる）
            }
            ////////////////////////////////////////////////////////////////////////////////////////

            // ピクセル→座標
            Coordinates mouse = Plot.GetCoordinates(new Pixel(px, py));

            double newX = mouse.X;
            double newY = mouse.Y;

            // ロック適用
            if (shift)
            {
                if (_lockMode == LockMode.LockX)      // 垂直ドラッグ → X固定
                    newX = _startX;
                else if (_lockMode == LockMode.LockY) // 水平ドラッグ → Y固定
                    newY = _startY;
            }

            _xs[_dragIndex] = newX;
            _ys[_dragIndex] = newY;

            Refresh();
            PointMoved?.Invoke(_dragIndex, newX, newY);
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
