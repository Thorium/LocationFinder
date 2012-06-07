using System.Windows;
using Microsoft.Phone.Controls;

namespace LocationFinder
{
    public partial class MainPage : PhoneApplicationPage
    {
        public ViewModel.MainViewModel ViewModel { get; set; }
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            this.DataContext = ViewModel = new ViewModel.MainViewModel();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Refresh();
        }
    }
}