using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfProject.Views
{
    public static class RowDefinitionBinder
    {
        public static readonly DependencyProperty HeightBindingProperty =
            DependencyProperty.RegisterAttached(
                "HeightBinding",
                typeof(GridLength),
                typeof(RowDefinitionBinder),
                new PropertyMetadata(new GridLength(1, GridUnitType.Star), OnHeightBindingChanged));

        public static void SetHeightBinding(DependencyObject element, GridLength value)
            => element.SetValue(HeightBindingProperty, value);

        public static GridLength GetHeightBinding(DependencyObject element)
            => (GridLength)element.GetValue(HeightBindingProperty);

        private static void OnHeightBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RowDefinition row && e.NewValue is GridLength gl)
                row.Height = gl;
        }
    }
}
