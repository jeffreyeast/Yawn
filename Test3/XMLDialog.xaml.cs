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

namespace Test3
{
    /// <summary>
    /// Interaction logic for XMLDialog.xaml
    /// </summary>
    public partial class XMLDialog : Window
    {
        public XMLDialog()
        {
            InitializeComponent();
        }

        private void CopyCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            Clipboard.SetText(XMLTextBlock.Text);
            e.Handled = true;
        }
    }
}
