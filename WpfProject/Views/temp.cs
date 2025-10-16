
<Style x:Key="ToggleWithBehavior" TargetType="ToggleButton"
       BasedOn="{StaticResource ToggleVisual}">
    <Setter Property="Template">
        <Setter.Value>
            <ControlTemplate TargetType="ToggleButton">
                <Border x:Name="root" Background="{TemplateBinding Background}">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    <!-- ★ここに直接ビヘイビアを置く -->
                    <i:Interaction.Triggers>
                        <i:EventTrigger EventName="Checked">
                            <i:InvokeCommandAction Command="{Binding ToggleCommand}"
                                                   CommandParameter="{Binding Tag, RelativeSource={RelativeSource TemplatedParent}}"/>
                        </i:EventTrigger>
                        <i:EventTrigger EventName="Unchecked">
                            <i:InvokeCommandAction Command="{Binding ToggleCommand}"
                                                   CommandParameter="{Binding Tag, RelativeSource={RelativeSource TemplatedParent}}"/>
                        </i:EventTrigger>
                    </i:Interaction.Triggers>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="IsChecked" Value="True">
                        <Setter TargetName="root" Property="Background" Value="#2E80FF"/>
                        <Setter Property="Foreground" Value="White"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>


using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

public class MainViewModel
{
    public ICommand ToggleCommand => new RelayCommand<object?>(param =>
    {
        var id = param?.ToString();
        // ここでは ON/OFF はイベント側で区別される（Checked / Unchecked）
        // 必要なら View 側で IsChecked も渡す（下に例あり）
        System.Diagnostics.Debug.WriteLine($"Toggle: {id}");
    });
}



<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:i="http://schemas.microsoft.com/xaml/behaviors">

    <!-- 見た目だけ（基底） -->
    <Style x:Key="ToggleVisual" TargetType="ToggleButton">
        <Setter Property="Width" Value="110"/>
        <Setter Property="Height" Value="40"/>
        <Setter Property="Foreground" Value="White"/>
        <Setter Property="Background" Value="#2E80FF"/>
        <Setter Property="BorderBrush" Value="#1B5FCC"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="ToggleButton">
                    <Border x:Name="Bd"
                            CornerRadius="6"
                            Background="{TemplateBinding Background}"
                            BorderBrush="{TemplateBinding BorderBrush}"
                            BorderThickness="{TemplateBinding BorderThickness}">
                        <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                    </Border>
                    <ControlTemplate.Triggers>
                        <Trigger Property="IsChecked" Value="False">
                            <Setter TargetName="Bd" Property="Background" Value="#E0E5EB"/>
                            <Setter TargetName="Bd" Property="BorderBrush"  Value="#9AA4AE"/>
                            <Setter Property="Foreground" Value="#222"/>
                        </Trigger>
                        <Trigger Property="IsPressed" Value="True">
                            <Setter TargetName="Bd" Property="Opacity" Value="0.9"/>
                        </Trigger>
                        <Trigger Property="IsEnabled" Value="False">
                            <Setter TargetName="Bd" Property="Opacity" Value="0.5"/>
                        </Trigger>
                    </ControlTemplate.Triggers>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <!-- 見た目＋ビヘイビア（派生） -->
    <Style x:Key="ToggleWithBehavior" TargetType="ToggleButton"
           BasedOn="{StaticResource ToggleVisual}">
        <!-- ビヘイビア（Checked/Unchecked→Command） -->
        <Setter Property="i:Interaction.Triggers">
            <Setter.Value>
                <i:EventTrigger EventName="Checked">
                    <i:InvokeCommandAction Command="{Binding ToggleCommand}"
                                           CommandParameter="{Binding Tag, RelativeSource={RelativeSource Self}}"/>
                </i:EventTrigger>
                <i:EventTrigger EventName="Unchecked">
                    <i:InvokeCommandAction Command="{Binding ToggleCommand}"
                                           CommandParameter="{Binding Tag, RelativeSource={RelativeSource Self}}"/>
                </i:EventTrigger>
            </Setter.Value>
        </Setter>
    </Style>

</ResourceDictionary>

<!-- 見た目だけ適用（ビヘイビアなし） -->
<ToggleButton Style="{StaticResource ToggleVisual}" Content="LED-A" Tag="LED-A" />

<!-- 見た目＋ビヘイビアを適用（Checked/UncheckedでVMのコマンド呼ぶ） -->
<ToggleButton Style="{StaticResource ToggleWithBehavior}" Content="LED-B" Tag="LED-B" />
<ToggleButton Style="{StaticResource ToggleWithBehavior}" Content="LED-C" Tag="LED-C" />