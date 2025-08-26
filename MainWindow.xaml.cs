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
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LIB
{
    /// <summary>
    /// Класс для представления книги
    /// </summary>
    public class Book
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime AddedDate { get; set; }

        public Book(string title, string author, string filePath, string fileName)
        {
            Title = title;
            Author = author;
            FilePath = filePath;
            FileName = fileName;
            AddedDate = DateTime.Now;
        }

        public override string ToString()
        {
            if (Author == "Не указан")
            {
                return $"{Title} ({FileName})";
            }
            return $"{Title} - {Author} ({FileName})";
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isDarkTheme = true;
        private List<Book> books = new List<Book>();
        private readonly string booksFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "books.json");

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

            // Загружаем книги из JSON файла
            LoadBooksFromJson();
            
            // Инициализируем отображение книг
            UpdateBooksDisplay();
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

        private void LibraryTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Возврат на главную страницу
            // Здесь можно добавить логику для сброса к главной странице
        }

        private void AddBookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаём диалог выбора файла
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Выберите книгу для добавления";
                openFileDialog.Filter = "Все файлы (*.*)|*.*|Текстовые файлы (*.txt)|*.txt|PDF файлы (*.pdf)|*.pdf|Word документы (*.doc;*.docx)|*.doc;*.docx";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = false;

                // Показываем диалог
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = System.IO.Path.GetFileName(filePath);
                    
                    // Убираем расширение файла для названия книги
                    string title = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    
                    // Автор по умолчанию
                    string author = "Не указан";

                    // Создаём новую книгу и добавляем в список
                    Book newBook = new Book(title, author, filePath, fileName);
                    books.Add(newBook);

                    // Сохраняем книги в JSON файл
                    SaveBooksToJson();

                    // Обновляем отображение
                    UpdateBooksDisplay();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении книги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateBooksDisplay()
        {
            // Обновляем ListBox со списком книг
            BooksListBox.ItemsSource = null;
            BooksListBox.ItemsSource = books;

            // Обновляем статистику
            TotalBooksText.Text = $"Всего книг: {books.Count}";
            
            if (books.Count > 0)
            {
                // Скрываем текст "Книги ещё не добавлены"
                NoBooksText.Visibility = Visibility.Collapsed;
                
                // Показываем последнюю добавленную книгу
                Book lastBook = books[books.Count - 1];
                LastAddedText.Text = $"Последняя добавлена: {lastBook.Title}";
            }
            else
            {
                // Показываем текст "Книги ещё не добавлены"
                NoBooksText.Visibility = Visibility.Visible;
                LastAddedText.Text = "Последняя добавлена: -";
            }
        }

        /// <summary>
        /// Загружает книги из JSON файла
        /// </summary>
        private void LoadBooksFromJson()
        {
            try
            {
                if (File.Exists(booksFilePath))
                {
                    string jsonContent = File.ReadAllText(booksFilePath);
                    if (!string.IsNullOrWhiteSpace(jsonContent))
                    {
                        books = JsonSerializer.Deserialize<List<Book>>(jsonContent) ?? new List<Book>();
                    }
                }
            }
            catch (Exception ex)
            {
                // Тихо обрабатываем ошибку, создаем пустой список
                books = new List<Book>();
            }
        }

        /// <summary>
        /// Сохраняет книги в JSON файл
        /// </summary>
        private void SaveBooksToJson()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                string jsonContent = JsonSerializer.Serialize(books, options);
                File.WriteAllText(booksFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                // Тихо обрабатываем ошибку
            }
        }

        /// <summary>
        /// Очищает список книг
        /// </summary>
        private void ClearBooksButton_Click(object sender, RoutedEventArgs e)
        {
            books.Clear();
            SaveBooksToJson();
            UpdateBooksDisplay();
        }

        /// <summary>
        /// Двойной клик по книге - открывает файл
        /// </summary>
        private void BooksListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BooksListBox.SelectedItem is Book selectedBook)
            {
                OpenBookFile(selectedBook);
            }
        }

        /// <summary>
        /// Открывает файл книги
        /// </summary>
        private void OpenBookFile_Click(object sender, RoutedEventArgs e)
        {
            if (BooksListBox.SelectedItem is Book selectedBook)
            {
                OpenBookFile(selectedBook);
            }
        }

        /// <summary>
        /// Удаляет выбранную книгу
        /// </summary>
        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            if (BooksListBox.SelectedItem is Book selectedBook)
            {
                books.Remove(selectedBook);
                SaveBooksToJson();
                UpdateBooksDisplay();
            }
        }

        /// <summary>
        /// Показывает информацию о книге
        /// </summary>
        private void ShowBookInfo_Click(object sender, RoutedEventArgs e)
        {
            if (BooksListBox.SelectedItem is Book selectedBook)
            {
                string authorDisplay = selectedBook.Author == "Не указан" ? "Не указан" : selectedBook.Author;
                string info = $"📚 Название: {selectedBook.Title}\n" +
                             $"✍️ Автор: {authorDisplay}\n" +
                             $"📁 Файл: {selectedBook.FileName}\n" +
                             $"📂 Путь: {selectedBook.FilePath}\n" +
                             $"📅 Добавлена: {selectedBook.AddedDate:dd.MM.yyyy HH:mm}";
                
                MessageBox.Show(info, "Информация о книге", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Открывает файл книги в системе
        /// </summary>
        private void OpenBookFile(Book book)
        {
            try
            {
                if (File.Exists(book.FilePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = book.FilePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                // Тихо обрабатываем ошибку
            }
        }

        /// <summary>
        /// Изменяет название книги (кнопка в списке)
        /// </summary>
        private void EditBookTitle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                // Создаем простое окно для редактирования
                var editWindow = new Window
                {
                    Title = "Изменить название книги",
                    Width = 400,
                    Height = 220,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize,
                    Background = this.Resources["WindowBackgroundBrush"] as SolidColorBrush
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var titleLabel = new TextBlock
                {
                    Text = "Новое название:",
                    Margin = new Thickness(15, 20, 15, 10),
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = this.Resources["TextBrush"] as SolidColorBrush
                };
                Grid.SetRow(titleLabel, 0);

                var titleTextBox = new TextBox
                {
                    Text = book.Title,
                    Margin = new Thickness(15, 10, 15, 20),
                    FontSize = 14,
                    Height = 35,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                    BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                    Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                    CaretBrush = this.Resources["TextBrush"] as SolidColorBrush
                };
                Grid.SetRow(titleTextBox, 1);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(15, 10, 15, 20)
                };
                Grid.SetRow(buttonPanel, 2);

                var saveButton = new Button
                {
                    Content = "Сохранить",
                    Width = 90,
                    Height = 35,
                    Margin = new Thickness(8, 0, 0, 0),
                    Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                    BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                    Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                    Style = this.Resources["RoundedButtonStyle"] as Style,
                    FocusVisualStyle = null,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold
                };

                var cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 90,
                    Height = 35,
                    Margin = new Thickness(8, 0, 0, 0),
                    Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                    BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                    Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                    Style = this.Resources["RoundedButtonStyle"] as Style,
                    FocusVisualStyle = null,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold
                };

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);

                grid.Children.Add(titleLabel);
                grid.Children.Add(titleTextBox);
                grid.Children.Add(buttonPanel);

                editWindow.Content = grid;

                // Обработчики событий
                saveButton.Click += (s, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(titleTextBox.Text))
                    {
                        book.Title = titleTextBox.Text.Trim();
                        SaveBooksToJson();
                        UpdateBooksDisplay();
                        editWindow.Close();
                    }
                };

                cancelButton.Click += (s, args) => editWindow.Close();

                // Фокус на текстовое поле и Enter для сохранения
                titleTextBox.Focus();
                titleTextBox.KeyDown += (s, args) =>
                {
                    if (args.Key == Key.Enter)
                    {
                        saveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    else if (args.Key == Key.Escape)
                    {
                        editWindow.Close();
                    }
                };

                editWindow.ShowDialog();
            }
        }

        /// <summary>
        /// Удаляет книгу (кнопка в списке)
        /// </summary>
        private void DeleteBookInline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                books.Remove(book);
                SaveBooksToJson();
                UpdateBooksDisplay();
            }
        }
    }
}