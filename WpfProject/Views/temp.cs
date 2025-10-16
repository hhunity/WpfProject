
<ItemsControl ItemsSource="{Binding Rows}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Horizontal" Margin="4">
                <!-- 左のラベル -->
                <TextBlock Text="{Binding Label}"
                           Width="80"
                           FontWeight="Bold"
                           VerticalAlignment="Center"/>

                <!-- 右のトグル群 -->
                <ItemsControl ItemsSource="{Binding Toggles}">
                    <ItemsControl.ItemsPanel>
                        <ItemsPanelTemplate>
                            <UniformGrid Columns="8"/>
                        </ItemsPanelTemplate>
                    </ItemsControl.ItemsPanel>

                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Border BorderBrush="#999" BorderThickness="0.5">
                                <ToggleButton Content="{Binding Label}"
                                              IsChecked="{Binding IsOn, Mode=TwoWay}"
                                              Width="70" Height="40" Margin="2"/>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

public class ToggleItem
{
    public string Label { get; set; }
    public bool IsOn { get; set; }
}

public class ToggleRow
{
    public string Label { get; set; }           // "Row 1" など
    public ObservableCollection<ToggleItem> Toggles { get; } = new();
}

public class MainViewModel
{
    public ObservableCollection<ToggleRow> Rows { get; } = new();

    public MainViewModel()
    {
        for (int r = 1; r <= 3; r++)
        {
            var row = new ToggleRow { Label = $"Row {r}" };
            for (int c = 1; c <= 8; c++)
                row.Toggles.Add(new ToggleItem { Label = $"{r}-{c}" });
            Rows.Add(row);
        }
    }
}




<Grid Margin="10">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <!-- 3行 × トグル群に合わせる -->
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <!-- 左ラベル列 -->
    <TextBlock Grid.Row="0" Grid.Column="0" Text="Row 1" Margin="10"/>
    <TextBlock Grid.Row="1" Grid.Column="0" Text="Row 2" Margin="10"/>
    <TextBlock Grid.Row="2" Grid.Column="0" Text="Row 3" Margin="10"/>

    <!-- 右 UniformGrid（RowSpan=3で全体を覆う） -->
    <UniformGrid Grid.Column="1"
                 Grid.RowSpan="3"
                 Columns="8" Rows="3"
                 HorizontalAlignment="Left"
                 VerticalAlignment="Top">
        <!-- ここにトグル24個 -->
        <Border BorderBrush="#888" BorderThickness="0.5" Margin="-0.25">
            <ToggleButton Content="1-1" Width="70" Height="40"/>
        </Border>
        <!-- ... -->
    </UniformGrid>
</Grid>

<Grid>
    <!-- 左端ラベル -->
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>  <!-- ラベル列 -->
        <ColumnDefinition Width="*"/>     <!-- トグルボタン群 -->
    </Grid.ColumnDefinitions>

    <TextBlock Text="LED群"
               FontSize="16"
               FontWeight="Bold"
               VerticalAlignment="Center"
               Margin="10"
               Grid.Column="0"/>

    <ItemsControl Grid.Column="1"
                  ItemsSource="{Binding Toggles}">
        <ItemsControl.ItemsPanel>
            <ItemsPanelTemplate>
                <!-- 横8列×縦3行 -->
                <UniformGrid Columns="8" Rows="3"/>
            </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>

        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <ToggleButton Content="{Binding Label}"
                              IsChecked="{Binding IsOn, Mode=TwoWay}"
                              Width="70" Height="40"
                              Margin="4"
                              Command="{Binding DataContext.ToggleCommand,
                                                RelativeSource={RelativeSource AncestorType=ItemsControl}}"/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Grid>



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