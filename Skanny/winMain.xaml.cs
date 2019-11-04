using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Skanny
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class winMain : Window
  {
    public winMain()
    {
      InitializeComponent();
    }
    public class Thumb : INotifyPropertyChanged
    {
      public event PropertyChangedEventHandler PropertyChanged;
      public void NotifyPropertyChanged(string propName)
      {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
      }
      public Thumb() { }
      private string _filespec;
      public string FileSpec { get { return _filespec; } set { if (value != _filespec) { _filespec = value; NotifyPropertyChanged("FileSpec"); } } }
      private int? _index;
      public int? Index { get { return _index; } set { if (value != _index) { _index = value; NotifyPropertyChanged("Index"); } } }
      private bool _toPdf;
      public bool ToPdf { get { return _toPdf; } set { if (value != _toPdf) { _toPdf = value; NotifyPropertyChanged("ToPdf"); } } }
      private double _width;
      public double Width { get { return _width; } set { if (value != _width) { _width = value; NotifyPropertyChanged("Width"); } } }
      private double _fontsize;
      public double FontSize { get { return _fontsize; } set { if (value != _fontsize) { _fontsize = value; NotifyPropertyChanged("FontSize"); } } }
      public ImageSource Image { get; set; }
    }
    public AvailableThumbSizes ThumbSize { get; set; }
    public ObservableCollection<Thumb> scans { get; set; } = new ObservableCollection<Thumb>();
    public ObservableCollection<Thumb> pics { get; set; } = new ObservableCollection<Thumb>();
    FileSystemWatcher watcher = new FileSystemWatcher();

    private void Window_Initialized(object sender, EventArgs e)
    {
      txtVersion.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
      ClearStatus();
      ApplySettings();

      scans.Add(new Thumb() { FileSpec = "C:\\test.png", Width = 150, Image = new BitmapImage(new Uri(@"pack://application:,,,/include/camera.png", UriKind.Absolute)) });
      scans.Add(new Thumb() { FileSpec = "C:\\123.bmp", Width = 150, Image = new BitmapImage(new Uri(@"pack://application:,,,/include/scanner.png", UriKind.Absolute)) });
      scans.Add(new Thumb() { FileSpec = "C:\\test.png", Width = 150, Image = new BitmapImage(new Uri(@"pack://application:,,,/include/camera.png", UriKind.Absolute)) });
      scans.Add(new Thumb() { FileSpec = "C:\\123.bmp", Width = 150, Image = new BitmapImage(new Uri(@"pack://application:,,,/include/scanner.png", UriKind.Absolute)) });
      scans.Add(new Thumb() { FileSpec = "C:\\test.png", Width = 150, Image = new BitmapImage(new Uri(@"pack://application:,,,/include/camera.png", UriKind.Absolute)) });
      scans.Add(new Thumb() { FileSpec = "C:\\123.bmp", Width = 150, Image = new BitmapImage(new Uri(@"pack://application:,,,/include/scanner.png", UriKind.Absolute)) });
      pics.Add(new Thumb() { FileSpec = "C:\\test.png", Width = 150, Image = new BitmapImage(new Uri(@"pack://application:,,,/include/camera.png", UriKind.Absolute)) });
      pics.Add(new Thumb() { FileSpec = "C:\\123.bmp", Width = 150, Image = new BitmapImage(new Uri(@"pack://application:,,,/include/scanner.png", UriKind.Absolute)) });
      ResizeThumbs();
      listScans.ItemsSource = scans;
      listPics.ItemsSource = pics;
      RadioButton_Checked(null, null);
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      watchPicDirectory();
    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      SaveSettings();
    }
    private void ApplySettings()
    {
      if (Properties.Settings.Default.UpgradeRequired)
      {
        Properties.Settings.Default.Upgrade();
        Properties.Settings.Default.UpgradeRequired = false;
        Properties.Settings.Default.Save();
      }
      this.WindowState = Properties.Settings.Default.LastWindowState;
      this.Width = Properties.Settings.Default.LastWindowRect.Width;
      this.Height = Properties.Settings.Default.LastWindowRect.Height;
      this.Top = Properties.Settings.Default.LastWindowRect.Top;
      this.Left = Properties.Settings.Default.LastWindowRect.Left;
      gridMain.ColumnDefinitions[0].Width = new GridLength(Properties.Settings.Default.SidebarWidth);
      cmbSize.SelectedItem = (AvailableThumbSizes)Properties.Settings.Default.LastThumbSize;

    }
    private void SaveSettings()
    {
      Properties.Settings.Default.LastWindowState = this.WindowState;
      Properties.Settings.Default.LastWindowRect = this.RestoreBounds;
      Properties.Settings.Default.SidebarWidth = gridMain.ColumnDefinitions[0].Width.Value;
      Properties.Settings.Default.LastThumbSize = (int)(AvailableThumbSizes)(cmbSize.SelectedItem ?? AvailableThumbSizes.Medium);
      Properties.Settings.Default.Save();
    }
    private void UpdateStatus(string message, System.Windows.Media.Brush background, System.Windows.Media.Brush foreground)
    {
      if (string.IsNullOrEmpty(message)) { return; }
      Dispatcher.Invoke(() =>
      {
        txtStatus.Text = message;
        txtStatus.Background = background ?? txtStatus.Background;
        txtStatus.Foreground = foreground ?? txtStatus.Foreground;
      });
    }
    private void ClearStatus()
    {
      Dispatcher.Invoke(() =>
      {
        txtStatus.Text = string.Empty;
        txtStatus.Background = new SolidColorBrush(Colors.Transparent);
        txtStatus.Foreground = new SolidColorBrush(Colors.White);
      });
    }
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
      this.WindowState = WindowState.Minimized;
    }
    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
      if (WindowState == WindowState.Maximized)
      {
        WindowState = WindowState.Normal;
      }
      else
      {
        WindowState = WindowState.Maximized;
      }
    }
    private void BtnApp_Click(object sender, RoutedEventArgs e)
    {
      if (gridMain.ColumnDefinitions[0].Width.Value > 0)
      {
        gridMain.ColumnDefinitions[0].Width = new GridLength(0);
      }
      else
      {
        gridMain.ColumnDefinitions[0].Width = new GridLength(100);
      }
    }
    private void _MouseDown(object sender, MouseButtonEventArgs e)
    {
      if (e.ChangedButton == MouseButton.Left) { this.DragMove(); }
      if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
      {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
      }
    }
    private void btnScan_Click(object sender, RoutedEventArgs e)
    {

    }
    private void BtnPdf_Click(object sender, RoutedEventArgs e)
    {
      foreach (var x in scans)
      {
        x.ToPdf = !x.ToPdf;
      }
      foreach (var x in pics)
      {
        x.ToPdf = !x.ToPdf;
      }
    }
    private void cmbSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
      ResizeThumbs();
    }
    private void ResizeThumbs()
    {
      if (cmbSize.SelectedItem != null)
      {
        foreach (var x in scans)
        {
          x.Width = (int)(AvailableThumbSizes)cmbSize.SelectedItem;
          x.FontSize = x.Width / 5;
        }
        foreach (var x in pics)
        {
          x.Width = (int)(AvailableThumbSizes)cmbSize.SelectedItem;
          x.FontSize = x.Width / 5;
        }
      }
    }
    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
      if (this.IsInitialized)
      {
        if (rdoScans.IsChecked == true)
        {
          SetViewScans();
        }
        else
        {
          SetViewPics();
        }
      }
    }
    private void SetViewScans()
    {
      listScans.Visibility = Visibility.Visible;
      listPics.Visibility = Visibility.Hidden;
    }
    private void SetViewPics()
    {
      listScans.Visibility = Visibility.Hidden;
      listPics.Visibility = Visibility.Visible;
    }
    private void Overlay_MouseUp(object sender, MouseButtonEventArgs e)
    {
      TogglePdfIndex((Thumb)((Grid)sender).DataContext);
    }
    private void Overlay_KeyUp(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Space || e.Key == Key.Enter)
      {
        TogglePdfIndex((Thumb)((Grid)sender).DataContext);
      }
    }
    private void TogglePdfIndex(Thumb t)
    {
      if (t.Index == null)
      {
        t.Index = Math.Max(scans.Max(p => p.Index) ?? 0, pics.Max(p => p.Index) ?? 0) + 1;
      }
      else
      {
        t.Index = null;
        ReorderPdfIndexes();
      }
    }
    private void ReorderPdfIndexes()
    {
      int i = 1;
      foreach (var t in scans.Concat(pics).Where(p => p.Index > 0).OrderBy(p => p.Index))
      {
        t.Index = i; i++;
      }
    }
    private void watchPicDirectory()
    {
      if (!string.IsNullOrWhiteSpace(Skanny.Properties.Settings.Default.PicDirectory))
      {
        watcher.Path = Properties.Settings.Default.PicDirectory;

        watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        watcher.Filter = "*.*";
        //remove event handler before adding any new      
        watcher.Created -= new FileSystemEventHandler(OnChanged);
        //add new event handler      
        watcher.Created += new FileSystemEventHandler(OnChanged);

        //using renamed event instead of created. this is because of windows 10 camera app
        //watcher.Renamed += new RenamedEventHandler(OnRename);
        watcher.EnableRaisingEvents = true;
      }
    }
    public void OnChanged(object source, FileSystemEventArgs e) // FileSystemWatcher may come from some other thread which is internally created by the framework.
    {
      Dispatcher.Invoke(() => { UpdatePics(); });
    }
    private void UpdatePics()
    {
      //watcher.EnableRaisingEvents = false; // temporarily disable file system watcher
      LoadImagesForPics();
      //watcher.EnableRaisingEvents = true; // re enable file system watcher
      if (rdoPics.IsChecked != true)
      {
        rdoPics.IsChecked = true;
      }
      else
      {
        SetViewPics();
      }
    }
    private void LoadImagesForScans()
    {
    }
    private void LoadImagesForPics()
    {

    }
  }
}
