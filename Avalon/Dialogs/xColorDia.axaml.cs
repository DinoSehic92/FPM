using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Avalon.Dialog;

public partial class xColorDia : Window
{
    public xColorDia()
    {
        InitializeComponent();
    }

    public void ResetThemeColors(object sender, RoutedEventArgs e)
    {
        BackgroundColorPickerDark.Color = Color.Parse("#333333");
        AccentColorPickerDark.Color = Color.Parse("#444444");

        BackgroundColorPickerLight.Color = Color.Parse("#dfe6e9");
        AccentColorPickerLight.Color = Color.Parse("#999999");

        this.Close();
    }

    public void OnClose(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}