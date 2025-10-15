<ItemsControl ItemsSource="{Binding Toggles}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate><WrapPanel/></ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>

    <ItemsControl.ItemTemplate>
        <DataTemplate DataType="{x:Type local:ToggleItem}">
            <ToggleButton Content="{Binding Id}"
                          IsChecked="{Binding IsChecked, Mode=TwoWay}"
                          Command="{Binding DataContext.ToggleCommand, RelativeSource={RelativeSource AncestorType=Window}}">
                <ToggleButton.CommandParameter>
                    <MultiBinding Converter="{StaticResource ToggleParamConverter}">
                        <Binding Path="IsChecked"/>
                        <Binding Path="Id"/>
                    </MultiBinding>
                </ToggleButton.CommandParameter>
            </ToggleButton>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>

// VM 側：表示したいトグルの一覧
public ObservableCollection<ToggleItem> Toggles { get; } = new()
{
    new("A"), new("B"), new("C")
};
public record ToggleItem(string Id) { public bool? IsChecked { get; set; } }

xmlns:i="http://schemas.microsoft.com/xaml/behaviors"

<Style TargetType="ToggleButton" x:Key="ToggleWithBehaviors">
    <Setter Property="Template"> ...（見た目はお好み）... </Setter>
    <Setter Property="Tag" Value="{x:Null}"/>
    <Setter Property="i:Interaction.Triggers">
        <Setter.Value>
            <i:EventTrigger EventName="Checked">
                <i:InvokeCommandAction Command="{Binding ToggleCommand}"
                                       CommandParameter="{Binding RelativeSource={RelativeSource Self}.AssociatedObject.Tag}"/>
            </i:EventTrigger>
            <i:EventTrigger EventName="Unchecked">
                <i:InvokeCommandAction Command="{Binding ToggleCommand}"
                                       CommandParameter="{Binding RelativeSource={RelativeSource Self}.AssociatedObject.Tag}"/>
            </i:EventTrigger>
        </Setter.Value>
    </Setter>
</Style>

<ToggleButton Style="{StaticResource ToggleWithBehaviors}" Content="A" Tag="A"/>
<ToggleButton Style="{StaticResource ToggleWithBehaviors}" Content="B" Tag="B"/>
