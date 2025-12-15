using System.Windows.Media.Imaging;
using System.Windows;

namespace BCSH2_RPG_Generator.Views
{
    public enum CustomMessageBoxButtons
    {
        OK,
        OKCancel,
        YesNo
    }

    public partial class CustomMessageBox : Window
    {
        private MessageBoxResult _result = MessageBoxResult.None;

        public CustomMessageBox(string message, string title, CustomMessageBoxButtons buttons)
        {
            InitializeComponent();

            TitleText.Text = title;
            MessageText.Text = message;

            ConfigureButtons(buttons);
        }

        private void ConfigureButtons(CustomMessageBoxButtons buttons)
        {
            switch (buttons)
            {
                case CustomMessageBoxButtons.OK:
                    Btn1.Content = "OK";
                    Btn2.Visibility = Visibility.Collapsed;
                    break;

                case CustomMessageBoxButtons.OKCancel:
                    Btn1.Content = "OK";
                    Btn2.Content = "Zrušit";
                    Btn2.Visibility = Visibility.Visible;
                    break;

                case CustomMessageBoxButtons.YesNo:
                    Btn1.Content = "Ano";
                    Btn2.Content = "Ne";
                    Btn2.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            _result = Btn1.Content.ToString() switch
            {
                "OK" => MessageBoxResult.OK,
                "Ano" => MessageBoxResult.Yes,
                _ => MessageBoxResult.OK
            };
            Close();
        }

        private void Btn2_Click(object sender, RoutedEventArgs e)
        {
            _result = Btn2.Content.ToString() switch
            {
                "Ne" => MessageBoxResult.No,
                "Zrušit" => MessageBoxResult.Cancel,
                _ => MessageBoxResult.Cancel
            };
            Close();
        }

        public static MessageBoxResult Show(string message, string title, CustomMessageBoxButtons buttons = CustomMessageBoxButtons.OK)
        {
            var msg = new CustomMessageBox(message, title, buttons);
            msg.ShowDialog();
            return msg._result;
        }
    }
}
