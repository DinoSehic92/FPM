using Avalon.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;

namespace Avalon.Views;

public partial class PreView : UserControl
{
    public PreView()
    {
        InitializeComponent();

        ScrollSlider.AddHandler(Slider.ValueChangedEvent, PageNrSlider);
        PreviewGrid.AddHandler(Grid.SizeChangedEvent, PreviewSizeChanged);

        MuPDFRenderer.AddHandler(LoadedEvent, InitSetup);
    }

    public MainViewModel ctx = null;
    public PreviewViewModel pwr = null;
    private bool ZoomMode = false;

    public void InitSetup(object sender, RoutedEventArgs e)
    {

        ctx = (MainViewModel)this.DataContext;
        pwr = ctx.PreviewVM;

        SetRenderer();
    }

    public void SetRenderer()
    {
        pwr.GetRenderControl(MuPDFRenderer);
    }

    private void ToggleSearchMode(object sender, RoutedEventArgs e)
    {

        if (pwr.SearchMode)
        {
            MainPreviewGrid.ColumnDefinitions[0] = new ColumnDefinition(1f, GridUnitType.Star);
            MainPreviewGrid.ColumnDefinitions[1] = new ColumnDefinition(200f, GridUnitType.Pixel);
            SearchRegex.Focus();
        }
        else
        {
            SearchRegex.Text = "";

            MainPreviewGrid.ColumnDefinitions[0] = new ColumnDefinition(1f, GridUnitType.Star);
            MainPreviewGrid.ColumnDefinitions[1] = new ColumnDefinition(0f, GridUnitType.Pixel);
        }
    }

    private void OnSeachRegex(object sender, RoutedEventArgs e)
    {
        string text = SearchRegex.Text;
        pwr.Search(text);
    }

    private void OnClearSearch(object sender, RoutedEventArgs e)
    {
        if (pwr.SearchBusy)
        {
            pwr.StopSearch();
        }
        else
        {
            pwr.ClearSearch();
            SearchRegex.Clear();
        }
    }

    private void OnStartSearhRegex(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnSeachRegex(null, null);
        }
    }

    private void PageNrSlider(object sender, RoutedEventArgs e)
    {
        if (ScrollSlider.IsFocused)
        {
            if ((int)ScrollSlider.Value - 1 != pwr.RequestPage1)
            {
                pwr.RequestPage1 = (int)ScrollSlider.Value - 1;
            }
        }
    }


    private void PreviewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (true)//previewMode)
        {
            ResetView(null, null);
        }
    }

    private void ResetView(object sender, RoutedEventArgs e)
    {
        MuPDFRenderer.Contain();
    }

    private void ModifiedControlPointerWheelChanged(object sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            ZoomMode = true;
        }
        else
        {
            ZoomMode = false;
        }

        MuPDFRenderer.ZoomEnabled = ZoomMode;

        if (!ZoomMode && pwr.Pagecount > 0)
        {
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                Avalonia.Vector mode = e.Delta;

                if (mode.Y > 0)
                {
                    pwr.PrevPage();
                }

                if (mode.Y < 0)
                {
                    pwr.NextPage();
                }
            }
        }
    }
}