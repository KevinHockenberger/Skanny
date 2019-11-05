using Ghostscript.NET;
using Ghostscript.NET.Rasterizer;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
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
    GrayScale = 2,
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
    public static readonly string defaultScanDirectory = @"C:\\Skanny\Scans";

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
      if (Properties.Settings.Default.LastView != 0)
      {
        rdoPics.IsChecked = true;
      }
    }
    private void SaveSettings()
    {
      Properties.Settings.Default.LastWindowState = this.WindowState;
      Properties.Settings.Default.LastWindowRect = this.RestoreBounds;
      Properties.Settings.Default.SidebarWidth = gridMain.ColumnDefinitions[0].Width.Value;
      Properties.Settings.Default.LastThumbSize = (int)(AvailableThumbSizes)(cmbSize.SelectedItem ?? AvailableThumbSizes.Medium);
      Properties.Settings.Default.LastView = rdoPics.IsChecked == true ? (byte)1 : (byte)0;
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
    private void OpenSettings()
    {
      winSettings w = new winSettings(){ Owner = this};
      w.ShowDialog();
    }
    private bool StartScan(out List<System.Drawing.Image> images)
    {
      bool ret = false;
      images = new List<System.Drawing.Image>();
      string defaultScanner = Properties.Settings.Default.DefaultScannerId;
      Dictionary<string, string> devices = WiaScanner.GetDevices();
      if (!devices.Values.Contains(defaultScanner))
      {
        MessageBoxResult result = MessageBox.Show("The default scanner was not detected. Would you like to change the default scanner?", "Default Scanner Missing", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
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
            MessageBoxResult result = MessageBox.Show(string.Format("Scan more?", images.Count), "Scan Complete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
              try
              {
                images.AddRange(WiaScanner.Scan(defaultScanner));
              }
              catch (Exception ex)
              {
                if (images.Any())
                {
                  result = MessageBox.Show(string.Format("{0}\nAttempt to scan again? If yes, insert next page before confirming.", ex.Message), "Scan Error", MessageBoxButton.YesNo, MessageBoxImage.Question);
                  if (result == MessageBoxResult.Yes)
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
            else if (result == MessageBoxResult.No)
            {
              scanComplete = true;
            }
          }
          ret = true;
        }
        catch (Exception ex)
        {
          MessageBox.Show(ex.Message, "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                fileName = string.Format(@"{0}\skannyscan_{1}.jpg", Properties.Settings.Default.ScanDirectory, now);
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
                fileName = string.Format(@"{0}\skannyscan_{1}.pdf", Properties.Settings.Default.ScanDirectory, now);
                pdfDoc.Save(fileName);
              }
              File.SetAttributes(fileName, FileAttributes.ReadOnly);
              //Thumbs.CleanScanDirectory(Skanny.Properties.Settings.Default.ScanDirectory, Skanny.Properties.Settings.Default.ScanCount == 0 ? 1 : Skanny.Properties.Settings.Default.ScanCount);
              //getScannedThumbnails();
              //if (rdoPreviewScan.IsChecked != true)
              //{
              //  rdoPreviewScan.IsChecked = true; // fires rdoPreviewScan_Checked event                  
              //}
              //else
              //{
              //  setScanView();
              //}
              //loadScanThumbnails((string)cmbThumbSize.SelectedValue);
            }
            else
            {
              MessageBox.Show("No images were acquired in the scan process.");
            }
          }
          else
          {
            MessageBox.Show("Scanning of documents could not be completed.");
          }
        }
        catch (Exception ex)
        {
          MessageBox.Show(ex.Message);
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
    private bool validateScanDir()
    {
      if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ScanDirectory) || !Directory.Exists(Properties.Settings.Default.ScanDirectory))
      {
        MessageBox.Show("Scan folder is not set or does not exist. Use Settings to change the folder.");
        return false;
      }
      return true;
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
      ResizeThumbs();
    }
    private void LoadImages()
    {
      LoadImagesForScans();
      LoadImagesForPics();
      ResizeThumbs();
    }
    private void LoadImagesForScans()
    {
      scans.Clear();
      var t = GetThumbnails(Properties.Settings.Default.ScanDirectory);
      if (t == null)
      {
        Properties.Settings.Default.ScanDirectory = defaultScanDirectory;
        t = GetThumbnails(Properties.Settings.Default.ScanDirectory);
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
      var t = GetThumbnails(Properties.Settings.Default.PicDirectory);
      if (t == null)
      {
        Properties.Settings.Default.PicDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        t = GetThumbnails(Properties.Settings.Default.PicDirectory);
      }
      if (t != null)
      {
        pics = new ObservableCollection<Thumb>(t);
      }
      listPics.ItemsSource = pics;
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
                var t = new Thumb() { Created = file.CreationTime, FileSpec = file.FullName, Length = file.Length };
                thumbList.Add(t);
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
                  bmp.DecodePixelWidth = (width <= 200 ? 500 : (int)width);
                  bmp.EndInit();
                  t.Image = bmp;
                }
                // turn on readonly attribute of file
                File.SetAttributes(file.FullName, FileAttributes.ReadOnly);
              }
              break;
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
    private void thumbMouseMove(object sender, MouseEventArgs e)
    {
      if (sender is Grid g && g.DataContext is Thumb t && e.LeftButton == MouseButtonState.Pressed)
      {
        var obj = new DataObject();
        System.Collections.Specialized.StringCollection files = new System.Collections.Specialized.StringCollection();
        files.Add(t.FileSpec);
        obj.SetFileDropList(files);
        var dd = DragDrop.DoDragDrop(new ContentElement(), obj, DragDropEffects.Copy);
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
            MessageBox.Show("File does not exist!");
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
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
            }
            catch (Exception ex)
            {
              MessageBox.Show(ex.Message);
            }
          }
          else
          {
            MessageBox.Show("File does not exist!");
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message);
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
  }
}
