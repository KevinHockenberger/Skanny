using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Skanny
{
  /// <summary>
  /// Interaction logic for Settings.xaml
  /// </summary>
  public partial class winSettings : Window
  {
    public winSettings()
    {
      InitializeComponent();
      this.Width = 600;
      this.Height = 500;
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

    }
  }
}
