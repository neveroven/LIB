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
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;


namespace LIB
{
    /// Класс для хранения прогресса чтения
    public class ReadingProgress
    {
        public string FilePath { get; set; }
        public int CurrentPage { get; set; }
        public double ProgressPercentage { get; set; }
        public DateTime LastReadDate { get; set; }
        public int TotalPages { get; set; }

        public ReadingProgress(string filePath, int currentPage, double progressPercentage, int totalPages)
        {
            FilePath = filePath;
            CurrentPage = currentPage;
            ProgressPercentage = progressPercentage;
            TotalPages = totalPages;
            LastReadDate = DateTime.Now;
        }
    }

    /// Класс для представления книги

    public class Book
    {
        public int BookID{ get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime AddedDate { get; set; }
        public string CoverImageSource { get; set; } 
        public double ProgressWidth { get; set; } 
        public string ProgressText { get; set; } 

        public Book(int bookid,string title, string author, string filePath, string fileName)
        {
            BookID = bookid;
            Title = title;
            Author = author;
            FilePath = filePath;
            FileName = fileName;
            AddedDate = DateTime.Now;
            CoverImageSource = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\unknown.png"); 
            ProgressWidth = 0;
            ProgressText = "";
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

    public partial class MainWindow : Window
    {
        private bool isDarkTheme = false;
        private bool isLogin = false;
        private List<Book> books = new List<Book>();
        private int num_index = -1;
        private readonly string booksFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "books.json");
        private readonly string readingProgressFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reading_progress.json");
        private string currentXmlContent = ""; 
        private void index_found() 
        { 
            if (num_index == -1|| books.Count==0) 
                num_index = 0; 
            else  
                num_index = books[books.Count-1].BookID + 1; 
        }
        
        // Система страниц
        private List<string> bookPages = new List<string>();
        private int currentPageIndex = 0;
        private Book? currentBook = null;
        
        // Прогресс чтения
        private Dictionary<string, ReadingProgress> readingProgress = new Dictionary<string, ReadingProgress>();

        public MainWindow()
        {
            index_found();
            InitializeComponent();

            //// Инициализация тёмной темы по умолчанию
            //this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            //this.Resources["TextBrush"] = new SolidColorBrush(Colors.White);
            //this.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
            //this.Resources["ButtonBorderBrush"] = new SolidColorBrush(Color.FromRgb(96, 96, 96));

            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;

            // Загружаем книги из JSON файла
            LoadBooksFromJson();
            
            // Загружаем прогресс чтения
            LoadReadingProgress();
            
            // Инициализируем отображение книг
            UpdateBooksDisplay();
            
            // Добавляем обработчик для кнопки чтения
            ReadSelectedBookButton.Click += ReadSelectedBook_Click;
            
            // Добавляем обработчик для кнопки возврата
            BackToLibraryButton.Click += BackToLibrary_Click;
            
            // Добавляем обработчик для кнопки XML
            ShowXmlButton.Click += ShowXml_Click;
            
            // Добавляем обработчики для кнопок навигации
            PreviousPageButton.Click += PreviousPage_Click;
            NextPageButton.Click += NextPage_Click;
            
            // Добавляем обработчики клавиатуры для навигации по страницам
            this.KeyDown += MainWindow_KeyDown;
            
            // Добавляем обработчик для кнопки возврата из грида
            BackToWelcomeButton.Click += BackToWelcome_Click;
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (isDarkTheme)
            {
                // Переключение на светлую тему
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["TextBrush"] = new SolidColorBrush(Colors.Black);
                this.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 218, 185));
                this.Resources["ButtonBorderBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                
                ThemeToggleButton.Content = "🌙 Тёмная тема";
                isDarkTheme = false;
                this.InvalidateVisual();
            }
            else
            {
                // Переключение на тёмную тему
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                this.Resources["TextBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                this.Resources["ButtonBorderBrush"] = new SolidColorBrush(Color.FromRgb (130, 130,130));
                
                ThemeToggleButton.Content = "☀️ Светлая тема";
                isDarkTheme = true;
                this.InvalidateVisual();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void LibraryTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isLogin)
            {
                BooksButton.Visibility = Visibility.Visible;
                NavigationButtons.Visibility = Visibility.Visible;
                AutorisationPanel.Visibility = Visibility.Collapsed;
                BackToLibraryButton.Visibility = Visibility.Collapsed;
                BooksGridPanel.Visibility = Visibility.Collapsed;
                ReadingPanel.Visibility = Visibility.Collapsed;
                WelcomePanel.Visibility = Visibility.Visible;
                UpdateBooksDisplay();
            }
            }

        private void AddBookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                index_found();
                // Создаём диалог выбора файла
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Выберите книгу для добавления";
                openFileDialog.Filter = "Все файлы (*.*)|*.*|Текстовые файлы (*.txt)|*.txt|FictionBook (*.fb2)|*.fb2|XML файлы (*.xml)|*.xml|RTF файлы (*.rtf)|*.rtf|Markdown (*.md)|*.md|PDF файлы (*.pdf)|*.pdf|Word документы (*.doc;*.docx)|*.doc;*.docx";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = false;

                // Показываем диалог
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = System.IO.Path.GetFileName(filePath);
                    
                    string title = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    
                    string author = "Не указан";

                    Book newBook = new Book(num_index,title, author, filePath, fileName);

                    newBook.CoverImageSource = GetCoverPlaceholder(filePath);
                    
                    books.Add(newBook);

                    SaveBooksToJson();

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
            
            // Обновляем грид книг
            UpdateBooksGridDisplay();

            // Обновляем статистику
            TotalBooksText.Text = $"Всего книг: {books.Count}";
            
            if (books.Count > 0)
            {
                NoBooksText.Visibility = Visibility.Collapsed;
                
                // Показываем последнюю добавленную книгу
                Book lastBook = books[books.Count - 1];
                LastAddedText.Text = $"Последняя добавлена: {lastBook.Title}";
                
                // Показываем статистику прогресса чтения
                int booksWithProgress = readingProgress.Count;
                if (booksWithProgress > 0)
                {
                    TotalBooksText.Text += $" | Читается: {booksWithProgress}";
                }
            }
            else
            {
                // Показываем текст "Книги ещё не добавлены"
                NoBooksText.Visibility = Visibility.Visible;
                LastAddedText.Text = "Последняя добавлена: -";
            }
        }

        /// Загружает книги из JSON файла
        
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
                books = new List<Book>();
            }
        }

        /// Загружает прогресс чтения из JSON файла

        private void LoadReadingProgress()
        {
            try
            {
                if (File.Exists(readingProgressFilePath))
                {
                    string jsonContent = File.ReadAllText(readingProgressFilePath);
                    if (!string.IsNullOrWhiteSpace(jsonContent))
                    {
                        var progressList = JsonSerializer.Deserialize<List<ReadingProgress>>(jsonContent) ?? new List<ReadingProgress>();
                        readingProgress.Clear();
                        
                        foreach (var progress in progressList)
                        {
                            readingProgress[progress.FilePath] = progress;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                readingProgress = new Dictionary<string, ReadingProgress>();
            }
        }
        
        /// Сохраняет прогресс чтения в JSON файл

        private void SaveReadingProgress()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                string jsonContent = JsonSerializer.Serialize(readingProgress.Values.ToList(), options);
                File.WriteAllText(readingProgressFilePath, jsonContent);
            }
            catch (Exception ex)
            {}
        }
        
        /// Сохраняет книги в JSON файл

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
            {}
        }

        /// Очищает список книг

        private void ClearBooksButton_Click(object sender, RoutedEventArgs e)
        {
            readingProgress.Clear();
            SaveReadingProgress();
            books.Clear();
            SaveBooksToJson();
            UpdateBooksDisplay();
        }

        /// Двойной клик по книге - открывает панель чтения

        private void BooksListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BooksListBox.SelectedItem is Book selectedBook)
            {
                ShowReadingPanel(selectedBook);
            }
        }

        /// Открывает файл книги

        private void OpenBookFile_Click(object sender, RoutedEventArgs e)
        {
            if (BooksListBox.SelectedItem is Book selectedBook)
            {
                OpenBookFile(selectedBook);
            }
        }

        /// Удаляет выбранную книгу

        private void DeleteBook_Click(object sender, RoutedEventArgs e)
        {
            if (BooksListBox.SelectedItem is Book selectedBook)
            {
                books.Remove(selectedBook);
                SaveBooksToJson();
                UpdateBooksDisplay();
            }
        }

        /// Показывает информацию о книге

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

        /// Открывает файл книги в системе

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
            {}
        }

        /// Изменяет название книги (кнопка в списке)

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

        /// Удаляет книгу (кнопка в списке)

        private void DeleteBookInline_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                readingProgress.Remove(book.FilePath);
                books.Remove(book);
                SaveBooksToJson();
                UpdateBooksDisplay();
            }
        }
        
        /// Открывает панель чтения для выбранной книги

        private void ReadBook_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu)
            {
                if (contextMenu.PlacementTarget is ListBox listBox && listBox.SelectedItem is Book book)
                {
                    ShowReadingPanel(book);
                }
            }
        }
        
        /// Открывает панель чтения для книги (кнопка в списке)

        private void ReadBookInline_Click(object sender, RoutedEventArgs e)
        {
            if ((sender is Button button && button.Tag is Book book) )
            {
                ShowReadingPanel(book);
            }
            
        }
        /// Открывает панель чтения для книги (Нажатие на название книги)
        private void ReadBookText_Click(object sender, RoutedEventArgs e)
        {
            if ((sender is TextBlock block && block.DataContext is Book book))
            {
                ShowReadingPanel(book);
            }

        }

        /// Читает выбранную книгу (кнопка в панели быстрых действий)

        private void ReadSelectedBook_Click(object sender, RoutedEventArgs e)
        {
            if (BooksListBox.SelectedItem is Book selectedBook)
            {
                ShowReadingPanel(selectedBook);
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите книгу для чтения", 
                              "Книга не выбрана", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
            }
        }
        
        /// Показывает панель чтения для выбранной книги
        private void ShowReadingPanel(Book book)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            BooksGridPanel.Visibility = Visibility.Collapsed;
            ReadingPanel.Visibility = Visibility.Visible;
            BackToLibraryButton.Visibility = Visibility.Visible;

            currentBook = book;
            
            ReadingBookTitle.Text = book.Title;
            ReadingBookAuthor.Text = book.Author;
            
            // Показываем содержимое книги
            ShowBookContentPlaceholder(book);
            
            // Загружаем сохраненный прогресс чтения
            LoadBookProgress(book);
            
            // Обновляем статус
            StatusText.Text = "Книга загружена";
            ProgressText.Text = "Прогресс чтения: 0%";
            PageText.Text = "Страница 1 из 1";
            ReadingProgressBar.Value = 0;
            
        }
        
        /// Показывает содержимое книги с разбивкой на страницы

        private async void ShowBookContentPlaceholder(Book book)
        {
            try
            {

                StatusText.Text = "Загрузка книги...";
                
                // Пытаемся прочитать содержимое файла
                string content;
                if (System.IO.Path.GetExtension(book.FilePath).ToLower() == ".pdf")
                {
                    // Для PDF используем асинхронное чтение
                    content = await ReadBookContentAsync(book.FilePath);
                }
                else
                {
                    // Для других форматов используем синхронное чтение
                    content = ReadBookContent(book.FilePath);
                }
                
                if (!string.IsNullOrEmpty(content))
                {
                    // Создаём страницы из содержимого
                    CreateBookPages(content);
                    
                    StatusText.Text = "Книга успешно загружена. Используйте стрелки ← → для навигации";
                    
                }
                else
                {
                    ShowErrorContent(book);
                }
            }
            catch (Exception ex)
            {
                ShowErrorContent(book, ex.Message);
            }
        }
        
        /// Читает содержимое файла книги

        private async Task<string> ReadBookContentAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Файл не найден");
            }
            
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            
            switch (extension)
            {
                case ".txt":
                    return ReadTextFile(filePath);
                case ".md":
                    return ReadTextFile(filePath);
                case ".rtf":
                    return ReadRtfFile(filePath);
                case ".fb2":
                    return ReadFictionBookFile(filePath);
                case ".xml":
                    return ReadXmlFile(filePath);
                case ".pdf":
                    return await ReadPdfFileAsync(filePath);
                case ".doc":
                case ".docx":
                    return ReadWordFile(filePath);
                default:
                    return ReadTextFile(filePath); 
            }
        }

        /// Читает содержимое файла книги (синхронная версия для совместимости)

        private string ReadBookContent(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Файл не найден");
            }
            
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            
            switch (extension)
            {
                case ".txt":
                    return ReadTextFile(filePath);
                case ".md":
                    return ReadTextFile(filePath);
                case ".rtf":
                    return ReadRtfFile(filePath);
                case ".fb2":
                    return ReadFictionBookFile(filePath);
                case ".xml":
                    return ReadXmlFile(filePath);
                case ".pdf":
                    return ReadPdfFile(filePath);
                case ".doc":
                case ".docx":
                    return ReadWordFile(filePath);
                default:
                    return ReadTextFile(filePath);
            }
        }
        
        /// Читает текстовый файл

        private string ReadTextFile(string filePath)
        {
            try
            {
                string[] encodings = { "UTF-8", "Windows-1251", "UTF-16", "ASCII" };
                
                foreach (string encodingName in encodings)
                {
                    try
                    {
                        Encoding encoding = Encoding.GetEncoding(encodingName);
                        string content = File.ReadAllText(filePath, encoding);
                        if (IsTextContent(content))
                        {
                            return FormatTextContent(content);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        /// Читает RTF файл
        
        private string ReadRtfFile(string filePath)
        {
            try
            {
                // Простое чтение RTF как текста (убираем RTF разметку)
                string rtfContent = File.ReadAllText(filePath, Encoding.UTF8);
                return CleanRtfContent(rtfContent);
            }
            catch
            {
                return null;
            }
        }
        
        /// Читает PDF файл с полной поддержкой (асинхронно для больших файлов)

        private async Task<string> ReadPdfFileAsync(string filePath)
        {
            return await Task.Run(() => ReadPdfFile(filePath));
        }
        
        /// Читает PDF файл с полной поддержкой

        private string ReadPdfFile(string filePath)
        {
            try
            {
                using (PdfDocument document = PdfDocument.Open(filePath))
                {
                    var result = new StringBuilder();
                    
                    // Получаем информацию о документе
                    var information = document.Information;
                    var title = information?.Title ?? "Неизвестно";
                    var author = information?.Author ?? "Неизвестно";
                    var subject = information?.Subject ?? "";
                    var creator = information?.Creator ?? "";
                    var producer = information?.Producer ?? "";
                    var creationDate = information?.CreationDate?.ToString() ?? "";
                    // var modificationDate = information?.ModificationDate?.ToString("dd.MM.yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture) ?? "";
                    
                    // Убираем метаданные и заголовки для чистого чтения
                    
                    int pageCount = 0;
                    int totalPages = document.NumberOfPages;
                    
                    // Убираем системную отладку для чистого чтения
                    
                    foreach (UglyToad.PdfPig.Content.Page page in document.GetPages())
                    {
                        pageCount++;
                        
                        // Извлекаем текст со страницы
                        string pageText = page.Text;
                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            // Очищаем и форматируем текст
                            pageText = CleanPdfText(pageText);
                            result.AppendLine(pageText);
                        }
                        else
                        {
                            result.AppendLine("[Страница не содержит текста или содержит только изображения]");
                        }
                        
                        result.AppendLine();
                        result.AppendLine();
                    }
                    
                    // Убираем системную информацию о прочтении
                    
                    return result.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка при чтении PDF файла: {ex.Message}\n\n" +
                       "Возможные причины:\n" +
                       "• Файл повреждён или защищён паролем\n" +
                       "• Неподдерживаемый формат PDF\n" +
                       "• Недостаточно прав для чтения файла\n" +
                       "• Файл зашифрован или имеет ограничения\n\n" +
                       "Попробуйте:\n" +
                       "• Проверить целостность файла\n" +
                       "• Открыть файл в другом PDF-ридере\n" +
                       "• Использовать другой PDF файл\n" +
                       "• Убрать защиту с PDF файла";
            }
        }
        
        /// Читает Word файл (базовая поддержка)

        private string ReadWordFile(string filePath)
        {
            try
            {
                string wordContent = File.ReadAllText(filePath, Encoding.UTF8);
                return FormatTextContent(wordContent);
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка при чтении Word файла: {ex.Message}\n\n" +
                       "Попробуйте проверить целостность файла.";
            }
        }
        
        /// Читает FictionBook (.fb2) файл

        private string ReadFictionBookFile(string filePath)
        {
            try
            {
                // Читаем XML содержимое
                string xmlContent = File.ReadAllText(filePath, Encoding.UTF8);
                currentXmlContent = xmlContent; // Сохраняем для отладки
                return ParseFictionBookXml(xmlContent);
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка при чтении FictionBook файла: {ex.Message}\n\n" +
                       "Попробуйте проверить целостность файла.";
            }
        }
        
        /// Читает XML файл

        private string ReadXmlFile(string filePath)
        {
            try
            {
                // Читаем XML содержимое
                string xmlContent = File.ReadAllText(filePath, Encoding.UTF8);
                currentXmlContent = xmlContent; // Сохраняем для отладки
                
                // Проверяем, является ли это FictionBook
                if (xmlContent.Contains("<FictionBook") || xmlContent.Contains("fictionbook"))
                {
                    return ParseFictionBookXml(xmlContent);
                }
                else
                {
                    return ParseGenericXml(xmlContent);
                }
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка при чтении XML файла: {ex.Message}\n\n" +
                       "Попробуйте проверить целостность файла.";
            }
        }
        
        /// Парсит FictionBook XML и форматирует для чтения

        private string ParseFictionBookXml(string xmlContent)
        {
            try
            {
                // Создаём XML документ
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(xmlContent);
                
                // Извлекаем метаданные (пробуем разные способы)
                string title = ExtractXmlValue(xmlDoc, "//title-info/book-title") ?? 
                              ExtractXmlValue(xmlDoc, "//book-title") ??
                              ExtractXmlValue(xmlDoc, "book-title");
                              
                string authorFirstName = ExtractXmlValue(xmlDoc, "//title-info/author/first-name") ?? 
                                       ExtractXmlValue(xmlDoc, "//author/first-name") ??
                                       ExtractXmlValue(xmlDoc, "first-name");
                                       
                string authorLastName = ExtractXmlValue(xmlDoc, "//title-info/author/last-name") ?? 
                                      ExtractXmlValue(xmlDoc, "//author/last-name") ??
                                      ExtractXmlValue(xmlDoc, "last-name");
                                      
                string genre = ExtractXmlValue(xmlDoc, "//title-info/genre") ?? 
                              ExtractXmlValue(xmlDoc, "//genre") ??
                              ExtractXmlValue(xmlDoc, "genre");
                              
                string annotation = ExtractXmlValue(xmlDoc, "//title-info/annotation") ?? 
                                   ExtractXmlValue(xmlDoc, "//annotation") ??
                                   ExtractXmlValue(xmlDoc, "annotation");
                                   
                string language = ExtractXmlValue(xmlDoc, "//lang") ?? 
                                 ExtractXmlValue(xmlDoc, "lang");
                                 
                string date = ExtractXmlValue(xmlDoc, "//date") ?? 
                             ExtractXmlValue(xmlDoc, "date");
                
                // Формируем заголовок
                string result = $"📚 {title}\n\n";
                
                // Автор
                if (!string.IsNullOrEmpty(authorFirstName) || !string.IsNullOrEmpty(authorLastName))
                {
                    string author = $"{authorFirstName} {authorLastName}".Trim();
                    if (!string.IsNullOrEmpty(author))
                    {
                        result += $"✍️ Автор: {author}\n";
                    }
                }
                
                // Жанр
                if (!string.IsNullOrEmpty(genre))
                {
                    result += $"🏷️ Жанр: {genre}\n";
                }
                
                // Язык
                if (!string.IsNullOrEmpty(language))
                {
                    result += $"🌐 Язык: {language}\n";
                }
                
                // Дата
                if (!string.IsNullOrEmpty(date))
                {
                    result += $"📅 Дата: {date}\n";
                }
                
                result += "\n";
                
                // Аннотация
                if (!string.IsNullOrEmpty(annotation))
                {
                    result += $"📖 АННОТАЦИЯ:\n{FormatAnnotation(annotation)}\n\n";
                }
                
                // Извлекаем основной текст (пробуем разные способы)
                var bodyNodes = xmlDoc.SelectNodes("//body") ?? 
                               xmlDoc.SelectNodes("body") ??
                               xmlDoc.GetElementsByTagName("body");
                               
                if (bodyNodes != null && bodyNodes.Count > 0)
                {
                    result += "📖 СОДЕРЖАНИЕ:\n\n";
                    
                    foreach (System.Xml.XmlNode bodyNode in bodyNodes)
                    {
                        result += ParseBodyContent(bodyNode);
                    }
                }
                else
                {
                    // Попробуем найти текст другими способами
                    result += "📖 СОДЕРЖАНИЕ:\n\n";
                    
                    // Ищем все параграфы
                    var paragraphs = xmlDoc.SelectNodes("//p") ?? xmlDoc.GetElementsByTagName("p");
                    if (paragraphs != null && paragraphs.Count > 0)
                    {
                        foreach (System.Xml.XmlNode pNode in paragraphs)
                        {
                            string? text = pNode.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(text))
                            {
                                result += $"{text}\n";
                            }
                        }
                    }
                    else
                    {
                        // Ищем любой текстовый контент
                        var textNodes = xmlDoc.SelectNodes("//text()");
                        if (textNodes != null && textNodes.Count > 0)
                        {
                            foreach (System.Xml.XmlNode textNode in textNodes)
                            {
                                string? text = textNode.Value?.Trim();
                                if (!string.IsNullOrEmpty(text) && text.Length > 10)
                                {
                                    result += $"{text}\n";
                                }
                            }
                        }
                        else
                        {
                            result += "Основной текст не найден в файле.\n" +
                                      "Возможно, файл повреждён или имеет нестандартную структуру.\n\n" +
                                      "Попробуйте открыть файл в текстовом редакторе для проверки.\n\n" +
                                      "📋 СТРУКТУРА XML:\n" +
                                      "Для диагностики показана структура файла:\n\n" +
                                      FormatXmlStructure(xmlDoc.DocumentElement, 0);
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка при парсинге FictionBook XML: {ex.Message}\n\n" +
                       "Файл может быть повреждён или иметь нестандартную структуру.\n\n" +
                       "Попробуйте:\n" +
                       "• Проверить целостность файла\n" +
                       "• Открыть в текстовом редакторе\n" +
                       "• Использовать другой .fb2 файл";
            }
        }
        
        /// Парсит обычный XML файл

        private string ParseGenericXml(string xmlContent)
        {
            try
            {
                // Создаём XML документ
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(xmlContent);
                
                string result = "📄 XML ФАЙЛ\n\n";
                
                // Извлекаем корневой элемент
                string rootElement = xmlDoc.DocumentElement?.Name ?? "Неизвестно";
                result += $"🏷️ Тип: {rootElement}\n\n";
                
                // Показываем структуру XML
                result += "📋 СТРУКТУРА XML:\n\n";
                result += FormatXmlStructure(xmlDoc.DocumentElement, 0);
                
                return result;
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка при парсинге XML: {ex.Message}\n\n" +
                       "Файл может быть повреждён или иметь нестандартную структуру.";
            }
        }
        
        /// Извлекает значение из XML по XPath

        private string ExtractXmlValue(System.Xml.XmlDocument xmlDoc, string xpath)
        {
            try
            {
                // Пробуем XPath
                var node = xmlDoc.SelectSingleNode(xpath);
                if (node != null)
                {
                    return node.InnerText?.Trim() ?? "";
                }
                
                // Если XPath не сработал, пробуем найти по имени тега
                if (!xpath.StartsWith("//"))
                {
                    var nodes = xmlDoc.GetElementsByTagName(xpath);
                    if (nodes.Count > 0)
                    {
                        return nodes[0].InnerText?.Trim() ?? "";
                    }
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }
        
        /// Форматирует аннотацию для чтения

        private string FormatAnnotation(string annotation)
        {
            if (string.IsNullOrEmpty(annotation))
                return "";
            
            // Убираем лишние пробелы и переносы строк
            annotation = annotation.Replace("\r\n", "\n").Replace("\r", "\n");
            annotation = System.Text.RegularExpressions.Regex.Replace(annotation, @"\s+", " ");
            
            // Разбиваем на параграфы
            var paragraphs = annotation.Split(new[] { "<p>", "</p>" }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            
            foreach (var paragraph in paragraphs)
            {
                var cleanParagraph = paragraph.Trim();
                if (!string.IsNullOrEmpty(cleanParagraph))
                {
                    result.Add(cleanParagraph);
                }
            }
            
            return string.Join("\n\n", result);
        }
        
        /// Парсит содержимое body элемента FictionBook

        private string ParseBodyContent(System.Xml.XmlNode bodyNode)
        {
            string result = "";
            
            // Обрабатываем все дочерние элементы
            foreach (System.Xml.XmlNode childNode in bodyNode.ChildNodes)
            {
                switch (childNode.Name.ToLower())
                {
                    case "title":
                        result += $"\n📖 {childNode.InnerText.Trim()}\n\n";
                        break;
                    case "epigraph":
                        result += $"💭 ЭПИГРАФ:\n{childNode.InnerText.Trim()}\n\n";
                        break;
                    case "section":
                        result += ParseSection(childNode);
                        break;
                    case "p":
                        string paragraphText = childNode.InnerText.Trim();
                        // Форматируем абзац для правильного отображения
                        paragraphText = FormatParagraphText(paragraphText);
                        result += $"{paragraphText}\n\n";
                        break;
                    default:
                        if (!string.IsNullOrEmpty(childNode.InnerText?.Trim()))
                        {
                            string defaultText = childNode.InnerText.Trim();
                            defaultText = FormatParagraphText(defaultText);
                            result += $"{defaultText}\n\n";
                        }
                        break;
                }
            }
            
            return result;
        }
        
        /// Парсит section элемент FictionBook

        private string ParseSection(System.Xml.XmlNode sectionNode)
        {
            string result = "";
            
            // Заголовок секции
            var titleNode = sectionNode.SelectSingleNode("title");
            if (titleNode != null)
            {
                result += $"📖 {titleNode.InnerText.Trim()}\n\n";
            }
            
            // Содержимое секции
            foreach (System.Xml.XmlNode childNode in sectionNode.ChildNodes)
            {
                switch (childNode.Name.ToLower())
                {
                    case "title":
                        // Уже обработали выше
                        break;
                    case "p":
                        string paragraphText = childNode.InnerText.Trim();
                        paragraphText = FormatParagraphText(paragraphText);
                        result += $"{paragraphText}\n\n";
                        break;
                    case "section":
                        result += ParseSection(childNode);
                        break;
                    case "epigraph":
                        result += $"💭 ЭПИГРАФ:\n{childNode.InnerText.Trim()}\n\n";
                        break;
                    default:
                        if (!string.IsNullOrEmpty(childNode.InnerText?.Trim()))
                        {
                            result += $"{childNode.InnerText.Trim()}\n\n";
                        }
                        break;
                }
            }
            
            return result;
        }
        
        /// Форматирует структуру XML для отображения

        private string FormatXmlStructure(System.Xml.XmlNode node, int depth)
        {
            if (node == null) return "";
            
            string indent = new string(' ', depth * 2);
            string result = $"{indent}• {node.Name}";
            
            // Показываем атрибуты
            if (node.Attributes != null && node.Attributes.Count > 0)
            {
                var attributes = new List<string>();
                foreach (System.Xml.XmlAttribute attr in node.Attributes)
                {
                    attributes.Add($"{attr.Name}=\"{attr.Value}\"");
                }
                result += $" [{string.Join(", ", attributes)}]";
            }
            
            // Показываем значение, если это текстовый узел
            if (!string.IsNullOrEmpty(node.InnerText?.Trim()) && node.ChildNodes.Count == 1 && node.FirstChild.NodeType == System.Xml.XmlNodeType.Text)
            {
                string text = node.InnerText.Trim();
                if (text.Length > 50)
                {
                    text = text.Substring(0, 50) + "...";
                }
                result += $" = \"{text}\"";
            }
            
            result += "\n";
            
            // Рекурсивно обрабатываем дочерние элементы
            foreach (System.Xml.XmlNode childNode in node.ChildNodes)
            {
                if (childNode.NodeType == System.Xml.XmlNodeType.Element)
                {
                    result += FormatXmlStructure(childNode, depth + 1);
                }
            }
            
            return result;
        }
        
        /// Проверяет, является ли содержимое текстовым

        private bool IsTextContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return false;
            
            // Проверяем первые 1000 символов на наличие текстового содержимого
            string sample = content.Length > 1000 ? content.Substring(0, 1000) : content;
            
            // Считаем печатные символы
            int printableChars = sample.Count(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c));
            double ratio = (double)printableChars / sample.Length;
            
            return ratio > 0.7; // Если больше 70% символов - печатные, считаем текстом
        }
        
        /// Форматирует текстовое содержимое для чтения

        private string FormatTextContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
            
            // Убираем лишние пробелы и переносы строк
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // Исправляем слипание предложений - добавляем пробелы после точек, восклицательных и вопросительных знаков
            content = System.Text.RegularExpressions.Regex.Replace(content, @"([.!?])([А-ЯЁA-Z])", "$1 $2");
            
            // Исправляем слипание после запятых, точек с запятой, двоеточий
            content = System.Text.RegularExpressions.Regex.Replace(content, @"([,;:])([А-ЯЁA-Zа-яёa-z])", "$1 $2");
            
            // Убираем множественные пробелы
            while (content.Contains("  "))
            {
                content = content.Replace("  ", " ");
            }
            
            // Убираем множественные переносы строк
            while (content.Contains("\n\n\n"))
            {
                content = content.Replace("\n\n\n", "\n\n");
            }
            
            // Ограничиваем длину (чтобы не перегружать интерфейс)
            const int maxLength = 50000;
            if (content.Length > maxLength)
            {
                content = content.Substring(0, maxLength) + "\n\n... [Файл обрезан для удобства чтения] ...";
            }
            
            return content;
        }

        /// Форматирует размер файла в читаемый вид

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// Извлекает базовую информацию из PDF файла

        private string ExtractBasicPdfInfo(string filePath)
        {
            try
            {
                // Читаем первые несколько килобайт файла для поиска метаданных
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[Math.Min(8192, (int)fileStream.Length)];
                    int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                    string content = Encoding.UTF8.GetString(buffer);
                    
                    var result = new StringBuilder();
                    
                    // Ищем базовые метаданные в PDF
                    if (content.Contains("/Title"))
                    {
                        var titleMatch = System.Text.RegularExpressions.Regex.Match(content, @"/Title\s*\(([^)]+)\)");
                        if (titleMatch.Success)
                        {
                            result.AppendLine($"📚 Название: {titleMatch.Groups[1].Value}");
                        }
                    }
                    
                    if (content.Contains("/Author"))
                    {
                        var authorMatch = System.Text.RegularExpressions.Regex.Match(content, @"/Author\s*\(([^)]+)\)");
                        if (authorMatch.Success)
                        {
                            result.AppendLine($"✍️ Автор: {authorMatch.Groups[1].Value}");
                        }
                    }
                    
                    if (content.Contains("/Subject"))
                    {
                        var subjectMatch = System.Text.RegularExpressions.Regex.Match(content, @"/Subject\s*\(([^)]+)\)");
                        if (subjectMatch.Success)
                        {
                            result.AppendLine($"📝 Тема: {subjectMatch.Groups[1].Value}");
                        }
                    }
                    
                    if (content.Contains("/Creator"))
                    {
                        var creatorMatch = System.Text.RegularExpressions.Regex.Match(content, @"/Creator\s*\(([^)]+)\)");
                        if (creatorMatch.Success)
                        {
                            result.AppendLine($"🛠️ Создано в: {creatorMatch.Groups[1].Value}");
                        }
                    }
                    
                    if (content.Contains("/Producer"))
                    {
                        var producerMatch = System.Text.RegularExpressions.Regex.Match(content, @"/Producer\s*\(([^)]+)\)");
                        if (producerMatch.Success)
                        {
                            result.AppendLine($"⚙️ Обработано: {producerMatch.Groups[1].Value}");
                        }
                    }
                    
                    // Ищем количество страниц
                    var pageCountMatch = System.Text.RegularExpressions.Regex.Match(content, @"/Count\s+(\d+)");
                    if (pageCountMatch.Success)
                    {
                        result.AppendLine($"📊 Количество страниц: {pageCountMatch.Groups[1].Value}");
                    }
                    
                    return result.ToString();
                }
            }
            catch
            {
                return "";
            }
        }
        
        /// Очищает и форматирует текст из PDF

        private string CleanPdfText(string pdfText)
        {
            if (string.IsNullOrEmpty(pdfText))
                return pdfText;
            
            // Убираем лишние пробелы и переносы строк
            pdfText = pdfText.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // Исправляем слипание предложений - добавляем пробелы после точек, восклицательных и вопросительных знаков
            pdfText = System.Text.RegularExpressions.Regex.Replace(pdfText, @"([.!?])([А-ЯЁA-Z])", "$1 $2");
            
            // Исправляем слипание после запятых, точек с запятой, двоеточий
            pdfText = System.Text.RegularExpressions.Regex.Replace(pdfText, @"([,;:])([А-ЯЁA-Zа-яёa-z])", "$1 $2");
            
            // Убираем множественные пробелы
            while (pdfText.Contains("  "))
            {
                pdfText = pdfText.Replace("  ", " ");
            }
            
            // Убираем множественные переносы строк
            while (pdfText.Contains("\n\n\n"))
            {
                pdfText = pdfText.Replace("\n\n\n", "\n\n");
            }
            
            // Убираем лишние пробелы в начале и конце строк
            var lines = pdfText.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
            }
            pdfText = string.Join("\n", lines);
            
            // Убираем пустые строки в начале и конце
            pdfText = pdfText.Trim();
            
            return pdfText;
        }
        
        /// Форматирует текст абзаца для правильного отображения

        private string FormatParagraphText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            // Исправляем слипание предложений - добавляем пробелы после точек, восклицательных и вопросительных знаков
            text = System.Text.RegularExpressions.Regex.Replace(text, @"([.!?])([А-ЯЁA-Z])", "$1 $2");
            
            // Исправляем слипание после запятых, точек с запятой, двоеточий
            text = System.Text.RegularExpressions.Regex.Replace(text, @"([,;:])([А-ЯЁA-Zа-яёa-z])", "$1 $2");
            
            // Убираем множественные пробелы
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }
            
            return text.Trim();
        }

        /// Очищает RTF содержимое от разметки

        private string CleanRtfContent(string rtfContent)
        {
            if (string.IsNullOrEmpty(rtfContent))
                return rtfContent;
            
            // Простая очистка RTF разметки
            string cleaned = rtfContent;
            
            // Убираем RTF заголовки
            if (cleaned.StartsWith("{\\rtf"))
            {
                int startIndex = cleaned.IndexOf("\\viewkind");
                if (startIndex > 0)
                {
                    cleaned = cleaned.Substring(startIndex);
                }
            }
            
            // Убираем основные RTF команды
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\\[a-z]+\d*", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\{[^}]*\}", "");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\\'[0-9a-fA-F]{2}", "");
            
            // Исправляем слипание предложений - добавляем пробелы после точек, восклицательных и вопросительных знаков
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"([.!?])([А-ЯЁA-Z])", "$1 $2");
            
            // Исправляем слипание после запятых, точек с запятой, двоеточий
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"([,;:])([А-ЯЁA-Zа-яёa-z])", "$1 $2");
            
            // Убираем лишние пробелы
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");
            
            return cleaned.Trim();
        }

        /// Показывает содержимое с ошибкой

        private void ShowErrorContent(Book book, string errorMessage = null)
        {
            string errorText = $"📚 {book.Title}\n\n" +
                              $"✍️ Автор: {book.Author}\n" +
                              $"📁 Файл: {book.FileName}\n" +
                              $"📅 Дата добавления: {book.AddedDate:dd.MM.yyyy}\n\n";
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                errorText += $"❌ Ошибка при чтении файла:\n{errorMessage}\n\n";
            }
            else
            {
                errorText += $"❌ Не удалось прочитать содержимое файла.\n\n";
            }
            
            errorText += $"🔧 Возможные причины:\n" +
                        $"• Файл повреждён или недоступен\n" +
                        $"• Неподдерживаемый формат файла\n" +
                        $"• Недостаточно прав для чтения\n\n" +
                        $"💡 Попробуйте:\n" +
                        $"• Проверить, что файл существует\n" +
                        $"• Использовать текстовые файлы (.txt)\n" +
                        $"• Перезапустить приложение";
            
            BookContentText.Text = errorText;
            StatusText.Text = "Ошибка при чтении файла";
        }

        /// Обновляет прогресс чтения

        private void UpdateReadingProgress(double percentage)
        {
            ReadingProgressBar.Value = percentage;
            ProgressText.Text = $"Прогресс чтения: {percentage:F0}%";
        }

        /// Находит ScrollViewer для указанного элемента

        private ScrollViewer FindScrollViewer(DependencyObject element)
        {
            if (element == null) return null;
            
            // Проверяем текущий элемент
            if (element is ScrollViewer scrollViewer)
                return scrollViewer;
            
            // Рекурсивно ищем в родительских элементах
            DependencyObject parent = VisualTreeHelper.GetParent(element);
            return FindScrollViewer(parent);
        }

        /// Обработчик изменения прокрутки для отслеживания прогресса

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Вычисляем прогресс чтения на основе позиции прокрутки
                double progress = 0;
                
                if (scrollViewer.ExtentHeight > 0)
                {
                    progress = (scrollViewer.VerticalOffset / (scrollViewer.ExtentHeight - scrollViewer.ViewportHeight)) * 100;
                    progress = Math.Max(0, Math.Min(100, progress)); // Ограничиваем от 0 до 100
                }
                
                UpdateReadingProgress(progress);
                StatusText.Text = $"Прогресс чтения: {progress:F0}%";
            }
        }

        /// Возвращает к главной панели библиотеки

        private void BackToLibrary_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем панель чтения
            ReadingPanel.Visibility = Visibility.Collapsed;
            
            // Показываем приветственную панель
            WelcomePanel.Visibility = Visibility.Visible;
            
            // Скрываем кнопку возврата
            BackToLibraryButton.Visibility = Visibility.Collapsed;
            
            // Сбрасываем состояние страниц
            bookPages.Clear();
            currentPageIndex = 0;
            currentBook = null;
            currentXmlContent = "";
        }

        /// Увеличивает размер шрифта

        private void FontSizeUp_Click(object sender, RoutedEventArgs e)
        {
            double currentSize = BookContentText.FontSize;
            if (currentSize < 32)
            {
                BookContentText.FontSize = currentSize + 2;
                StatusText.Text = $"Размер шрифта: {BookContentText.FontSize}";
            }
        }

        /// Уменьшает размер шрифта

        private void FontSizeDown_Click(object sender, RoutedEventArgs e)
        {
            double currentSize = BookContentText.FontSize;
            if (currentSize > 12)
            {
                BookContentText.FontSize = currentSize - 2;
                StatusText.Text = $"Размер шрифта: {BookContentText.FontSize}";
            }
        }

        /// Обработчик нажатий клавиш для навигации по страницам

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (ReadingPanel.Visibility == Visibility.Visible && bookPages.Count > 0)
            {
                switch (e.Key)
                {
                    case Key.Left:
                    case Key.PageUp:
                    case Key.Up:
                        GoToPreviousPage();
                        e.Handled = true;
                        break;
                    case Key.Right:
                    case Key.PageDown:
                    case Key.Space:
                    case Key.Down:
                        GoToNextPage();
                        e.Handled = true;
                        break;
                    case Key.Home:
                        GoToFirstPage();
                        e.Handled = true;
                        break;
                    case Key.End:
                        GoToLastPage();
                        e.Handled = true;
                        break;
                }
            }
        }

        /// Переход на предыдущую страницу

        private void GoToPreviousPage()
        {
            if (currentPageIndex > 0)
            {
                currentPageIndex--;
                ShowCurrentPage();
                UpdateReadingProgressFromPage();
            }
        }

        /// Переход на следующую страницу

        private void GoToNextPage()
        {
            if (currentPageIndex < bookPages.Count - 1)
            {
                currentPageIndex++;
                ShowCurrentPage();
                UpdateReadingProgressFromPage();
            }
        }

        /// Переход на первую страницу

        private void GoToFirstPage()
        {
            currentPageIndex = 0;
            ShowCurrentPage();
            UpdateReadingProgressFromPage();
        }

        /// Переход на последнюю страницу

        private void GoToLastPage()
        {
            currentPageIndex = bookPages.Count - 1;
            ShowCurrentPage();
            UpdateReadingProgressFromPage();
        }

        /// Показывает текущую страницу

        private void ShowCurrentPage()
        {
            if (bookPages.Count > 0 && currentPageIndex >= 0 && currentPageIndex < bookPages.Count)
            {
                BookContentText.Text = bookPages[currentPageIndex];
                UpdatePageInfo();
                UpdateReadingProgressFromPage();
                StatusText.Text = $"Страница {currentPageIndex + 1} из {bookPages.Count}";
            }
        }

        /// Обновляет информацию о текущей странице
 
        private void UpdatePageInfo()
        {
            if (bookPages.Count > 0)
            {
                PageText.Text = $"Страница {currentPageIndex + 1} из {bookPages.Count}";
                
                // Обновляем состояние кнопок навигации
                PreviousPageButton.IsEnabled = currentPageIndex > 0;
                NextPageButton.IsEnabled = currentPageIndex < bookPages.Count - 1;
            }
        }

        /// Загружает сохраненный прогресс чтения для книги

        private void LoadBookProgress(Book book)
        {
            if (readingProgress.ContainsKey(book.FilePath))
            {
                var progress = readingProgress[book.FilePath];
                
                // Проверяем, что количество страниц совпадает
                if (progress.TotalPages == bookPages.Count)
                {
                    currentPageIndex = Math.Min(progress.CurrentPage, bookPages.Count - 1);
                    ShowCurrentPage();
                    
                    // Показываем уведомление о восстановлении прогресса
                    StatusText.Text = $"Прогресс восстановлен: страница {currentPageIndex + 1} из {bookPages.Count}";
                }
                else
                {
                    // Если количество страниц изменилось, начинаем сначала
                    currentPageIndex = 0;
                    ShowCurrentPage();
                    StatusText.Text = "Книга изменена, начинаем сначала";
                }
            }
            else
            {
                // Если прогресс не найден, начинаем сначала
                currentPageIndex = 0;
                ShowCurrentPage();
                StatusText.Text = "Начинаем чтение с начала";
            }
        }

        /// Сохраняет текущий прогресс чтения

        private void SaveCurrentProgress()
        {
            if (currentBook != null && bookPages.Count > 0)
            {
                double percentage = ((double)(currentPageIndex + 1) / bookPages.Count) * 100;
                var progress = new ReadingProgress(currentBook.FilePath, currentPageIndex, percentage, bookPages.Count);
                readingProgress[currentBook.FilePath] = progress;
                SaveReadingProgress();
            }
        }

        /// Обновляет прогресс чтения на основе текущей страницы

        private void UpdateReadingProgressFromPage()
        {
            if (bookPages.Count > 0)
            {
                double percentage = ((double)(currentPageIndex + 1) / bookPages.Count) * 100;
                UpdateReadingProgress(percentage);
                
                // Сохраняем прогресс при каждом изменении
                SaveCurrentProgress();
            }
        }

        /// Возвращает путь к заглушке обложки в зависимости от типа файла
 
        private string GetCoverPlaceholder(string filePath)
        {
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            
                
            switch (extension)
            {
                case ".fb2":
                    return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\fb2.png");
                case ".txt":
                    return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\txt.png");
                case ".md":
                    return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\md.png");
                case ".rtf":
                    return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\rtf.png");
                case ".xml":
                    return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\xml.png");
                case ".pdf":
                    return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\pdf.png");
                default:
                    return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\unknown.png"); 
            }
        }

        /// Создаёт страницы из содержимого книги с адаптивным размером

        private void CreateBookPages(string content)
        {
            bookPages.Clear();
            
            // Получаем размеры экрана для адаптивного размера страниц
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            
            // Оптимальное количество символов на страницу
            int baseCharsPerPage = 1500; 
            
            // Адаптируем под размер экрана
            double scaleFactor = Math.Min(screenHeight / 1080.0, screenWidth / 1920.0);
            int charsPerPage = (int)(baseCharsPerPage * scaleFactor);
            
            // Ограничиваем размер страницы (минимум 1000, максимум 3000 символов)
            charsPerPage = Math.Max(1500, Math.Min(3000, charsPerPage));
            
            if (content.Length <= 500)
            {
                // Если содержимое помещается на одну страницу
                bookPages.Add(content);
            }
            else
            {
                // Разбиваем на страницы
                int startIndex = 0;
                while (startIndex < content.Length)
                {
                    int endIndex = Math.Min(startIndex + charsPerPage, content.Length);
                    
                    // Ищем хорошее место для разрыва страницы (конец абзаца)
                    if (endIndex < content.Length)
                    {
                        // Ищем ближайший конец абзаца (двойной перенос строки) в пределах 300 символов
                        int searchRange = Math.Min(300, endIndex - startIndex);
                        int breakIndex = content.LastIndexOf("\n\n", endIndex - 1, searchRange);
                        
                        // Если не нашли двойной перенос, ищем одинарный
                        if (breakIndex <= startIndex + charsPerPage / 4)
                        {
                            breakIndex = content.LastIndexOf('\n', endIndex - 1, searchRange);
                        }
                        
                        // Если не нашли перенос строки, ищем точку
                        if (breakIndex <= startIndex + charsPerPage / 4)
                        {
                            breakIndex = content.LastIndexOf('.', endIndex - 1, searchRange);
                        }
                        
                        // Если нашли хорошее место для разрыва
                        if (breakIndex > startIndex + charsPerPage / 4)
                        {
                            endIndex = breakIndex + 1;
                        }
                    }
                    
                    string pageContent = content.Substring(startIndex, endIndex - startIndex).Trim();
                    if (!string.IsNullOrEmpty(pageContent))
                    {
                        bookPages.Add(pageContent);
                    }
                    
                    startIndex = endIndex;
                }
            }
            
            // Обновляем информацию о страницах
            if (bookPages.Count > 0)
            {
                PageText.Text = $"Страница 1 из {bookPages.Count}";
            }
        }
        

        
        /// <summary>
        /// Показывает панель с гридом книг
        /// </summary>
        private void BooksButton_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем все панели
            WelcomePanel.Visibility = Visibility.Collapsed;
            ReadingPanel.Visibility = Visibility.Collapsed;
            BackToLibraryButton.Visibility = Visibility.Collapsed;
            // Показываем панель с гридом книг
            BooksGridPanel.Visibility = Visibility.Visible;

            // Обновляем отображение книг в гриде
            UpdateBooksDisplay();
        }
        
        /// <summary>
        /// Возврат к главной панели из грида книг
        /// </summary>
        private void BackToWelcome_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем панель с гридом
            BooksGridPanel.Visibility = Visibility.Collapsed;
            
            // Показываем главную панель
            WelcomePanel.Visibility = Visibility.Visible;
        }
        
        /// <summary>
        /// Обновляет отображение книг в гриде
        /// </summary>
        private void UpdateBooksGridDisplay()
        {
            // Обновляем прогресс для каждой книги
            UpdateBooksProgress();
            
            // Привязываем список книг к ItemsControl
            BooksItemsControl.ItemsSource = null;
            BooksItemsControl.ItemsSource = books;
        }
        
        /// <summary>
        /// Обновляет прогресс чтения для всех книг
        /// </summary>
        private void UpdateBooksProgress()
        {
            foreach (var book in books)
            {
                if (readingProgress.ContainsKey(book.FilePath))
                {
                    var progress = readingProgress[book.FilePath];
                    book.ProgressWidth = (progress.ProgressPercentage / 100.0) * 180; 
                    book.ProgressText = $"{progress.ProgressPercentage:F0}% ({progress.CurrentPage + 1}/{progress.TotalPages})";
                }
                else
                {
                    book.ProgressWidth = 0;
                    book.ProgressText = "Не читалось";
                }
            }
        }
        
        /// <summary>
        /// Чтение книги из грида
        /// </summary>
        private void ReadBookFromGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                ShowReadingPanel(book);
            }
        }
        
        /// <summary>
        /// Редактирование книги из грида
        /// </summary>
        private void EditBookFromGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                ShowEditBookDialog(book);
            }
        }
        
        /// <summary>
        /// Показывает диалог редактирования книги
        /// </summary>
        private void ShowEditBookDialog(Book book)
        {
            // Создаём окно редактирования
            var editWindow = new Window
            {
                Title = $"Редактирование книги: {book.Title}",
                Width = 700,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = this.Resources["WindowBackgroundBrush"] as SolidColorBrush,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.Margin = new Thickness(30);

            // Основная панель с полями
            var stackPanel = new StackPanel();
            
            // Название книги
            var titleLabel = new TextBlock
            {
                Text = "Название книги:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanel.Children.Add(titleLabel);

            var titleTextBox = new TextBox
            {
                Text = book.Title,
                FontSize = 16,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(0, 0, 0, 20),
                Height = 45
            };
            stackPanel.Children.Add(titleTextBox);

            // Автор
            var authorLabel = new TextBlock
            {
                Text = "Автор:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanel.Children.Add(authorLabel);

            var authorTextBox = new TextBox
            {
                Text = book.Author,
                FontSize = 16,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(0, 0, 0, 20),
                Height = 45
            };
            stackPanel.Children.Add(authorTextBox);

            // Путь к файлу (только для чтения)
            var fileLabel = new TextBlock
            {
                Text = "Файл:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanel.Children.Add(fileLabel);

            var fileTextBox = new TextBox
            {
                Text = book.FilePath,
                FontSize = 14,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(0, 0, 0, 20),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 60
            };
            stackPanel.Children.Add(fileTextBox);

            // Дата добавления (только для чтения)
            var dateLabel = new TextBlock
            {
                Text = "Дата добавления:",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanel.Children.Add(dateLabel);

            var dateTextBox = new TextBox
            {
                Text = book.AddedDate.ToString("dd.MM.yyyy HH:mm"),
                FontSize = 16,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15, 12, 15, 12),
                Margin = new Thickness(0, 0, 0, 20),
                IsReadOnly = true,
                Height = 45
            };
            stackPanel.Children.Add(dateTextBox);

            Grid.SetRow(stackPanel, 0);
            grid.Children.Add(stackPanel);

            // Панель кнопок
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var saveButton = new Button
            {
                Content = "💾 Сохранить",
                Width = 150,
                Height = 45,
                Background = this.Resources["AccentBrush"] as SolidColorBrush ?? new SolidColorBrush(Colors.Blue),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Style = this.Resources["RoundedButtonStyle"] as Style,
                Margin = new Thickness(0, 0, 15, 0),
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            };

            var cancelButton = new Button
            {
                Content = "❌ Отмена",
                Width = 150,
                Height = 45,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Style = this.Resources["RoundedButtonStyle"] as Style,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            };

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            editWindow.Content = grid;

            // Обработчики кнопок
            saveButton.Click += (s, e) =>
            {
                // Сохраняем изменения
                book.Title = titleTextBox.Text.Trim();
                book.Author = authorTextBox.Text.Trim();
                
                // Проверяем, что название не пустое
                if (string.IsNullOrWhiteSpace(book.Title))
                {
                    MessageBox.Show("Название книги не может быть пустым!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохраняем в JSON
                SaveBooksToJson();
                
                // Обновляем отображение
                UpdateBooksDisplay();
                UpdateBooksGridDisplay();
                
                editWindow.Close();
                
                MessageBox.Show($"Книга '{book.Title}' успешно отредактирована!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            };

            cancelButton.Click += (s, e) =>
            {
                editWindow.Close();
            };

            // Показываем окно
            editWindow.ShowDialog();
        }
        
        /// <summary>
        /// Удаление книги из грида
        /// </summary>
        private void DeleteBookFromGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите удалить книгу '{book.Title}'?", 
                                           "Подтверждение удаления", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    books.Remove(book);
                    SaveBooksToJson();
                    UpdateBooksDisplay();
                    UpdateBooksGridDisplay();
                }
            }
        }
        
        /// <summary>
        /// Сброс прогресса чтения из грида
        /// </summary>
        private void ResetProgressFromGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите сбросить прогресс чтения книги '{book.Title}'?", 
                                           "Подтверждение сброса", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    if (readingProgress.ContainsKey(book.FilePath))
                    {
                        readingProgress.Remove(book.FilePath);
                        SaveReadingProgress();
                        UpdateBooksDisplay();
                        UpdateBooksGridDisplay();

                        MessageBox.Show($"Прогресс чтения книги '{book.Title}' сброшен.", 
                                      "Прогресс сброшен", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Для книги '{book.Title}' нет сохранённого прогресса.", 
                                      "Нет прогресса", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                }
            }
        }
        
        /// <summary>
        /// Обработчик кнопки "Предыдущая страница"
        /// </summary>
        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            GoToPreviousPage();
        }
        
        /// <summary>
        /// Обработчик кнопки "Следующая страница"
        /// </summary>
        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            GoToNextPage();
        }
        
        /// <summary>
        /// Показывает исходный XML для отладки
        /// </summary>
        private void ShowXml_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentXmlContent))
            {
                // Создаём окно для показа XML
                var xmlWindow = new Window
                {
                    Title = "Исходный XML - Отладка",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = this.Resources["WindowBackgroundBrush"] as SolidColorBrush
                };

                var textBox = new TextBox
                {
                    Text = currentXmlContent,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                    Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                    BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                    BorderThickness = new Thickness(1),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    TextWrapping = TextWrapping.NoWrap,
                    IsReadOnly = true
                };

                xmlWindow.Content = textBox;
                xmlWindow.Show();
                
                StatusText.Text = "Открыто окно отладки XML";
            }
            else
            {
                MessageBox.Show("XML содержимое недоступно для отладки.", 
                              "Отладка", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
            }
        }

        private void Clear_readingProgress()
        {
            readingProgress.Clear();
        }


        /// <summary>
        /// Сброс прогресса чтения для всех книг
        /// </summary>
        private void ResetAllProgress_Click(object sender, RoutedEventArgs e)
        {
            if (readingProgress.Count > 0)
            {
                var result = MessageBox.Show($"Вы уверены, что хотите сбросить прогресс чтения для всех книг ({readingProgress.Count} книг)?", 
                                           "Подтверждение сброса", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    readingProgress.Clear();
                    SaveReadingProgress();
                    UpdateBooksDisplay();
                    UpdateBooksGridDisplay();
                    Clear_readingProgress();


                    MessageBox.Show($"Прогресс чтения для всех книг сброшен.", 
                                  "Прогресс сброшен", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Нет сохранённого прогресса чтения для сброса.", 
                              "Нет прогресса", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
            }
        }

        private void AutorisationButton_Click(object sender, RoutedEventArgs e)
        {
            LoginForm.Visibility = Visibility.Visible;
            RegisterForm.Visibility = Visibility.Collapsed;
            AutorisationButton.Style = this.Resources["ActiveToggleButtonStyle"] as Style;
            RegistrationButton.Style = this.Resources["ToggleButtonStyle"] as Style;
        }

        private void RegistrationButton_Click(object sender, RoutedEventArgs e)
        {
            LoginForm.Visibility = Visibility.Collapsed;
            RegisterForm.Visibility = Visibility.Visible;
            RegistrationButton.Style = this.Resources["ActiveToggleButtonStyle"] as Style;
            AutorisationButton.Style = this.Resources["ToggleButtonStyle"] as Style;
        }
        private void LoginSubmit_Click(object sender, RoutedEventArgs e)
        {
            isLogin = true;
        }
        private void RegisterSubmit_Click(object sender, RoutedEventArgs e)
        {
            isLogin = true;

        }
        private void GuestLogin_Click(object sender, RoutedEventArgs e)
        {
            BooksButton.Visibility = Visibility.Visible;
            NavigationButtons.Visibility = Visibility.Visible;
            // Скрываем все панели
            AutorisationPanel.Visibility = Visibility.Collapsed;
            BackToLibraryButton.Visibility = Visibility.Collapsed;
            BooksGridPanel.Visibility = Visibility.Collapsed;
            ReadingPanel.Visibility = Visibility.Collapsed;

            // Показываем главную панель
            WelcomePanel.Visibility = Visibility.Visible;
            UpdateBooksDisplay();
            isLogin = true;

        }
    }
}