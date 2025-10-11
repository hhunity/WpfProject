using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WpfProject.Views;

namespace WpfProject.ViewModels
{
    partial class MainWindowsModel : ObservableObject
    {
        [RelayCommand]
        private void ScotPlot()
        {
            var win = new Window
            {
                Title = "scot plot",
                Content = new ScotPlotView(),
                Width = 800,
                Height = 600
            };
            win.ShowDialog();
        }
    }
}
