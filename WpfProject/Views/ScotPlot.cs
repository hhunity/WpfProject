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

        public ScotPlot()
        {
            Loaded += OnLoaded;

            // WPF マウスイベント
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseLeave += (_, __) => _dragIndex = -1;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this)) return;

            // デザイナ用の仮データ（未設定なら）
            if (_xs is null || _ys is null)
            {
                var rand = new Random(0);
                var x = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
                var y = x.Select(_ => rand.NextDouble() * 10).ToArray();
                SetData(x, y, title: "Draggable Points");
            }
        }

        /// <summary>表示データを丸ごと差し替え</summary>
        public void SetData(double[] xs, double[] ys, string? title = null)
        {
            if (xs is null || ys is null) throw new ArgumentNullException();
            if (xs.Length != ys.Length) throw new ArgumentException("xs and ys must have same length");

            _xs = xs;
            _ys = ys;

            Plot.Clear();

            _scatter = Plot.Add.Scatter(_xs, _ys);
            _scatter.MarkerSize = 6;
            _scatter.LineWidth = 1.5F;
            if (!string.IsNullOrWhiteSpace(title))
                _scatter.LegendText = title;

            Plot.Title(title ?? "Draggable Scatter");
            Plot.XLabel("X");
            Plot.YLabel("Y");

            Refresh();
        }

        /// <summary>現在のデータを取得</summary>
        public (double[] xs, double[] ys) GetData()
        {
            if (_xs is null || _ys is null) throw new InvalidOperationException("No data");
            return (_xs, _ys);
        }

        /// <summary>任意の点をプログラムから更新</summary>
        public void SetPoint(int index, double x, double y)
        {
            if (_xs is null || _ys is null) return;
            if (index < 0 || index >= _xs.Length) return;

            _xs[index] = x;
            _ys[index] = y;
            Refresh();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!DragEnabled || _scatter is null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            // WPF座標 → 物理解像度ピクセル
            var p = e.GetPosition(this);
            double px = p.X * DisplayScale;
            double py = p.Y * DisplayScale;

            // ピクセル → 軸座標
            Coordinates mouse = Plot.GetCoordinates(new Pixel(px, py));

            // 近傍点探索（v5: Scatter.Data.GetNearest）
            var nearest = _scatter.Data.GetNearest(mouse, Plot.LastRender, HitTestPixelRadius);
            _dragIndex = nearest.IsReal ? nearest.Index : -1;

            if (_dragIndex >= 0)
                Cursor = Cursors.SizeAll;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!DragEnabled || _scatter is null || _dragIndex < 0) return;
            if (_xs is null || _ys is null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var p = e.GetPosition(this);
            double px = p.X * DisplayScale;
            double py = p.Y * DisplayScale;

            Coordinates mouse = Plot.GetCoordinates(new Pixel(px, py));

            double newX = LockX ? _xs[_dragIndex] : mouse.X;
            double newY = LockY ? _ys[_dragIndex] : mouse.Y;

            _xs[_dragIndex] = newX;
            _ys[_dragIndex] = newY;

            // ドラッグ中は低品質で軽く
            Refresh();
            PointMoved?.Invoke(_dragIndex, newX, newY);
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!DragEnabled) return;

            if (_dragIndex >= 0)
            {
                _dragIndex = -1;
                Cursor = Cursors.Arrow;
                Refresh(); // 最終フレームを高品質で
            }
        }
    }
}
