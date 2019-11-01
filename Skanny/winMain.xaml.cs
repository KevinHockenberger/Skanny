using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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
    public AvailableThumbSizes ThumbSize { get; set; }
    private void Window_Initialized(object sender, EventArgs e)
    {
      txtVersion.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
      ClearStatus();
      LoadSettings();
    }
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {

    }
    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      SaveSettings();
    }
    private void LoadSettings()
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
    }
    private void SaveSettings()
    {
      Properties.Settings.Default.LastWindowState = this.WindowState;
      Properties.Settings.Default.LastWindowRect = this.RestoreBounds;
      Properties.Settings.Default.SidebarWidth = gridMain.ColumnDefinitions[0].Width.Value;
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
    private void BtnApp_Click(object sender, RoutedEventArgs e)
    {

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

    private void cmbSize_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {

    }
  }
}
