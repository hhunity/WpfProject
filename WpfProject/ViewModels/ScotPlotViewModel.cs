using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfProject.Views;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace WpfProject.ViewModels
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
        {
            Index = index;
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


    partial class ScotPlotViewModel : ObservableObject
    {
        [RelayCommand]
        private void CopyBtn()
        {
            var win = new CopyWindowView();
            win.ShowDialog();
        }

        private GridLength _savedA, _savedB, _savedC;
        private int? _expandedIndex = null; // null=誰も展開してない

        [ObservableProperty] 
        private GridLength rowAHeight = new(1, GridUnitType.Star);
        
        //[ObservableProperty] private GridLength rowBHeight = new(1, GridUnitType.Star);
        //[ObservableProperty] private GridLength rowCHeight = new(1, GridUnitType.Star);

        [RelayCommand]
        private void Expand(string? parameter)
        {
            if (!int.TryParse(parameter, out int index)) return;

            if (_expandedIndex == index)
            {
                RowAHeight = _savedA;
                //RowBHeight = _savedB;
                //RowCHeight = _savedC;
                _expandedIndex = null;
                return;
            }

            // 展開前のサイズを保存（Splitterでの手動調整も含めて復元できる）
            _savedA = RowAHeight;
            //_savedB = RowBHeight;
            //_savedC = RowCHeight;


            const double big = 4;
            const double small = 1;

            RowAHeight = new GridLength(index == 0 ? big : small, GridUnitType.Star);
            //RowBHeight = new GridLength(index == 1 ? big : small, GridUnitType.Star);
            //RowCHeight = new GridLength(index == 2 ? big : small, GridUnitType.Star);

            _expandedIndex = index;
        }
    }
}