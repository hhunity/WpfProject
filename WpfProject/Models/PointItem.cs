using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfProject.Models;

public partial class PointItem : ObservableObject
{
    public int Index { get; }

    [ObservableProperty] private double x;
    [ObservableProperty] private double y;

    public PointItem(int index, double x, double y)
    {
        Index = index;
        this.x = x;
        this.y = y;
    }
}