using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LIB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isDarkTheme = true;

        public MainWindow()
        {
            InitializeComponent();
            
            // Инициализация тёмной темы по умолчанию
            this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            this.Resources["TextBrush"] = new SolidColorBrush(Colors.White);
            this.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
            this.Resources["ButtonBorderBrush"] = new SolidColorBrush(Color.FromRgb(96, 96, 96));
            
            // Разворачиваем окно на весь экран при запуске
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (isDarkTheme)
            {
                // Переключение на светлую тему
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["TextBrush"] = new SolidColorBrush(Colors.Black);
                this.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                this.Resources["ButtonBorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                
                ThemeToggleButton.Content = "🌙 Тёмная тема";
                isDarkTheme = false;
                
                // Принудительное обновление UI
                this.InvalidateVisual();
            }
            else
            {
                // Переключение на тёмную тему
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                this.Resources["TextBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                this.Resources["ButtonBorderBrush"] = new SolidColorBrush(Color.FromRgb(96, 96, 96));
                
                ThemeToggleButton.Content = "☀️ Светлая тема";
                isDarkTheme = true;
                
                // Принудительное обновление UI
                this.InvalidateVisual();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}