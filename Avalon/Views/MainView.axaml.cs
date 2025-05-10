﻿using Avalonia.Controls;
using System;
using Avalonia.Interactivity;
using System.Linq;
using Avalon.ViewModels;
using System.ComponentModel;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Input;
using System.Threading;
using System.Collections.Generic;
using Avalon.Model;
using System.IO;
using Avalonia.Data;
using Avalonia.Styling;
using Org.BouncyCastle.Asn1.BC;
using System.Diagnostics;
using iText.Kernel.Pdf.Xobject;
using MuPDFCore.MuPDFRenderer;


namespace Avalon.Views;

public partial class MainView : UserControl, INotifyPropertyChanged
{
    public MainView() 
    {
        InitializeComponent();

        FileGrid.AddHandler(DataGrid.LoadedEvent, InitStartup);

        FileGrid.AddHandler(DataGrid.DoubleTappedEvent, OnOpenFile);
        CollectionContent.AddHandler(DataGrid.DoubleTappedEvent, OnOpenFile);

        FetchMetadata.AddHandler(Button.ClickEvent, OnFetchFullMeta);

        FileGrid.AddHandler(DataGrid.SelectionChangedEvent, SetPreviewRequestMain);
        FileGrid.AddHandler(DataGrid.SelectionChangedEvent, SelectFiles);
        FileGrid.AddHandler(DragDrop.DropEvent, OnDrop);

        AppendixGrid.AddHandler(DragDrop.DropEvent, OnDropAppendedFiles);
        //AppendixGrid.AddHandler(DataGrid.SelectionChangedEvent, SelectAppendedFiles);
        AppendixGrid.AddHandler(DataGrid.SelectionChangedEvent, SetPreviewRequestAppendedFiles);

        CollectionContent.AddHandler(DataGrid.SelectionChangedEvent, SelectFavorite);
        CollectionContent.AddHandler(DataGrid.SelectionChangedEvent, SetPreviewRequestCollection);

        BookmarkGrid.AddHandler(DataGrid.SelectionChangedEvent, BookmarkSelected);

        InitMetaworker();
    }

    public bool darkmode = true;

    private BackgroundWorker MetaWorker = new BackgroundWorker();

    private Thread taskThread = null;

    private CancellationTokenSource cts = new CancellationTokenSource();

    public MainViewModel ctx = null;
    public PreviewViewModel pwr = null;
    public GeneralData gnr = null;

    public List<DataGridRowEventArgs> Args = new List<DataGridRowEventArgs>();

    public ColumnDefinition a = new ColumnDefinition();
    public ColumnDefinition b = new ColumnDefinition();
    public ColumnDefinition c = new ColumnDefinition();
    public ColumnDefinition d = new ColumnDefinition();

    private bool PreviewTaskBusy = false;
    private bool PreviewReady = false;
    private double BitmapRes = 0.5;

    private void InitStartup(object sender, RoutedEventArgs e)
    {
        GetDatacontext();
        UpdateFont();
        ctx.PreviewEmbeddedOpen = false;

        try
        {
            ctx.LoadFileAuto();
            UpdateFont();

        }
        catch
        { }
    }

    public void GetDatacontext()
    {
        ctx = (MainViewModel)this.DataContext;
        pwr = ctx.PreviewVM;
        
        
        ctx.PropertyChanged += OnBindingCtx;
    }


    public void OnBindingCtx(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "FilteredFiles") { OnUpdateColumns(); }
        if (e.PropertyName == "UpdateColumns") { OnUpdateColumns(); }
        if (e.PropertyName == "Color1") { UpdateRowColor(); }
        if (e.PropertyName == "Color3") { UpdateRowColor(); }
        if (e.PropertyName == "TreeViewUpdate") { SetupTreeview(null, null); }

        if (e.PropertyName == "PreviewEmbeddedOpen") { UpdateMainGrid(); }
        if (e.PropertyName == "PreviewWindowOpen") { OnTogglePreviewWindow(); }
        if (e.PropertyName == "FontChanged") { UpdateFont(); }

    }

    private void UpdateFont()
    {
        var window = Window.GetTopLevel(this);

        window.FontFamily = new FontFamily(ctx.Storage.General.Font);
        window.FontSize = ctx.Storage.General.FontSize;

        //FileGrid.FontFamily = new FontFamily(ctx.Storage.General.Font);
        //FileGrid.FontSize = ctx.Storage.General.FontSize;
    }

    public void OnTogglePreviewWindow()
    {
        
        if (ctx.PreviewWindowOpen)
        {
            var window = Window.GetTopLevel(this);
            ThemeVariant theme = window.RequestedThemeVariant;
            ctx.OpenPreviewWindow(theme);
        }

        else
        {
            if (ctx.PreviewWindow != null)
            {
                ctx.PreviewWindow.Close();
            }
        }
    }

    public void UpdateMainGrid()
    {
        if (ctx.PreviewEmbeddedOpen)
        {
            MainGrid.ColumnDefinitions[2] = new ColumnDefinition(5, GridUnitType.Pixel);
            MainGrid.ColumnDefinitions[3] = new ColumnDefinition(2.5, GridUnitType.Star);

            EmbeddedPreview.SetRenderer();
        }
        else
        {
            MainGrid.ColumnDefinitions[2] = new ColumnDefinition(0, GridUnitType.Star);
            MainGrid.ColumnDefinitions[3] = new ColumnDefinition(0, GridUnitType.Star);
        }

        MainGrid.ColumnDefinitions[1] = new ColumnDefinition(1, GridUnitType.Star);
    }

    public void OnSearch(object sender, RoutedEventArgs e)
    {
        string searchtext = SearchText.Text;

        if (searchtext != null)
        {
            ctx.Search(searchtext);
        }

        OnUpdateColumns();
    }

    public void OnStartSearch(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter)
        {
            OnSearch(null, null);
        }
    }

    public void OnDrop(object sender, DragEventArgs e)
    {
        var items = e.Data.GetFiles();

        foreach(var item in items)
        {
            if (item.Path.IsFile)
            {
                string path = item.Path.LocalPath;
                string type = Path.GetExtension(path);

                if (type == ".pdf")
                {
                    ctx.AddFilesDrag(path);
                }
            }
        }
        SetupTreeview(null, null);
    }

    public void OnDropAppendedFiles(object sender, DragEventArgs e)
    {
        var items = e.Data.GetFiles();

        foreach(var item in items)
        {
            if (item.Path.IsFile)
            {
                string path = item.Path.LocalPath;
                string type = Path.GetExtension(path);

                if (type == ".pdf")
                {
                    ctx.AddAppendedFile(path);
                }
            }
        }
    }

    private void SetupTreeview(object sender, RoutedEventArgs e)
    {
        MainTree.Items.Clear();
        ctx.GetGroups();     

        List<string> typeList = new List<string>() { "Archive", "Library", "Project" };


        foreach (string type in typeList)
        {
            List<TreeViewItem> items = new List<TreeViewItem>();

            IEnumerable<ProjectData> projects = ctx.Storage.StoredProjects.Where(x => x.Category == type);

            if (projects.Count() != 0)
            {
                foreach (ProjectData project in projects)
                {

                    if (project.Parent == null || project.Parent == "")
                    {
                        List<TreeViewItem> fileTypeTree = new List<TreeViewItem>();
                        foreach (string filetype in project.StoredFiles.Select(x => x.Filtyp).Distinct())
                        {
                            int nfiles = project.StoredFiles.Where(x => x.Filtyp == filetype).Count();
                            fileTypeTree.Add(new TreeViewItem() 
                            {
                                FontSize = 13,
                                FontWeight = FontWeight.Light,
                                Header = filetype + "  (" + nfiles + ")", 
                                Tag = project.Namn 
                            });
                        }
                        items.Add(
                            new TreeViewItem()
                            {
                                FontSize = 15,
                                FontWeight = FontWeight.Normal,
                                FontStyle = FontStyle.Normal,
                                Header = project.Namn,
                                IsExpanded = (project == ctx.CurrentProject),
                                Tag = "All Types",
                                ItemsSource = fileTypeTree
                            }
                        );
                    }
                }

                if (type == "Project")
                {
                    foreach (string group in ctx.Groups)
                    {
                        IEnumerable<ProjectData> groupedProject = ctx.Storage.StoredProjects.Where(x => x.Parent == group);
                        List<TreeViewItem> groupedTree = new List<TreeViewItem>();

                        foreach (ProjectData project in groupedProject)
                        {
                            List<TreeViewItem> fileTypeTree = new List<TreeViewItem>();
                            foreach (string filetype in project.StoredFiles.Select(x => x.Filtyp).Distinct())
                            {
                                int nfiles = project.StoredFiles.Where(x => x.Filtyp == filetype).Count();
                                fileTypeTree.Add(new TreeViewItem() 
                                {
                                    FontSize = 13,
                                    FontWeight = FontWeight.Light,
                                    Header = filetype + "  (" + nfiles + ")", 
                                    Tag = project.Namn 
                                });
                            }

                            groupedTree.Add(new TreeViewItem()
                            {
                                FontSize = 15,
                                FontWeight = FontWeight.Normal,
                                FontStyle = FontStyle.Normal,
                                Header = project.Namn,
                                IsExpanded = (project == ctx.CurrentProject),
                                Tag = "All Types",
                                ItemsSource = fileTypeTree
                            });
                        }

                        items.Add(
                            new TreeViewItem()
                            {
                                FontSize = 15,
                                FontWeight = FontWeight.Bold,
                                FontStyle = FontStyle.Normal,
                                Header = group,
                                IsExpanded = true,
                                Tag = "Group",
                                ItemsSource = groupedTree
                            }
                        );

                    }
                }

                MainTree.Items.Add(
                    new TreeViewItem()
                    {
                        FontSize = 16,
                        FontWeight = FontWeight.Bold,
                        FontStyle = FontStyle.Italic,
                        Header = type,
                        Tag = "Header",
                        IsExpanded = true,
                        ItemsSource = items
                    }
                );
            }

        }
    }

    public void OnTreeviewSelected(object sender, SelectionChangedEventArgs e)
    {
        object selected = MainTree.SelectedItem;


        if (selected != null)
        {
    
            TreeViewItem selectedTree = (TreeViewItem)selected;

            if(selectedTree.Tag == "Header" || selectedTree.Tag == "Group")
            {
                MainTree.SelectedItem = null;
                MainTree.ContextMenu.IsEnabled = false;
                return;
            }

            else
            {

                TreeViewItem parentTree = (TreeViewItem)selectedTree.Parent;
                

                MainTree.ContextMenu.IsEnabled = true;

                if (selectedTree.Tag == "All Types")
                {
                    ctx.SelectType("All Types");
                    ctx.SelectProject(selectedTree.Header.ToString());
                }
                else
                {
                    ctx.SelectType(selectedTree.Header.ToString().Split("  ")[0]);
                    ctx.SelectProject(selectedTree.Tag.ToString());
                }
            }

            OnUpdateColumns();
        }
    }

    public void ToggleTheme(object sender, RoutedEventArgs e)
    {
        darkmode = !darkmode;

        ModeDayIcon.IsVisible = !ModeDayIcon.IsVisible;
        ModeNightIcon.IsVisible = !ModeNightIcon.IsVisible;

        var window = Window.GetTopLevel(this);

        if (darkmode)
        {
            window.RequestedThemeVariant = ThemeVariant.Dark;
        }
        else
        {
            window.RequestedThemeVariant = ThemeVariant.Light;
        }

    }


    private void Border_PointerPressed(object sender, RoutedEventArgs args)
    {
        var ctl = sender as Control;
        if (ctl != null)
        {
            FlyoutBase.ShowAttachedFlyout(ctl);
        }
    }

    private void SetPreviewRequestMain(object sender, RoutedEventArgs r)
    {
        FileData file = (FileData)FileGrid.SelectedItem;
        SetPreviewRequest(file);

    }

    private void SetPreviewRequestCollection(object sender, RoutedEventArgs r)
    {
        FileData file = (FileData)CollectionContent.SelectedItem;
        SetPreviewRequest(file);
    }

    private void SetPreviewRequestAppendedFiles(object sender, RoutedEventArgs r)
    {
        FileData file = (FileData)AppendixGrid.SelectedItem;
        SetPreviewRequest(file);
    }

    private void SetPreviewRequest(FileData file)
    {
        if (ctx.PreviewEmbeddedOpen || ctx.PreviewWindowOpen)
        {
            CheckStatusSingleFile();

            if (file != null && Path.Exists(file.Sökväg)) 
            {
                int startPage = file.DefaultPage;

                pwr.SetupPage(startPage);
                pwr.RequestFile = file;

                pwr.SetFile();
            }

            else
            {
                pwr.ClearRenderer();
            }
        }
    }

    private void EditColor(object sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;
        string color = menuItem.Tag.ToString();

        if (color != "None")
        {
            ctx.AddColor(color);
        }

        DeselectItems();
        UpdateRowColor();
    }

    private void EditType(object sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;

        MenuItem SelectedMenu = ctx.FileTypeSelection[menuItem.SelectedIndex];

        ctx.EditType(SelectedMenu.Header.ToString());
        SetupTreeview(null, null);
    }

    private void DeselectItems()
    {
        FileGrid.SelectedItem = null;
    }


    private void OnClearFiles(object sender, RoutedEventArgs e)
    {
        ctx.ClearAll();

       DeselectItems();
       UpdateRowColor();
    }


    private void OnCheckStatusSingleFile(object sender, RoutedEventArgs e)
    {
        CheckStatusSingleFile();
    }


    private void CheckStatusSingleFile()
    {
        ctx.CheckSingleFile();
        UpdateRowColor();
    }


    private async void OnCheckProjectFiles(object sender, RoutedEventArgs e)
    {
        await ctx.CheckProjectFiles();
        DeselectItems();
        UpdateRowColor();
    }


    async void OnRemoveProject(object sender, RoutedEventArgs e)
    {
        if (ctx.Storage.StoredProjects.Count() > 1)
        {
            Window window = (MainWindow)Window.GetTopLevel(this);
            await ctx.ConfirmDeleteDia(window);

            if (ctx.Confirmed)
            {
                ctx.RemoveProject();
                SetupTreeview(null, null);
            }
        }
    }


    private void OnAddFiles(object sender, RoutedEventArgs e)
    {
        ctx.AddFile(this);
        SetupTreeview(null, null);
    }


    private void OnFetchSingleMeta(object sender, RoutedEventArgs e)
    {
        ProgressStatus.Content = "Fetching Metadata";

        ctx.SelectFilesForMetaworker(true);
        MetaWorker.RunWorkerAsync();
    }


    private void OnFetchFullMeta(object sender, RoutedEventArgs e)
    {
        ProgressStatus.Content = "Fetching Metadata";

        ctx.SelectFilesForMetaworker(false);
        MetaWorker.RunWorkerAsync();
    }


    private void InitMetaworker()
    {
        MetaWorker.DoWork += MetaWorkerDoWork;
        MetaWorker.WorkerReportsProgress = true;
        MetaWorker.ProgressChanged += MetaWorkerProgress;
        MetaWorker.RunWorkerCompleted += MetaWorkerRunWorkerCompleted;
    }


    private void MetaWorkerDoWork(object sender, DoWorkEventArgs e)
    {
        int nPaths = ctx.GetNrSelectedFiles();

        for (int k = 0; k < nPaths; k++)
        {
            ctx.GetMetadata(k);

            int percentage = (k + 1) * 100 / nPaths;
            MetaWorker.ReportProgress(percentage);
        }
    }


    private void MetaWorkerProgress(object sender, ProgressChangedEventArgs e)
    {
        ProgressBar.Value = e.ProgressPercentage;
    }


    private void MetaWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        ctx.SetMeta();
        ProgressStatus.Content = "";
        ProgressBar.Value = 0;
    }


    private void BookmarkSelected(object sender, RoutedEventArgs e)
    {
        if (ctx.PreviewEmbeddedOpen || ctx.PreviewWindowOpen)
        {
            PageData page = (PageData)BookmarkGrid.SelectedItem;
            ctx.SetBookmark(page);
        }
    }

    private void OnAddBookmark(object sender, RoutedEventArgs e)
    {
        if (ctx.PreviewEmbeddedOpen || ctx.PreviewWindowOpen)
        {
            ctx.AddBookmark(BookmarkInput.Text);
            BookmarkInput.Clear();
        }
    }


    private void OnRenameBookmark(object sender, RoutedEventArgs e)
    {
        ctx.RenameBookmark(BookmarkInput.Text);
        BookmarkInput.Clear();
    }


    private void OnRemoveBookmark(object sender, RoutedEventArgs e)
    {
        PageData page = (PageData)BookmarkGrid.SelectedItem;
        ctx.RemoveBookmark(page);
    }

    private void SelectFavorite(object sender, RoutedEventArgs e)
    {
        IList<FileData> files = CollectionContent.SelectedItems.Cast<FileData>().ToList();
        
        DeselectItems();
        ctx.select_files(files);
    }

    private void OnNewCollection(object sender, RoutedEventArgs e)
    {
        string text = CollectionInput.Text;

        if(text != null && text.ToString().Length > 0)
        {
            ctx.NewCollection(CollectionInput.Text);
            CollectionInput.Clear();
        }
    }

    private void OnRenameCollection(object sender, RoutedEventArgs e)
    {
        string text = CollectionInput.Text;

        if (text != null && text.ToString().Length > 0)
        {
            ctx.RenameCollection(CollectionInput.Text);
            CollectionInput.Clear();
        }
    }

    private void OnAddToCollection(object sender, RoutedEventArgs e)
    {
        MenuItem source = e.Source as MenuItem;
        Debug.WriteLine(source.Header.ToString());
        if (source.Header.ToString()!= "Collection")
        {
            ctx.AddFileToCollection(source.Header.ToString());
        }
    }



    async void OnRemoveAttachedFile(object sender, RoutedEventArgs e)
    {
        Window window = (MainWindow)Window.GetTopLevel(this);
        await ctx.ConfirmDeleteDia(window);

        if (ctx.Confirmed)
        {
            IList<FileData> files = AppendixGrid.SelectedItems.Cast<FileData>().ToList();

            ctx.RemoveAttachedFile(files);
        }
    }

    private void OnOpenFile(object sender, RoutedEventArgs e)
    {
        ctx.OpenFile();
    }

    private void SelectFiles(object sender, RoutedEventArgs e)
    {
        IList<FileData> files = FileGrid.SelectedItems.Cast<FileData>().ToList();
        CollectionContent.SelectedItem = null;

        ctx.select_files(files);
    }

    private void SelectAppendedFiles(object sender, RoutedEventArgs e)
    {
        IList<FileData> files = AppendixGrid.SelectedItems.Cast<FileData>().ToList();

        ctx.select_files(files);
    }

    private async void OnLoadFile(object sender, RoutedEventArgs e)
    {
        await ctx.LoadFile(this);
        SetupTreeview(null, null);
        UpdateFont();
    }

    private async void OnSaveFile(object sender, RoutedEventArgs e)
    {
        await ctx.SaveFile(this);
    }

    private async void OnSaveFileAuto(object sender, RoutedEventArgs e)
    {
        await ctx.SaveFileAuto();
    }

    async void OnRemoveFiles(object sender, RoutedEventArgs e)
    {
        Window window = (MainWindow)Window.GetTopLevel(this);
        await ctx.ConfirmDeleteDia(window);

        if (ctx.Confirmed)
        {
            ctx.RemoveSelectedFiles();
            ctx.UpdateFilter();
            SetupTreeview(null, null);
        }
    }

    private void OnUpdateColumns()
    {
        FileGrid.Columns[0].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        FileGrid.Columns[1].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        FileGrid.Columns[2].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        FileGrid.Columns[3].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        FileGrid.Columns[4].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        FileGrid.Columns[5].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        FileGrid.Columns[6].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        FileGrid.Columns[7].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        FileGrid.Columns[8].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
        FileGrid.Columns[9].Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);

        FileGrid.UpdateLayout();

    }

    private void DataGrid_OnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        Args.Add(e);

        var dataObject = e.Row.DataContext as FileData;
        e.Row.Classes.Clear();

        if (dataObject != null && dataObject.Färg == "") { e.Row.Classes.Clear(); }

        if (dataObject != null && dataObject.FileStatus == "Missing") { e.Row.Classes.Add("RedForeground"); }

        if (dataObject != null && dataObject.Färg == "Yellow") { e.Row.Classes.Add("Yellow"); }
        if (dataObject != null && dataObject.Färg == "Orange") { e.Row.Classes.Add("Orange"); }
        if (dataObject != null && dataObject.Färg == "Brown") { e.Row.Classes.Add("Brown"); }
        if (dataObject != null && dataObject.Färg == "Green") { e.Row.Classes.Add("Green"); }
        if (dataObject != null && dataObject.Färg == "Blue") { e.Row.Classes.Add("Blue"); }
        if (dataObject != null && dataObject.Färg == "Red") { e.Row.Classes.Add("Red"); }
        if (dataObject != null && dataObject.Färg == "Magenta") { e.Row.Classes.Add("Magenta"); }
    }

    private void UpdateRowColor()
    {

        foreach (DataGridRowEventArgs e in Args)
        {
            var dataObject = e.Row.DataContext as FileData;

            e.Row.Classes.Clear();

            if (dataObject != null && dataObject.FileStatus == "Missing") { e.Row.Classes.Add("RedForeground"); }

            if (dataObject != null && dataObject.Färg == "Yellow") { e.Row.Classes.Add("Yellow"); }
            if (dataObject != null && dataObject.Färg == "Orange") { e.Row.Classes.Add("Orange"); }
            if (dataObject != null && dataObject.Färg == "Brown") { e.Row.Classes.Add("Brown"); }
            if (dataObject != null && dataObject.Färg == "Green") { e.Row.Classes.Add("Green"); }
            if (dataObject != null && dataObject.Färg == "Blue") { e.Row.Classes.Add("Blue"); }
            if (dataObject != null && dataObject.Färg == "Red") { e.Row.Classes.Add("Red"); }
            if (dataObject != null && dataObject.Färg == "Magenta") { e.Row.Classes.Add("Magenta"); }
        }
    }

    private void RaisePropertyChanged(string propName)
    {
        if (PropertyChanged != null)
            PropertyChanged(this, new PropertyChangedEventArgs(propName));
    }
    public event PropertyChangedEventHandler PropertyChanged;

    private void Binding(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}

