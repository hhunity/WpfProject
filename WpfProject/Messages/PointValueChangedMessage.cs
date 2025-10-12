using CommunityToolkit.Mvvm.Messaging.Messages;

namespace WpfProject.Messages;

// index の点を (x,y) へ更新せよ、という通知
public sealed class PointValueChangedMessage : ValueChangedMessage<(int index, double x, double y)>
{
    public PointValueChangedMessage((int index, double x, double y) value) : base(value) { }
}