using System.Windows;

namespace BCSH2_RPG_Generator.Views
{
    public partial class MainMenuView : Window
    {
        public MainMenuView()
        {
            InitializeComponent();
        }

        private void OtevriHry_Click(object sender, RoutedEventArgs e)
        {
            var okno = new SpravaHerView();
            Application.Current.MainWindow = okno; 
            okno.Show();
            this.Close();
        }

        private void OtevriPovolani_Click(object sender, RoutedEventArgs e)
        {
            var okno = new SpravaPovolaniView();
            Application.Current.MainWindow = okno;  
            okno.Show();
            this.Close();
        }

        private void OtevriSchopnosti_Click(object sender, RoutedEventArgs e)
        {
            var okno = new SpravaSchopnostiView();
            Application.Current.MainWindow = okno;  
            okno.Show();
            this.Close();
        }

        private void OtevriRasy_Click(object sender, RoutedEventArgs e)
        {
            var okno = new SpravaRasView();
            Application.Current.MainWindow = okno;  
            okno.Show();
            this.Close();
        }

        private void Konec_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
