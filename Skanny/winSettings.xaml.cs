using System;
using System.Collections.Generic;
using System.Windows;

namespace Skanny
{
  /// <summary>
  /// Interaction logic for Settings.xaml
  /// </summary>
  public partial class winSettings : Window
  {
    private readonly string DefaultScanDirectory = @"C:\Skanny\Scans";
    public int[] AvailableDpi { get; set; } = new int[] { 100, 150, 200, 300, 400, 500, 600 };
    public int[] AvailableNumberOfFilesToKeep { get; set; } = new int[] { 0, 1, 5, 10, 15, 20, 30, 50 };

    public string PathPics
    {
      get
      { return txtPicDirectory.Text; }
      set
      { txtPicDirectory.Text = value; }
    }
    public string PathScans
    {
      get
      { return txtScanDirectory.Text; }
      set
      { txtScanDirectory.Text = value; }
    }
    public int KeepRecent
    {
      get
      {
        //return int.TryParse((cmbRecentToKeep.SelectedValue ?? 10).ToString(), out var i) ? i : 10;
        return (int)cmbRecentToKeep.SelectedItem;
      }
      set
      {
        cmbRecentToKeep.SelectedItem = value;
      }
    }
    public int DPI
    {
      get
      {
        //return int.TryParse((cmbDpi.SelectedValue ?? 200).ToString(), out var i) ? i : 200;
        return (int)cmbDpi.SelectedItem;
      }
      set
      {
        cmbDpi.SelectedItem = value;
      }
    }
    public ColorFormats Color
    {
      get
      {
        return rdoColor.IsChecked == true ? ColorFormats.Color : rdoGray.IsChecked == true ? ColorFormats.Grayscale : ColorFormats.BlackandWhiteOnly;
      }
      set
      {
        switch (value)
        {
          case ColorFormats.BlackandWhiteOnly:
            rdoBlack.IsChecked = true;
            break;
          case ColorFormats.Grayscale:
            rdoGray.IsChecked = true;
            break;
          default:
            rdoColor.IsChecked = true;
            break;
        }
      }
    }
    public string DefaultDevice
    {
      get
      { return lblDefaultDevice.Content.ToString(); }
      set
      { lblDefaultDevice.Content = value; }
    }

    public winSettings()
    {
      InitializeComponent();
      DataContext = this;
      this.Width = 650;
      this.Height = 550;
      refreshScannerList();
      PathScans = DefaultScanDirectory;
      PathPics = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
      Color = ColorFormats.Color;
      KeepRecent = 10;
      DPI = 200;
    }
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = false;
      this.Close();
    }
    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = true;
      this.Close();
    }
    private void BtnClean_Click(object sender, RoutedEventArgs e)
    {
      if (Owner != null && Owner is winMain)
      {
        ((winMain)Owner).CleanScanDirectory();
      }
    }
    private void BtnDefaultDevice_Click(object sender, RoutedEventArgs e)
    {
      DefaultDevice = listDevices.SelectedItem.ToString();
    }
    private void BtnRefreshDevices_Click(object sender, RoutedEventArgs e)
    {
      refreshScannerList();
    }
    private async void refreshScannerList()
    {
      btnDefaultDevice.IsEnabled = false;
      btnRefreshDevices.IsEnabled = false;
      listDevices.Items.Clear();
      listDevices.Items.Refresh();
      await System.Threading.Tasks.Task.Delay(100);
      PopulateDeviceList();
    }
    private void PopulateDeviceList()
    {
      Dictionary<string, string> devices = WiaScanner.GetDevices();

      WIA.DeviceManager manager = new WIA.DeviceManager();
      foreach (WIA.DeviceInfo info in manager.DeviceInfos)
      {
        if (info.Type == WIA.WiaDeviceType.ScannerDeviceType)
        {
          if (devices.ContainsKey(info.DeviceID))
          {
            listDevices.Items.Add(devices[info.DeviceID]);
          }
        }
      }
      if (listDevices.Items.Count == 0)
      {
        lblAvailDevices.Content = "Available devices: None found";
      }
      else
      {
        lblAvailDevices.Content = "Available devices:";
        foreach (string item in listDevices.Items)
        {
          if (item == DefaultDevice)
          {
            listDevices.SelectedItem = item;
            break;
          }
        }
        if (listDevices.SelectedIndex == -1)
        {
          listDevices.SelectedIndex = 0;
        }
        btnDefaultDevice.IsEnabled = true;
      }
      btnRefreshDevices.IsEnabled = true;
    }
    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
      System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog() { SelectedPath = txtPicDirectory.Text };
      System.Windows.Forms.DialogResult result = d.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK)
      {
        txtPicDirectory.Text = d.SelectedPath;
      }
    }
    private void BtnBrowseScan_Click(object sender, RoutedEventArgs e)
    {
      System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog() { SelectedPath = txtScanDirectory.Text };
      System.Windows.Forms.DialogResult result = d.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK)
      {
        txtScanDirectory.Text = d.SelectedPath;
      }
    }
  }
}
