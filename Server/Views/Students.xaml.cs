using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NORSU.EncodeMe.Views
{
    /// <summary>
    /// Interaction logic for Students.xaml
    /// </summary>
    public partial class Students : UserControl
    {
        public Students()
        {
            InitializeComponent();
        }

        private void Search_MouseLeave(object sender, MouseEventArgs e)
        {
            if (SearchBox.IsFocused || ViewModels.Students.Instance.EnableAdvanceFilter) return;
            var anim = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(111)));
            SearchBox.BeginAnimation(TextBox.WidthProperty,anim);
            SearchBox.Opacity = 0.7;
            SearchBox.Margin=new Thickness(0);
        }

        private void Search_MouseEnter(object sender, MouseEventArgs e)
        {
            var anim = new DoubleAnimation(222, new Duration(TimeSpan.FromMilliseconds(111)));
            SearchBox.BeginAnimation(TextBox.WidthProperty, anim);
            SearchBox.Opacity = 1;
            SearchBox.Margin = new Thickness(7);
        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ViewModels.Students.Instance.EnableAdvanceFilter) return;
            var anim = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(111)));
            SearchBox.BeginAnimation(TextBox.WidthProperty, anim);
            SearchBox.Opacity = 0.7;
            SearchBox.Margin = new Thickness(0);
        }
    }
}
