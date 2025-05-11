using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Avalon.Dialog;

public partial class xColorDia : Window, INotifyPropertyChanged
{
    public xColorDia()
    {
        InitializeComponent();

        //FontCombo.ItemsSource = FontManager.Current.SystemFonts.Select(x => x.Name).ToList();

        FontCombo.ItemsSource = new List<string>() {"Fira Sans", "IBM Plex Sans", "Jost", "Lato", "Lexend Deca", "Nunito", "Open Sans", "Quicksand", "Recursive", "Roboto", "Rosario", "Share Tech", "Source Code Pro", "Ubuntu", "Urbanist", "Work Sans"};

        FontSizeCombo.ItemsSource = new List<int>() { 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };

        KeyDown += CloseKey;
    }

    public void OnClose(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    public void ResetDark(object sender, RoutedEventArgs e)
    {
        BackgroundColorPickerDark.Color = Color.Parse("#333333");
        AccentColorPickerDark.Color = Color.Parse("#444444");
    }

    public void ResetLight(object sender, RoutedEventArgs e)
    {
        BackgroundColorPickerLight.Color = Color.Parse("#dfe6e9");
        AccentColorPickerLight.Color = Color.Parse("#999999");
    }

    public void ResetFonts(object sender, RoutedEventArgs e)
    {
        FontCombo.SelectedItem = "Roboto";
        FontSizeCombo.SelectedValue = 15;
    }

    private void CloseKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.Close();
        }
    }
}