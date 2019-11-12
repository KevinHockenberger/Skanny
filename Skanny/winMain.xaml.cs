using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using settings = Skanny.Properties.Settings;

namespace Skanny
{

  [TypeConverter(typeof(EnumDescriptionTypeConverter))]
  public enum AvailableThumbSizes
  {
    [Description("X-Small")]
    Xsmall = 100,
    [Description("Small")]
    Small = 250,
    [Description("Medium")]
    Medium = 500,
    [Description("Large")]
    Large = 800,
    [Description("X-Large")]
    Xlarge = 1000,
  }
  public enum ColorFormats
  {
    [Description("Black and White")]
    BlackandWhiteOnly = 1,
    [Description("Gray Scale")]
    Grayscale = 2,
    [Description("Color")]
    Color = 3,
  }

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class winMain : Window
  {
    public winMain()
    {
      try
      {
        InitializeComponent();
      }
      catch (Exception ex)
      {
        System.Windows.Forms.MessageBox.Show(ex.Message, "Fatal Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        throw;
      }
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
      public DateTime Created { get; set; }
      private int? _index;
      public int? Index { get { return _index; } set { if (value != _index) { _index = value; NotifyPropertyChanged("Index"); } } }
      public long Length { get; set; }
      private bool _toPdf;
      public bool ToPdf { get { return _toPdf; } set { if (value != _toPdf) { _toPdf = value; NotifyPropertyChanged("ToPdf"); } } }
      private double _width;
      public double Width { get { return _width; } set { if (value != _width) { _width = value; NotifyPropertyChanged("Width"); } } }
      private double _fontsize;
      public double FontSize { get { return _fontsize; } set { if (value != _fontsize) { _fontsize = value; NotifyPropertyChanged("FontSize"); } } }
      public ContextMenu ContextMenu;
      public ImageSource Image { get; set; }
    }
    public AvailableThumbSizes ThumbSize { get; set; }
    public ObservableCollection<Thumb> scans { get; set; } = new ObservableCollection<Thumb>();
    public ObservableCollection<Thumb> pics { get; set; } = new ObservableCollection<Thumb>();
    FileSystemWatcher watcher = new FileSystemWatcher();
    public static readonly string defaultScanDirectory = @"C:\Skanny\Scans";
    System.Threading.Timer clrHeader;
    Point startDragPoint;

    private void Window_Initialized(object sender, EventArgs e)
    {
      txtVersion.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
      ClearStatus();
      ApplySettings();

      LoadImages();
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
      if (settings.Default.UpgradeRequired)
      {
        settings.Default.Upgrade();
        settings.Default.UpgradeRequired = false;
        settings.Default.Save();
      }
      WindowState = settings.Default.LastWindowState;
      Width = settings.Default.LastWindowRect.Width;
      Height = settings.Default.LastWindowRect.Height;
      Top = settings.Default.LastWindowRect.Top;
      Left = settings.Default.LastWindowRect.Left;
      cmbSize.SelectedItem = (AvailableThumbSizes)settings.Default.LastThumbSize;
      SetSidebarLocation(settings.Default.SidebarLocation);
      if (settings.Default.LastView != 0)
      {
        SetViewPics();
      }
    }
    private void SaveSettings()
    {
      settings.Default.LastWindowState = this.WindowState;
      settings.Default.LastWindowRect = this.RestoreBounds;
      try
      {
        switch (settings.Default.SidebarLocation)
        {
          case 1:
            settings.Default.SidebarWidth = gridMain.ColumnDefinitions[2].Width.Value;
            break;
          case 2:
            settings.Default.SidebarWidth = gridMain.RowDefinitions[0].Height.Value;
            break;
          case 3:
            settings.Default.SidebarWidth = gridMain.RowDefinitions[2].Height.Value;
            break;
          default:
            settings.Default.SidebarWidth = gridMain.ColumnDefinitions[0].Width.Value;
            break;
        }
      }
      catch (Exception)
      {
      }
      settings.Default.LastThumbSize = (int)(AvailableThumbSizes)(cmbSize.SelectedItem ?? AvailableThumbSizes.Medium);
      settings.Default.LastView = rdoPics.IsChecked == true ? (byte)1 : (byte)0;
      settings.Default.Save();
    }
    private void UpdateStatus(object o)
    {
      UpdateStatus((o ?? string.Empty).ToString(), new SolidColorBrush(Colors.Transparent), new SolidColorBrush(Colors.White));
    }
    private void UpdateStatus(string message, Brush background, Brush foreground)
    {
      if (string.IsNullOrEmpty(message)) { ClearStatus(); return; }
      Dispatcher.Invoke(() =>
      {
        txtStatus.Text = message;
        txtStatus.Background = background ?? txtStatus.Background;
        txtStatus.Foreground = foreground ?? txtStatus.Foreground;
      });
      if (clrHeader == null)
      {
        clrHeader = new System.Threading.Timer(new System.Threading.TimerCallback(UpdateStatus), null, 30000, System.Threading.Timeout.Infinite);
      }
      else
      {
        clrHeader.Change(30000, System.Threading.Timeout.Infinite);
      }
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
      dynamic ColRow;
      switch (settings.Default.SidebarLocation)
      {
        default:
          ColRow = gridMain.ColumnDefinitions[0];
          break;
        case 1:
          ColRow = gridMain.ColumnDefinitions[2];
          break;
        case 2:
          ColRow = gridMain.RowDefinitions[0];
          break;
        case 3:
          ColRow = gridMain.RowDefinitions[2];
          break;
      }

      if (ColRow is RowDefinition)
      {
        if (((RowDefinition)ColRow).Height.Value > 0)
        {
          ((RowDefinition)ColRow).Height = new GridLength(0);
        }
        else
        {
          ((RowDefinition)ColRow).Height = new GridLength(settings.Default.SidebarWidth);
        }
      }
      else if (ColRow is ColumnDefinition)
      {
        if (((ColumnDefinition)ColRow).Width.Value > 0)
        {
          ((ColumnDefinition)ColRow).Width = new GridLength(0);
        }
        else
        {
          ((ColumnDefinition)ColRow).Width = new GridLength(settings.Default.SidebarWidth);
        }
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
    private bool DirectoryExistsOrCreate(string path)
    {
      if (Directory.Exists(path)) { return true; }
      else
      {
        try { Directory.CreateDirectory(path); }
        catch (Exception) { return false; }
        return true;
      }
    }
    private void OpenSettings()
    {
      winSettings w = new winSettings()
      {
        Owner = this,
        PathPics = settings.Default.PicDirectory,
        PathScans = settings.Default.ScanDirectory,
        Color = (ColorFormats)settings.Default.ScannerColorFormat,
        DPI = settings.Default.ScannerDPI,
        DefaultDevice = settings.Default.DefaultScannerId,
        KeepRecent = settings.Default.FilesToKeep
      };
      if (w.ShowDialog() == true)
      {
        if (settings.Default.ScanDirectory != w.PathScans)
        {
          if (DirectoryExistsOrCreate(w.PathScans))
          {
            settings.Default.ScanDirectory = w.PathScans;
            LoadImagesForScans();
          }
        }
        if (settings.Default.PicDirectory != w.PathPics)
        {
          if (DirectoryExistsOrCreate(w.PathScans))
          {
            settings.Default.PicDirectory = w.PathPics;
            LoadImagesForPics();
            watchPicDirectory();
          }
        }
        settings.Default.ScannerColorFormat = (int)w.Color;
        settings.Default.ScannerDPI = w.DPI;
        settings.Default.DefaultScannerId = w.DefaultDevice;
        if (settings.Default.FilesToKeep != w.KeepRecent)
        {
          settings.Default.FilesToKeep = w.KeepRecent;
          System.Threading.Tasks.Task.Run(() => { CleanScanDirectory(true); });
        }
        SaveSettings();
      }
    }
    private bool StartScan(out List<System.Drawing.Image> images)
    {
      bool ret = false;
      images = new List<System.Drawing.Image>();
      string defaultScanner = settings.Default.DefaultScannerId;
      Dictionary<string, string> devices = WiaScanner.GetDevices();
      if (!devices.Values.Contains(defaultScanner))
      {
        if (new winMessageBox(this, "Default Scanner missing", "The default scanner was not detected. Would you like to change the default scanner?", MessageBoxButton.YesNo, MessageBoxImage.Question).ShowDialog() == true)
        {
          OpenSettings();
        }
      }
      else
      {
        try
        {
          images = WiaScanner.Scan(defaultScanner);
          bool scanComplete = false;
          while (!scanComplete)
          {
            if (new winMessageBox(this, "Scan complete", "Scan more?", MessageBoxButton.YesNo, MessageBoxImage.Question).ShowDialog() == true)
            {
              try
              {
                images.AddRange(WiaScanner.Scan(defaultScanner));
              }
              catch (Exception ex)
              {
                if (images.Any())
                {
                  if (new winMessageBox(this, "Scan error", string.Format("{0}\nAttempt to scan again? If yes, insert document before confirming.", ex.Message), MessageBoxButton.YesNo, MessageBoxImage.Error).ShowDialog() == true)
                  {
                    images.AddRange(WiaScanner.Scan(defaultScanner));
                  }
                  else
                  {
                    return false;
                  }

                }
                else
                {
                  throw ex;
                }
              }
            }
            else
            {
              scanComplete = true;
            }
          }
          ret = true;
        }
        catch (Exception ex)
        {
          new winMessageBox(this, "Scan error", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error).ShowDialog();
          return false;
        }
      }
      return ret;
    }
    private void btnScan_Click(object sender, RoutedEventArgs e)
    {
      if (validateScanDir())
      {
        try
        {
          if (StartScan(out List<System.Drawing.Image> images))
          {
            if (images != null && images.Any())
            {
              var now = DateTime.Now.Ticks;
              var fileName = "";
              if (images.Count == 1)
              {
                fileName = string.Format(@"{0}\skannyscan_{1}.jpg", settings.Default.ScanDirectory, now);
                using (MemoryStream memory = new MemoryStream())
                {
                  using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite))
                  {
                    images.First().Save(memory, System.Drawing.Imaging.ImageFormat.Jpeg); //save the image to memory stream as Jpeg for compression 
                    byte[] bytes = memory.ToArray();
                    fs.Write(bytes, 0, bytes.Length);
                  }
                }
              }
              else if (images.Count > 1)
              {
                PdfDocument pdfDoc = new PdfDocument();
                pdfDoc.Info.Title = "Scan";
                pdfDoc.Info.Author = "Skanny";
                pdfDoc.Info.Subject = pdfDoc.Info.Title;

                foreach (var image in images)
                {
                  // Create an empty page	
                  PdfPage page = pdfDoc.AddPage();
                  // Get an XGraphics object for drawing
                  XGraphics gfx = XGraphics.FromPdfPage(page);
                  // add the scanned image
                  AddImage(gfx, page, image, 0, 0);
                }
                fileName = string.Format(@"{0}\skannyscan_{1}.pdf", settings.Default.ScanDirectory, now);
                pdfDoc.Save(fileName);
              }
              File.SetAttributes(fileName, FileAttributes.ReadOnly);
              GetAndInsertThumbFromFilename(fileName);
              SetViewScans();
              System.Threading.Tasks.Task.Run(() => { CleanScanDirectory(true); });
            }
            else
            {
              new winMessageBox(this, "Result", "No images were acquired in the scan process.", MessageBoxButton.OK, MessageBoxImage.Hand).ShowDialog();
            }
          }
          else
          {
            new winMessageBox(this, "Result", "Scanning of documents could not be completed.", MessageBoxButton.OK, MessageBoxImage.Hand).ShowDialog();
          }
        }
        catch (Exception ex)
        {
          new winMessageBox(this, "Error", ex.Message, MessageBoxButton.OK, MessageBoxImage.Error).ShowDialog();
        }
      }
    }
    private static void AddImage(XGraphics gfx, PdfPage page, System.Drawing.Image image, double xPosition, double yPosition, double? Width = null, double? Height = null)
    {
      try
      {
        using (MemoryStream stream = new MemoryStream())
        {
          image.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg); //save the image to memory stream as Jpeg for compression
          XImage xImg = XImage.FromStream(stream); //use XImage.FromStream DO NOT USE Image.FromStream!! PDF Document will be corrupted
          if (Width.HasValue && Height.HasValue)
          {
            gfx.DrawImage(xImg, xPosition, yPosition, Width.Value, Height.Value);
          }
          else
          {
            gfx.DrawImage(xImg, xPosition, yPosition);
          }
        }
      }
      catch (Exception)
      {
        // Handle exception
      }
    }
    private void BtnPdf_Click(object sender, RoutedEventArgs e)
    {
      var t = scans.Concat(pics).Where(p => p.ToPdf == true);
      if (t.Any())
      {
        var n = t.Where(p => p.Index != null).OrderBy(p => p.Index);
        if (n.Any() && n.Count() > 1)
        {
          if (new winMessageBox(this, "New document", string.Format("Create a {0} page PDF document from selected files?", n.Count()), MessageBoxButton.YesNo, MessageBoxImage.Question).ShowDialog() == true)
          //if (MessageBox.Show(string.Format("Create a {0} page PDF document from selected files?", n.Count()), "New document", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
          {
            // create the new pdf
            UpdateStatus("Creating new PDF. Please wait...");
            JoinDocuments(n);
          }
          else
          {
            UpdateStatus("New PDF aborted.");
          }
        }
        else
        {
          ClearStatus();
        }
        foreach (var x in scans)
        {
          x.ToPdf = false; x.Index = null;
        }
        foreach (var x in pics)
        {
          x.ToPdf = false; x.Index = null;
        }

      }
      else
      {
        UpdateStatus("Select the files to combine to PDF in the order that they should appear. Click [Join] again to complete/cancel.");
        foreach (var x in scans)
        {
          x.ToPdf = true; x.Index = null;
        }
        foreach (var x in pics)
        {
          x.ToPdf = true; x.Index = null;
        }

      }
    }
    private void cmbSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      ResizeThumbs();
    }
    private void ResizeThumbs()
    {
      var n = (int)(AvailableThumbSizes)(cmbSize.SelectedItem ?? AvailableThumbSizes.Medium);
      foreach (var x in scans)
      {
        ResizeThumb(x, n);
      }
      foreach (var x in pics)
      {
        ResizeThumb(x, n);
      }
    }
    private void ResizeThumb(Thumb t, int? s = null)
    {
      if (s == null) { s = (int)(AvailableThumbSizes)(cmbSize.SelectedItem ?? AvailableThumbSizes.Medium); }
      t.Width = s.Value;
      t.FontSize = t.Width / 5;
    }
    private void ResizeScanThumbs()
    {
      var n = (int)(AvailableThumbSizes)(cmbSize.SelectedItem ?? AvailableThumbSizes.Medium);
      foreach (var x in scans)
      {
        ResizeThumb(x, n);
      }
    }
    private void ResizePicThumbs()
    {
      var n = (int)(AvailableThumbSizes)(cmbSize.SelectedItem ?? AvailableThumbSizes.Medium);
      foreach (var x in pics)
      {
        ResizeThumb(x, n);
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
      rdoScans.IsChecked = true;
      listScans.Visibility = Visibility.Visible;
      listPics.Visibility = Visibility.Hidden;
    }
    private void SetViewPics()
    {
      rdoPics.IsChecked = true;
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
      if (!string.IsNullOrWhiteSpace(settings.Default.PicDirectory))
      {
        watcher.Path = settings.Default.PicDirectory;

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
    private bool validateScanDir()
    {
      if (string.IsNullOrWhiteSpace(settings.Default.ScanDirectory) || !Directory.Exists(settings.Default.ScanDirectory))
      {
        new winMessageBox(this, "Where to boss?", "Scan folder is not set or does not exist. Use Settings to change the folder.", MessageBoxButton.OK, MessageBoxImage.Warning).ShowDialog();
        return false;
      }
      return true;
    }
    private void UpdatePics()
    {
      //watcher.EnableRaisingEvents = false; // temporarily disable file system watcher
      LoadImagesForPics();
      //watcher.EnableRaisingEvents = true; // re enable file system watcher
      SetViewPics();
    }
    private void LoadImages()
    {
      LoadImagesForScans();
      LoadImagesForPics();
    }
    private void LoadImagesForScans()
    {
      scans.Clear();
      var t = GetThumbnails(settings.Default.ScanDirectory);
      if (t == null)
      {
        settings.Default.ScanDirectory = defaultScanDirectory;
        t = GetThumbnails(settings.Default.ScanDirectory);
      }
      if (t != null)
      {
        scans = new ObservableCollection<Thumb>(t);
      }
      listScans.ItemsSource = scans;
    }
    private void LoadImagesForPics()
    {
      pics.Clear();
      var t = GetThumbnails(settings.Default.PicDirectory);
      if (t == null)
      {
        settings.Default.PicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        t = GetThumbnails(settings.Default.PicDirectory);
      }
      if (t != null)
      {
        pics = new ObservableCollection<Thumb>(t);
      }
      listPics.ItemsSource = pics;
    }
    public Thumb GetThumbnail(FileInfo file)
    {
      Thumb t = null;
      bool fileLocked = true;
      var ext = Path.GetExtension(file.FullName).ToLower();
      switch (ext)
      {
        case ".pdf":
        case ".bmp":
        case ".gif":
        case ".jpg":
        case ".png":
        case ".tiff":
          for (int tries = 0; tries < 3; tries++)
          {
            try
            {
              // remove read-only attribute of file
              File.SetAttributes(file.FullName, FileAttributes.Normal);
              // Attempts to open then close the file in RW mode, denying other users to place any locks.
              using (FileStream fs = File.Open(file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
              {
                if (fs.Length > 0)
                {
                  fileLocked = false;
                }
              }
              if (!fileLocked)
              {
                break;
              }
            }
            catch (IOException)
            {
              try
              {
                File.SetAttributes(file.FullName, FileAttributes.ReadOnly);
              }
              catch (Exception)
              {
              }
              System.Threading.Thread.Sleep(100);
            }
          }
          if (!fileLocked)
          {
            t = new Thumb() { Created = file.CreationTime, FileSpec = file.FullName, Length = file.Length };
            int width = (int)(AvailableThumbSizes)cmbSize.SelectedItem;
            if (ext == ".pdf")
            {
              t.Image = GetPDFThumbnail(t.FileSpec);
            }
            else
            {
              Uri src = new Uri(t.FileSpec, UriKind.RelativeOrAbsolute);
              var bmp = new BitmapImage();
              bmp.BeginInit();
              bmp.CacheOption = BitmapCacheOption.OnLoad;
              bmp.UriSource = src;
              bmp.DecodePixelWidth = width < 500 ? 500 : width;
              bmp.EndInit();
              t.Image = bmp;
            }
            ResizeThumb(t);
            // turn on readonly attribute of file
            File.SetAttributes(file.FullName, FileAttributes.ReadOnly);
          }
          break;
      }
      return t;
    }
    public List<Thumb> GetThumbnails(string directory)
    {
      if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
      {
        return null;
      }
      List<Thumb> thumbList = new List<Thumb>();
      DirectoryInfo dir = new DirectoryInfo(directory);
      try
      {
        foreach (var file in dir.EnumerateFiles().OrderByDescending(x => x.CreationTime))
        {
          Thumb t = GetThumbnail(file);
          if (t != null)
          {
            thumbList.Add(t);
          }
        }
      }
      catch (Exception)
      {
      }
      return thumbList;
    }
    public BitmapImage GetPDFThumbnail(string filePath)
    {
      BitmapImage ret = new BitmapImage();
      int desired_x_dpi = 96;
      int desired_y_dpi = 96;

      GhostscriptVersionInfo gvi = GhostscriptVersionInfo.GetLastInstalledVersion(GhostscriptLicense.GPL | GhostscriptLicense.AFPL, GhostscriptLicense.GPL);
      try
      {
        //File.SetAttributes(filePath, FileAttributes.Normal);
        using (GhostscriptRasterizer rasterizer = new GhostscriptRasterizer())
        {
          rasterizer.Open(filePath, gvi, false);
          System.Drawing.Image tempImage = rasterizer.GetPage(desired_x_dpi, desired_y_dpi, 1);
          if (tempImage != null) // have to convert system.drawing.image to wpf image
          {
            using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
            {
              tempImage.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
              //memory.Position = 0;
              ret.BeginInit();
              ret.StreamSource = memory;
              ret.CacheOption = BitmapCacheOption.OnLoad;
              ret.EndInit();
            }
          }
        }
        //File.SetAttributes(filePath, FileAttributes.ReadOnly);
      }
      catch (Exception)
      {
        throw;
      }
      return ret;
    }
    private void _PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      startDragPoint = e.GetPosition(null);
    }
    private void thumbMouseMove(object sender, MouseEventArgs e)
    {
      if (sender is Grid g && g.DataContext is Thumb t && !t.ToPdf && e.LeftButton == MouseButtonState.Pressed)
      {
        Vector diff = startDragPoint - e.GetPosition(null);
        if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
          var obj = new DataObject();
          System.Collections.Specialized.StringCollection files = new System.Collections.Specialized.StringCollection { t.FileSpec };
          obj.SetFileDropList(files);
          DragDrop.DoDragDrop(new ContentElement(), obj, DragDropEffects.Copy);
        }
      }
    }
    private void thumbMouseEnter(object sender, MouseEventArgs e)
    {
      if (sender is Grid g && g.DataContext is Thumb t)
      {
        lblStatus.Content = string.Format("{0}", t.FileSpec);
        lblSize.Content = string.Format("{0:N2} MB", (t.Length / 1024f) / 1024f);
        lblMod.Content = string.Format("{0}", t.Created);
      }
    }
    private void thumbMouseLeave(object sender, MouseEventArgs e)
    {
      lblStatus.Content = null;
      lblSize.Content = null;
      lblMod.Content = null;
    }
    private void fileOpen(object sender, RoutedEventArgs e)
    {
      try
      {
        if (sender is MenuItem m && m.DataContext is Thumb t)
        {
          if (File.Exists(t.FileSpec))
          {
            UpdateStatus("Opening file...", null, null);
            Process process = new Process
            {
              StartInfo = new ProcessStartInfo
              {
                FileName = t.FileSpec
              }
            };
            process.Start();
          }
          else
          {
            UpdateStatus("File does not exist!", null, new SolidColorBrush(Colors.Yellow));
          }
        }
      }
      catch (Exception ex)
      {
        UpdateStatus(ex.Message, null, new SolidColorBrush(Colors.Red));
      }
    }
    private void fileDelete(object sender, RoutedEventArgs e)
    {
      try
      {
        if (sender is MenuItem m && m.DataContext is Thumb t)
        {
          if (File.Exists(t.FileSpec))
          {
            try
            {
              File.SetAttributes(t.FileSpec, FileAttributes.Normal);
              File.Delete(t.FileSpec);
              LoadImages();
              UpdateStatus("File deleted.", null, null);
            }
            catch (Exception ex)
            {
              UpdateStatus(ex.Message, null, new SolidColorBrush(Colors.Red));
            }
          }
          else
          {
            UpdateStatus("File does not exist!", null, new SolidColorBrush(Colors.Yellow));
          }
        }
      }
      catch (Exception ex)
      {
        UpdateStatus(ex.Message, null, new SolidColorBrush(Colors.Red));
      }
    }
    private void thumbGotFocus(object sender, RoutedEventArgs e)
    {
    }
    private void BtnReload_Click(object sender, RoutedEventArgs e)
    {
      LoadImages();
    }
    private void BtnConfig_Click(object sender, RoutedEventArgs e)
    {
      OpenSettings();
    }
    private void BtnClean_Click(object sender, RoutedEventArgs e)
    {
      CleanScanDirectory();
    }
    public void CleanScanDirectory(bool silent = false)
    {
      if (!validateScanDir())
      {
        UpdateStatus("Cannot clean folder. Folder not set or does not exist.", null, new SolidColorBrush(Colors.Red));
        return;
      }
      DirectoryInfo dir = new DirectoryInfo(settings.Default.ScanDirectory);
      int numFiles = 0;
      Dispatcher.Invoke(() =>
      {
        try
        {
          if (settings.Default.FilesToKeep == 0)
          {
            foreach (var file in dir.EnumerateFiles())
            {
              bool fileLocked = true;
              for (int i = 0; i < 3; i++)
              {
                try
                {
                  // remove readonly attribute before deleting
                  File.SetAttributes(file.FullName, FileAttributes.Normal);
                  // Attempts to open then close the file in RW mode, denying other users to place any locks.
                  using (FileStream fs = File.Open(file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                  {
                    if (fs.Length > 0)
                    {
                      fileLocked = false;
                    }
                  }
                  if (!fileLocked)
                  {
                    break;
                  }
                }
                catch (IOException)
                {
                  try
                  {
                    File.SetAttributes(file.FullName, FileAttributes.ReadOnly);
                  }
                  catch (Exception)
                  {
                  }
                  System.Threading.Thread.Sleep(50);
                }
              }
              if (!fileLocked)
              {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                file.Delete();
                numFiles++;
                var t = scans.Where(p => p.FileSpec == file.FullName);
                if (t.Any())
                { scans.Remove(t.First()); }
              }
              else
              {
                //throw new Exception(string.Format("{0} could not be deleted.", file.FullName));
              }
            }
          }
          else
          {
            foreach (var file in dir.EnumerateFiles().OrderByDescending(x => x.CreationTime).Skip(settings.Default.FilesToKeep))
            {
              bool fileLocked = true;
              for (int i = 0; i < 3; i++)
              {
                try
                {
                  // remove read-only attribute
                  File.SetAttributes(file.FullName, FileAttributes.Normal);
                  // Attempts to open then close the file in RW mode, denying other users to place any locks.
                  using (FileStream fs = File.Open(file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                  {
                    if (fs.Length > 0)
                    {
                      fileLocked = false;
                    }
                  }
                  if (!fileLocked)
                  {
                    break;
                  }
                }
                catch (IOException)
                {
                  try
                  {
                    File.SetAttributes(file.FullName, FileAttributes.ReadOnly);
                  }
                  catch (Exception)
                  {
                  }
                  System.Threading.Thread.Sleep(50);
                }
              }
              if (!fileLocked)
              {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                file.Delete();
                numFiles++;
                var t = scans.Where(p => p.FileSpec == file.FullName);
                if (t.Any())
                { scans.Remove(t.First()); }
              }
              else
              {
                //throw new Exception(string.Format("{0} could not be deleted.", file.FullName));
              }
            }
          }
        }
        catch (Exception)
        {
          throw;
        }

      });
      if (!silent) { UpdateStatus(string.Format("Folder cleaned. {0} files removed.", numFiles), null, new SolidColorBrush(Colors.Lime)); }
    }
    private void JoinDocuments(IEnumerable<Thumb> T)
    {
      if (T == null || !T.Any()) { return; }
      T = T.OrderBy(p => p.Index);
      using (PdfDocument doc = new PdfDocument())
      {
        foreach (var t in T)
        {
          try
          {
            bool TypeIsOk = false;
            bool TypeIsPdf = false;
            switch (Path.GetExtension(t.FileSpec).ToLower())
            {
              case ".pdf":
                TypeIsPdf = true;
                TypeIsOk = true;
                break;
              case ".jpg":
              case ".jpeg":
              case ".bmp":
              case ".gif":
              case ".png":
                TypeIsOk = true;
                break;
            }
            if (File.Exists(t.FileSpec) && TypeIsOk)
            {
              if (TypeIsPdf)
              {
                using (PdfDocument pdfDoc = PdfReader.Open(t.FileSpec, PdfDocumentOpenMode.Import))
                {
                  for (int x = 0; x < pdfDoc.PageCount; x++)
                  {
                    doc.AddPage(pdfDoc.Pages[x]);
                  }
                }
              }
              else
              {
                XImage img = XImage.FromFile(t.FileSpec);
                PdfPage page = doc.AddPage();
                var ratio = Math.Min((page.Width - 20) / img.PointWidth, (page.Height - 20) / img.PointHeight);
                XGraphics xgr = XGraphics.FromPdfPage(doc.Pages[doc.PageCount - 1]);
                xgr.DrawImage(img, 10, 10, img.PointWidth * ratio, img.PointHeight * ratio);
              }
            }
          }
          catch (Exception)
          {
          }
        }
        try
        {
          var f = string.Format(@"{0}\skannyscan_{1}.pdf", settings.Default.ScanDirectory, DateTime.Now.Ticks);
          doc.Save(f);
          GetAndInsertThumbFromFilename(f);
          UpdateStatus(string.Format("PDF created. {0}", f), null, null);
          System.Threading.Tasks.Task.Run(() => { CleanScanDirectory(true); });
          SetViewScans();
          return;
        }
        catch (Exception)
        {
        }
      }
      UpdateStatus("PDF not created.", null, null);
    }
    private void GetAndInsertThumbFromFilename(string fileName)
    {
      Thumb t = GetThumbnail(new FileInfo(fileName));
      if (t != null)
      {
        scans.Insert(0, t);
      }

    }
    private void btnLeft_Click(object sender, RoutedEventArgs e)
    {
      // align sidebar to left
      SetSidebarLocation(0);
    }
    private void btnRight_Click(object sender, RoutedEventArgs e)
    {
      // align sidebar to right
      SetSidebarLocation(1);
    }
    private void btnDown_Click(object sender, RoutedEventArgs e)
    {
      // align sidebar to bottom
      SetSidebarLocation(3);
    }
    private void btnUp_Click(object sender, RoutedEventArgs e)
    {
      // align sidebar to top
      SetSidebarLocation(2);
    }
    private void SetSidebarLocation(byte n)
    {
      switch (n)
      {
        default: // ############################################################### LEFT
          settings.Default.SidebarLocation = 0;
          SetWindowHorizontalStyling();
          gridMain.ColumnDefinitions[0].Width = new GridLength(settings.Default.SidebarWidth, GridUnitType.Pixel);
          gridMain.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
          Grid.SetColumn(LeftSidePanel, 0);
          Grid.SetColumn(gsMainSplitter, 1);
          Grid.SetColumn(gridMainView, 2);
          break;
        case 1: // ############################################################### RIGHT
          settings.Default.SidebarLocation = 1;
          SetWindowHorizontalStyling();
          gridMain.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
          gridMain.ColumnDefinitions[2].Width = new GridLength(settings.Default.SidebarWidth, GridUnitType.Pixel);
          Grid.SetColumn(LeftSidePanel, 2);
          Grid.SetColumn(gsMainSplitter, 1);
          Grid.SetColumn(gridMainView, 0);
          break;
        case 2: // ############################################################### TOP
          settings.Default.SidebarLocation = 2;
          SetWindowVerticalStyling();
          gridMain.RowDefinitions[0].Height = new GridLength(settings.Default.SidebarWidth, GridUnitType.Pixel);
          gridMain.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
          Grid.SetRow(LeftSidePanel, 0);
          Grid.SetRow(gsMainSplitter, 1);
          Grid.SetRow(gridMainView, 2);
          break;
        case 3: // ############################################################### BOTTOM
          settings.Default.SidebarLocation = 3;
          SetWindowVerticalStyling();
          gridMain.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
          gridMain.RowDefinitions[2].Height = new GridLength(settings.Default.SidebarWidth, GridUnitType.Pixel);
          Grid.SetRow(LeftSidePanel, 2);
          Grid.SetRow(gsMainSplitter, 1);
          Grid.SetRow(gridMainView, 0);
          break;
      }

    }
    private void SetWindowHorizontalStyling()
    {
      gridMain.ColumnDefinitions.Clear();
      gridMain.RowDefinitions.Clear();
      gridMain.ColumnDefinitions.Add(new ColumnDefinition()); // { Width = new GridLength(settings.Default.SidebarWidth, GridUnitType.Pixel) });
      gridMain.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
      gridMain.ColumnDefinitions.Add(new ColumnDefinition()); // { Width = new GridLength(1, GridUnitType.Star) });
      gsMainSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
      LeftSidePanel.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
      LeftSidePanel.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
      stackOptions.Orientation = Orientation.Vertical;
      stackOptions.Margin = new Thickness(0, 0, 0, 84);
      btnScan.Margin = new Thickness(0, 10, 0, 0);
      borderSidebarLocation.HorizontalAlignment = HorizontalAlignment.Stretch;
      borderSidebarLocation.VerticalAlignment = VerticalAlignment.Bottom;

    }
    private void SetWindowVerticalStyling()
    {
      gridMain.ColumnDefinitions.Clear();
      gridMain.RowDefinitions.Clear();
      gridMain.RowDefinitions.Add(new RowDefinition()); // { Height = new GridLength(settings.Default.SidebarWidth, GridUnitType.Pixel) });
      gridMain.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(5, GridUnitType.Pixel) });
      gridMain.RowDefinitions.Add(new RowDefinition()); // { Height = new GridLength(1, GridUnitType.Star) });
      gsMainSplitter.VerticalAlignment = VerticalAlignment.Stretch;
      LeftSidePanel.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
      LeftSidePanel.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
      stackOptions.Orientation = Orientation.Horizontal;
      stackOptions.Margin = new Thickness(0, 0, 84, 0);
      btnScan.Margin = new Thickness(10, 0, 0, 0);
      borderSidebarLocation.HorizontalAlignment = HorizontalAlignment.Right;
      borderSidebarLocation.VerticalAlignment = VerticalAlignment.Stretch;

    }
  }
}
