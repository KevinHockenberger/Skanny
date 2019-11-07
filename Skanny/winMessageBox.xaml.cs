using System;
using System.Windows;

namespace Skanny
{
  /// <summary>
  /// Interaction logic for NodeParamsWindow.xaml
  /// </summary>
  public partial class winMessageBox : Window
  {
    public new string Title { get { return lblTitle.Content.ToString(); } private set { lblTitle.Content = value; } }
    public string Caption { get { return txtPrompt.Text; } private set { txtPrompt.Text = value; } }
    public System.Windows.Media.Imaging.BitmapImage Image { get; set; }
    public winMessageBox(Window owner, string title, string caption, MessageBoxButton messageBoxButton, MessageBoxImage messageBoxImage = MessageBoxImage.None)
    {
      InitializeComponent();
      DataContext = this;
      Owner = owner;
      this.Title = title;
      this.Caption = caption;
      btnOk.Visibility = Visibility.Collapsed;
      btnCancel.Visibility = Visibility.Collapsed;
      btnYes.Visibility = Visibility.Collapsed;
      btnNo.Visibility = Visibility.Collapsed;
      switch (messageBoxButton)
      {
        case MessageBoxButton.OK:
          btnOk.Visibility = Visibility.Visible; btnOk.IsDefault = true; btnOk.IsCancel = true;
          btnCancel.Visibility = Visibility.Collapsed;
          btnYes.Visibility = Visibility.Collapsed;
          btnNo.Visibility = Visibility.Collapsed;
          break;
        case MessageBoxButton.OKCancel:
          btnOk.Visibility = Visibility.Visible; btnOk.IsDefault = true;
          btnCancel.Visibility = Visibility.Visible; btnCancel.IsCancel = true;
          btnYes.Visibility = Visibility.Collapsed;
          btnNo.Visibility = Visibility.Collapsed;
          break;
        case MessageBoxButton.YesNoCancel:
          btnYes.Visibility = Visibility.Visible;
          btnNo.Visibility = Visibility.Visible; btnNo.IsDefault = true;
          btnCancel.Visibility = Visibility.Visible; btnCancel.IsCancel = true;
          btnOk.Visibility = Visibility.Collapsed;
          break;
        case MessageBoxButton.YesNo:
          btnYes.Visibility = Visibility.Visible; btnYes.IsDefault = true;
          btnNo.Visibility = Visibility.Visible; btnNo.IsCancel = true;
          btnOk.Visibility = Visibility.Collapsed;
          btnCancel.Visibility = Visibility.Collapsed;
          break;
        default:
          break;
      }
      switch (messageBoxImage)
      {
        case MessageBoxImage.Stop:
          Image = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"pack://application:,,,/include/stop.png", UriKind.Absolute));
          icon.Width = 64;
          icon.Height = 64;
          break;
        case MessageBoxImage.Question:
          Image = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"pack://application:,,,/include/question.png", UriKind.Absolute));
          icon.Width = 64;
          icon.Height = 64;
          break;
        case MessageBoxImage.Warning:
          Image = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"pack://application:,,,/include/warning.png", UriKind.Absolute));
          icon.Width = 64;
          icon.Height = 64;
          break;
        case MessageBoxImage.Information:
          Image = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"pack://application:,,,/include/info.png", UriKind.Absolute));
          icon.Width = 64;
          icon.Height = 64;
          break;
        default:
          Image = null;
          icon.Width = 0;
          icon.Height = 0;
          break;
      }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = false;
      this.Close();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = true;
      this.Close();
    }

  }
}
