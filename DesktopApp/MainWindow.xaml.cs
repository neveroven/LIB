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
using System.Data;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;

namespace LIB
{
    /// Класс для хранения прогресса чтения
    public class ReadingProgress
    {
        public int Id { get; set; }
        public int BookFileId { get; set; }
        public int UserId { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public double ProgressPercent { get; set; }
        public DateTime LastReadAt { get; set; }

        public ReadingProgress()
        {
            LastReadAt = DateTime.Now;
        }

        public ReadingProgress(int bookFileId, int userId, int currentPage, int totalPages)
        {
            BookFileId = bookFileId;
            UserId = userId;
            CurrentPage = currentPage;
            TotalPages = totalPages;
            ProgressPercent = totalPages > 0 ? ((double)currentPage / totalPages) * 100 : 0;
            LastReadAt = DateTime.Now;
        }
    }

    /// Класс для работы с файлами книг в БД
    public class BookFile
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string Format { get; set; }
        public string SourceType { get; set; }
        public string LocalPath { get; set; }
        public string ServerUri { get; set; }
        public string FileName { get; set; }
        public string CoverImageUri { get; set; }

        public BookFile()
        {
            SourceType = "local";
        }
    }

    /// Класс для работы с книгами в БД
    public class BookDB
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int PublishedYear { get; set; }
        public string Description { get; set; }
        public List<BookFile> Files { get; set; }

        public BookDB()
        {
            Files = new List<BookFile>();
        }
    }

    /// Класс записи пользовательских книг
    public class UserBook
    {
        public int UserId { get; set; }
        public int BookId { get; set; }
        public string Status { get; set; }

        public UserBook()
        {
            Status = "planned";
        }
    }

    /// Класс для представления книги (совместимость с UI)
    public class Book
    {
        public int LocalBookID { get; set; }
        public int BookId { get; set; } // ID из БД
        public int BookFileId { get; set; } // ID файла из БД
        public string Title { get; set; }
        public string Author { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime AddedDate { get; set; }
        public string CoverImageSource { get; set; } 
        public double ProgressWidth { get; set; } 
        public string ProgressText { get; set; }
        public int PublishedYear { get; set; }
        public string Description { get; set; } 

        public Book()
        {
            LocalBookID = 0;
            BookId = 0;
            BookFileId = 0;
            Title = "";
            Author = "Не указан";
            FilePath = "";
            FileName = "";
            AddedDate = DateTime.Now;
            CoverImageSource = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\unknown.png"); 
            ProgressWidth = 0;
            ProgressText = "";
            PublishedYear = 0;
            Description = "";
        }

        public Book(int bookid, string title, string author, string filePath, string fileName)
        {
            LocalBookID = bookid;
            BookId = 0;
            BookFileId = 0;
            Title = title;
            Author = author;
            FilePath = filePath;
            FileName = fileName;
            AddedDate = DateTime.Now;
            CoverImageSource = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\unknown.png"); 
            ProgressWidth = 0;
            ProgressText = "";
            PublishedYear = 0;
            Description = "";
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
        private readonly string conectionString = "server=localhost;database=Paradise;uid=root;pwd=root;";
        private bool isDarkTheme = false;
        private bool isLogin = false;
        private List<Book> books = new List<Book>();
        private int num_index = -1;
        private int currentUserId = 0; // ID текущего пользователя (0 = неавторизован/гость)
        private bool isAdmin = false; // Флаг администратора
        private string currentXmlContent = "";
        private string dbFolderPath = ""; // Путь к папке DB для серверных книг
        private string currentAdminContentType = "";
        private class FormField
        {
            public string Key { get; set; } = "";
            public string Label { get; set; } = "";
            public string DefaultValue { get; set; } = "";
            // text, number, bool, datetime
            public string Type { get; set; } = "text";
        }
        private void index_found() //Костыль для LocalBookID
        {
            if (num_index == -1 || books.Count == 0)
                num_index = 0;
            else
                num_index = books[books.Count - 1].LocalBookID + 1;
        }

        // Система страниц
        private List<string> bookPages = new List<string>();
        private int currentPageIndex = 0;
        private Book? currentBook = null;


        // Прогресс чтения (теперь по BookFileId)
        private Dictionary<int, ReadingProgress> readingProgress = new Dictionary<int, ReadingProgress>();
        
        // Книги из каталога
        private List<Book> catalogBooks = new List<Book>();
        private List<Book> filteredCatalogBooks = new List<Book>();

        public MainWindow()
        {

            index_found();
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;

            // Загружаем настройки
            LoadSettings();

            // Инициализируем отображение книг
            UpdateBooksDisplay();
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
                HideAllPanels();
                WelcomePanel.Visibility = Visibility.Visible;
                UpdateBooksDisplay();
            }
        }

        private void AddBookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentUserId <= 0)
                {
                    MessageBox.Show("Чтобы добавлять книги, войдите в аккаунт.", "Требуется вход", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                index_found();
                // Создаём диалог выбора файла
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Выберите книгу для добавления";
                openFileDialog.Filter = "Все файлы (*.*)|*.*|Текстовые файлы (*.txt)|*.txt|FictionBook (*.fb2)|*.fb2|XML файлы (*.xml)|*.xml|RTF файлы (*.rtf)|*.rtf|Markdown (*.md)|*.md|PDF файлы (*.pdf)|*.pdf|EPUB файлы (*.epub)|*.epub|Word документы (*.doc;*.docx)|*.doc;*.docx";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = false;

                // Показываем диалог
                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    string fileName = System.IO.Path.GetFileName(filePath);

                    string title = System.IO.Path.GetFileNameWithoutExtension(filePath);

                    string author = "Не указан";

                    Book newBook = new Book(num_index, title, author, filePath, fileName);

                    newBook.CoverImageSource = GetCoverPlaceholder(filePath);

                    // Сохраняем книгу в БД
                    SaveBookToDatabase(newBook);

                    // Перезагружаем книги из БД
                    LoadBooksFromDatabase();

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

        /// Загружает книги из базы данных

        private void LoadBooksFromDatabase()
        {
            try
            {
                books.Clear();
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    // Загружаем книги с их файлами
                    using (var command = new MySqlCommand(
                        @"SELECT b.id, b.title, b.author, b.published_year, b.description,
                                 bf.id as file_id, bf.source_type, bf.local_path, bf.server_uri, bf.file_name, bf.cover_image_uri
                          FROM books b
                          INNER JOIN user_books ub ON ub.book_id = b.id AND ub.user_id = @user_id
                          LEFT JOIN book_files bf ON b.id = bf.book_id
                          ORDER BY b.id, bf.id", conn))
                    {
                        command.Parameters.AddWithValue("@user_id", currentUserId);
                        using (var reader = command.ExecuteReader())
                        {
                            var bookDict = new Dictionary<int, Book>();
                            
                            while (reader.Read())
                            {
                                int bookId = reader.GetInt32("id");
                                string title = reader.GetString("title");
                                string author = reader.IsDBNull(2) ? "Не указан" : reader.GetString(2);
                                
                                if (!bookDict.ContainsKey(bookId))
                                {
                                    var book = new Book
                                    {
                                        BookId = bookId,
                                        Title = title,
                                        Author = author,
                                        LocalBookID = bookId, // Используем ID из БД
                                        PublishedYear = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                        Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                        AddedDate = DateTime.Now
                                    };
                                    bookDict[bookId] = book;
                                }
                                
                                // Если есть файл, обновляем информацию о файле
                                if (!reader.IsDBNull(5))
                                {
                                    var book = bookDict[bookId];
                                    book.BookFileId = reader.GetInt32(5);
                                    string sourceType = reader.IsDBNull(6) ? "local" : reader.GetString(6);
                                    
                                    // Для серверных книг объединяем путь
                                    if (sourceType == "server")
                                    {
                                        string serverUri = reader.IsDBNull(8) ? "" : reader.GetString(8);
                                        book.FilePath = GetServerBookPath(serverUri);
                                    }
                                    else
                                    {
                                        book.FilePath = reader.GetString(7); // local_path
                                    }
                                    
                                    book.FileName = reader.GetString(9);
                                    
                                    // Обработка обложки из БД
                                    string coverImageUri = reader.IsDBNull(10) ? "" : reader.GetString(10);
                                    if (!string.IsNullOrEmpty(coverImageUri))
                                    {
                                        // Если это относительный путь, делаем его абсолютным
                                        if (!System.IO.Path.IsPathRooted(coverImageUri))
                                        {
                                            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                                            var libIndex = baseDir.IndexOf("LIB", StringComparison.OrdinalIgnoreCase);
                                            if (libIndex >= 0)
                                            {
                                                var projectRoot = baseDir.Substring(0, libIndex + 3);
                                                book.CoverImageSource = System.IO.Path.Combine(projectRoot, coverImageUri.Replace('/', '\\'));
                                            }
                                            else
                                            {
                                                string format = System.IO.Path.GetExtension(book.FilePath).ToLower().TrimStart('.');
                                                if (string.IsNullOrEmpty(format)) format = "unknown";
                                                book.CoverImageSource = GetCoverPlaceholder(format);
                                            }
                                        }
                                        else
                                        {
                                            book.CoverImageSource = coverImageUri;
                                        }
                                    }
                                    else
                                    {
                                        string format = System.IO.Path.GetExtension(book.FilePath).ToLower().TrimStart('.');
                                        if (string.IsNullOrEmpty(format)) format = "unknown";
                                        book.CoverImageSource = GetCoverPlaceholder(format);
                                    }
                                    
                                    // Проверяем существование обложки
                                    if (!string.IsNullOrEmpty(book.CoverImageSource) && !File.Exists(book.CoverImageSource))
                                    {
                                        string format = System.IO.Path.GetExtension(book.FilePath).ToLower().TrimStart('.');
                                        if (string.IsNullOrEmpty(format)) format = "unknown";
                                        book.CoverImageSource = GetCoverPlaceholder(format);
                                    }
                                }
                            }
                            
                            books = bookDict.Values.ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                books = new List<Book>();
                MessageBox.Show($"Ошибка при загрузке книг из БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Загружает доступные книги с сервера (source_type = 'server')
        private void LoadServerBooks()
        {
            try
            {
                catalogBooks.Clear();
                
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    // Загружаем книги с сервера, которые еще не добавлены пользователю
                    using (var command = new MySqlCommand(
                        @"SELECT DISTINCT b.id, b.title, b.author, b.published_year, b.description,
                                 bf.id as file_id, bf.source_type, bf.local_path, bf.server_uri, bf.file_name, 
                                 bf.cover_image_uri
                          FROM books b
                          INNER JOIN book_files bf ON b.id = bf.book_id
                          WHERE bf.source_type = 'server' 
                          AND b.id NOT IN (
                              SELECT book_id FROM user_books WHERE user_id = @user_id
                          )
                          ORDER BY b.id", conn))
                    {
                        command.Parameters.AddWithValue("@user_id", currentUserId > 0 ? currentUserId : 0);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int bookId = reader.GetInt32("id");
                                string title = reader.GetString("title");
                                string author = reader.IsDBNull(2) ? "Не указан" : reader.GetString(2);
                                string serverUri = reader.IsDBNull(8) ? "" : reader.GetString(8);
                                string coverImageUri = reader.IsDBNull(10) ? "" : reader.GetString(10);
                                
                                var book = new Book
                                {
                                    BookId = bookId,
                                    BookFileId = reader.GetInt32(5),
                                    Title = title,
                                    Author = author,
                                    LocalBookID = bookId,
                                    FilePath = GetServerBookPath(serverUri), // Объединяем путь к папке DB с путем из БД
                                    FileName = reader.GetString(9),
                                    PublishedYear = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                    Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    AddedDate = DateTime.Now
                                };
                                
                                // Обработка обложки
                                if (!string.IsNullOrEmpty(coverImageUri))
                                {
                                    // Если это относительный путь, делаем его абсолютным
                                    if (!System.IO.Path.IsPathRooted(coverImageUri))
                                    {
                                        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                                        var libIndex = baseDir.IndexOf("LIB", StringComparison.OrdinalIgnoreCase);
                                        if (libIndex >= 0)
                                        {
                                            var projectRoot = baseDir.Substring(0, libIndex + 3);
                                            book.CoverImageSource = System.IO.Path.Combine(projectRoot, coverImageUri.Replace('/', '\\'));
                                        }
                                        else
                                        {
                                            book.CoverImageSource = GetCoverPlaceholder(System.IO.Path.GetExtension(book.FileName));
                                        }
                                    }
                                    else
                                    {
                                        book.CoverImageSource = coverImageUri;
                                    }
                                }
                                else
                                {
                                    string format = System.IO.Path.GetExtension(book.FileName).ToLower().TrimStart('.');
                                    if (string.IsNullOrEmpty(format)) format = "unknown";
                                    book.CoverImageSource = GetCoverPlaceholder(format);
                                }
                                
                                // Проверяем существование обложки
                                if (!string.IsNullOrEmpty(book.CoverImageSource) && !File.Exists(book.CoverImageSource))
                                {
                                    book.CoverImageSource = GetCoverPlaceholder(System.IO.Path.GetExtension(book.FileName));
                                }
                                
                                catalogBooks.Add(book);
                            }
                        }
                    }
                }
                
                // Обновляем отображение каталога
                UpdateCatalogDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке книг с сервера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// Обновляет отображение каталога книг
        private void UpdateCatalogDisplay()
        {
            // Применяем фильтр поиска
            ApplyCatalogFilter();
            
            if (CatalogBooksItemsControl != null)
            {
                CatalogBooksItemsControl.ItemsSource = null;
                CatalogBooksItemsControl.ItemsSource = filteredCatalogBooks;
            }
            
            if (NoCatalogBooksText != null)
            {
                NoCatalogBooksText.Visibility = filteredCatalogBooks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        /// Применяет фильтр поиска к каталогу книг
        private void ApplyCatalogFilter()
        {
            string searchText = "";
            
            if (CatalogSearchTextBox != null)
            {
                searchText = CatalogSearchTextBox.Text?.Trim() ?? "";
            }
            
            filteredCatalogBooks.Clear();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Если поиск пустой, показываем все книги
                filteredCatalogBooks.AddRange(catalogBooks);
            }
            else
            {
                // Фильтруем книги по названию (без учета регистра)
                string searchLower = searchText.ToLower();
                foreach (var book in catalogBooks)
                {
                    if (book.Title != null && book.Title.ToLower().Contains(searchLower))
                    {
                        filteredCatalogBooks.Add(book);
                    }
                }
            }
        }
        
        /// Обработчик изменения текста в поле поиска каталога
        private void CatalogSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCatalogDisplay();
        }


        /// Загружает прогресс чтения из JSON файла

        /// Загружает прогресс чтения из базы данных

        private void LoadReadingProgressFromDatabase()
        {
            try
            {
                readingProgress.Clear();
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    using (var command = new MySqlCommand(
                        "SELECT * FROM reading_progress WHERE user_id = @user_id",
                        conn))
                    {
                        command.Parameters.AddWithValue("@user_id", currentUserId);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var progress = new ReadingProgress
                                {
                                    Id = reader.GetInt32("id"),
                                    BookFileId = reader.GetInt32("book_file_id"),
                                    UserId = reader.GetInt32("user_id"),
                                    CurrentPage = reader.GetInt32("current_page"),
                                    TotalPages = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                    ProgressPercent = reader.IsDBNull(6) ? 0 : reader.GetDouble(6),
                                    LastReadAt = reader.GetDateTime("last_read_at")
                                };
                                
                                readingProgress[progress.BookFileId] = progress;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                readingProgress = new Dictionary<int, ReadingProgress>();
                MessageBox.Show($"Ошибка при загрузке прогресса чтения из БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Удаляет прогресс чтения из базы данных

        private void DeleteReadingProgressFromDatabase(int bookFileId)
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    using (var command = new MySqlCommand(
                        "DELETE FROM reading_progress WHERE book_file_id = @book_file_id AND user_id = @user_id",
                        conn))
                    {
                        command.Parameters.AddWithValue("@book_file_id", bookFileId);
                        command.Parameters.AddWithValue("@user_id", currentUserId);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении прогресса чтения из БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Очищает весь прогресс чтения из базы данных

        private void ClearAllReadingProgressFromDatabase()
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    using (var command = new MySqlCommand(
                        "DELETE FROM reading_progress WHERE user_id = @user_id",
                        conn))
                    {
                        command.Parameters.AddWithValue("@user_id", currentUserId);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при очистке прогресса чтения из БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Сохраняет прогресс чтения в базу данных

        private void SaveReadingProgressToDatabase(ReadingProgress progress)
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    if (progress.Id == 0)
                    {
                        // Вставляем новый прогресс
                        using (var command = new MySqlCommand(
                            "INSERT INTO reading_progress (book_file_id, user_id, current_page, total_pages, progress_percent, last_read_at) VALUES (@book_file_id, @user_id, @current_page, @total_pages, @progress_percent, @last_read_at)",
                            conn))
                        {
                            command.Parameters.AddWithValue("@book_file_id", progress.BookFileId);
                            command.Parameters.AddWithValue("@user_id", progress.UserId);
                            command.Parameters.AddWithValue("@current_page", progress.CurrentPage);
                            command.Parameters.AddWithValue("@total_pages", progress.TotalPages);
                            command.Parameters.AddWithValue("@progress_percent", progress.ProgressPercent);
                            command.Parameters.AddWithValue("@last_read_at", progress.LastReadAt);
                            
                            command.ExecuteNonQuery();
                            progress.Id = (int)command.LastInsertedId;
                        }
                    }
                    else
                    {
                        // Обновляем существующий прогресс
                        using (var command = new MySqlCommand(
                            "UPDATE reading_progress SET current_page = @current_page, total_pages = @total_pages, progress_percent = @progress_percent, last_read_at = @last_read_at WHERE id = @id",
                            conn))
                        {
                            command.Parameters.AddWithValue("@current_page", progress.CurrentPage);
                            command.Parameters.AddWithValue("@total_pages", progress.TotalPages);
                            command.Parameters.AddWithValue("@progress_percent", progress.ProgressPercent);
                            command.Parameters.AddWithValue("@last_read_at", progress.LastReadAt);
                            command.Parameters.AddWithValue("@id", progress.Id);
                            
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении прогресса чтения в БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Очищает все книги из базы данных

        private void ClearAllBooksFromDatabase()
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Удаляем весь прогресс чтения
                            using (var command = new MySqlCommand(
                                "DELETE FROM reading_progress",
                                conn, transaction))
                            {
                                command.ExecuteNonQuery();
                            }
                            
                            // Удаляем все файлы книг
                            using (var command = new MySqlCommand(
                                "DELETE FROM book_files",
                                conn, transaction))
                            {
                                command.ExecuteNonQuery();
                            }
                            
                            // Удаляем все книги
                            using (var command = new MySqlCommand(
                                "DELETE FROM books",
                                conn, transaction))
                            {
                                command.ExecuteNonQuery();
                            }
                            
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при очистке БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Удаляет книгу из базы данных

        private void DeleteBookFromDatabase(Book book)
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Удаляем прогресс чтения
                            if (book.BookFileId > 0)
                            {
                                using (var command = new MySqlCommand(
                                    "DELETE FROM reading_progress WHERE book_file_id = @book_file_id",
                                    conn, transaction))
                                {
                                    command.Parameters.AddWithValue("@book_file_id", book.BookFileId);
                                    command.ExecuteNonQuery();
                                }
                            }
                            books.Remove(book);
                            catalogBooks.Remove(book);
                            UpdateBooksDisplay();
                            UpdateBooksGridDisplay();

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении книги из БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Сохраняет книгу в базу данных

        private void SaveBookToDatabase(Book book)
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Вставляем книгу в таблицу books
                            int bookId;
                            if (book.BookId == 0)
                            {
                                using (var command = new MySqlCommand(
                                    "INSERT INTO books (title, author, published_year, description) VALUES (@title, @author, @published_year, @description)",
                                    conn, transaction))
                                {
                                    command.Parameters.AddWithValue("@title", book.Title);
                                    command.Parameters.AddWithValue("@author", book.Author == "Не указан" ? null : book.Author);
                                    command.Parameters.AddWithValue("@published_year", book.PublishedYear);
                                    command.Parameters.AddWithValue("@description", string.IsNullOrEmpty(book.Description) ? null : book.Description);
                                    
                                    command.ExecuteNonQuery();
                                    bookId = (int)command.LastInsertedId;
                                    book.BookId = bookId;
                                }
                            }
                            else
                            {
                                bookId = book.BookId;
                                // Обновляем существующую книгу
                                using (var command = new MySqlCommand(
                                    "UPDATE books SET title = @title, author = @author, published_year = @published_year, description = @description WHERE id = @id",
                                    conn, transaction))
                                {
                                    command.Parameters.AddWithValue("@title", book.Title);
                                    command.Parameters.AddWithValue("@author", book.Author == "Не указан" ? null : book.Author);
                                    command.Parameters.AddWithValue("@published_year", book.PublishedYear);
                                    command.Parameters.AddWithValue("@description", string.IsNullOrEmpty(book.Description) ? null : book.Description);
                                    command.Parameters.AddWithValue("@id", bookId);
                                    command.ExecuteNonQuery();
                                }
                            }
                            
                            // Вставляем файл книги в таблицу book_files
                            if (book.BookFileId == 0)
                            {
                                string format = System.IO.Path.GetExtension(book.FilePath)?.ToLower().TrimStart('.') ?? "unknown";
                                if (string.IsNullOrWhiteSpace(format)) format = "unknown";
                                using (var command = new MySqlCommand(
                                    "INSERT INTO book_files (book_id, format, source_type, local_path, file_name) VALUES (@book_id, @format, @source_type, @local_path, @file_name)",
                                    conn, transaction))
                                {
                                    command.Parameters.AddWithValue("@book_id", bookId);
                                    command.Parameters.AddWithValue("@format", format);
                                    command.Parameters.AddWithValue("@source_type", "local");
                                    command.Parameters.AddWithValue("@local_path", book.FilePath);
                                    command.Parameters.AddWithValue("@file_name", book.FileName);
                                    
                                    command.ExecuteNonQuery();
                                    book.BookFileId = (int)command.LastInsertedId;
                                }
                            }

                            // Привязываем книгу к текущему пользователю в user_books (если ещё не привязана)
                            using (var command = new MySqlCommand(
                                "INSERT INTO user_books (user_id, book_id, status) VALUES (@user_id, @book_id, 'planned') ON DUPLICATE KEY UPDATE status = status",
                                conn, transaction))
                            {
                                command.Parameters.AddWithValue("@user_id", currentUserId);
                                command.Parameters.AddWithValue("@book_id", bookId);
                                command.ExecuteNonQuery();
                            }
                            
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении книги в БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Очищает список книг

        private void ClearBooksButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите удалить все книги? Это действие нельзя отменить!",
                                       "Подтверждение удаления",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ClearAllBooksFromDatabase();
                LoadBooksFromDatabase();
                LoadReadingProgressFromDatabase();
            UpdateBooksDisplay();
            }
        }

        /// Открывает панель каталога книг
        private void CatalogBooksButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            BackToLibraryButton.Visibility = Visibility.Collapsed;
            
            // Показываем панель каталога
            CatalogPanel.Visibility = Visibility.Visible;
            
            // Очищаем поле поиска
            if (CatalogSearchTextBox != null)
            {
                CatalogSearchTextBox.Text = "";
            }
            
            // Загружаем книги с сервера
            LoadServerBooks();
        }
        
        /// Возврат из каталога книг
        private void BackFromCatalog_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            WelcomePanel.Visibility = Visibility.Visible;
        }
        
        /// Добавляет книгу из каталога в библиотеку
        private void AddBookFromCatalog_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                if (currentUserId <= 0)
                {
                    MessageBox.Show("Чтобы добавлять книги, войдите в аккаунт.", "Требуется вход", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                try
                {
                    // Добавляем книгу пользователю
                    AddUserBook(currentUserId, book.BookId, "planned");
                    
                    MessageBox.Show($"Книга '{book.Title}' добавлена в вашу библиотеку!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Перезагружаем книги пользователя
                    LoadBooksFromDatabase();
                    UpdateBooksDisplay();
                    
                    // Удаляем книгу из каталога
                    catalogBooks.Remove(book);
                    UpdateCatalogDisplay();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении книги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
                DeleteBookFromDatabase(selectedBook);
                LoadBooksFromDatabase();
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
            { }
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
                        SaveBookToDatabase(book);
                        LoadBooksFromDatabase();
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
                // Удаляем прогресс чтения из БД
                if (book.BookFileId > 0)
                {
                    readingProgress.Remove(book.BookFileId);
                }
                
                DeleteBookFromDatabase(book);
                LoadBooksFromDatabase();
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
            if ((sender is Button button && button.Tag is Book book))
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

            HideAllPanels();
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
            GoToNextPage();
            GoToPreviousPage();

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
                    // Рендерим все страницы в панель для вертикальной прокрутки
                    RenderPagesToPanel();
                    // Переходим к текущей странице (по умолчанию 0 или восстановленная)
                    ShowCurrentPage();
                    StatusText.Text = "Книга успешно загружена. Используйте колесо мыши для прокрутки и стрелки для перехода по страницам";
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
                case ".epub":
                    return await ReadEpubFileAsync(filePath);
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
                case ".epub":
                    return ReadEpubFile(filePath);
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
                return $"❌ Ошибка при чтении PDF файла: {ex.Message}\n\n";
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

        /// Читает EPUB файл (асинхронная версия)

        private async Task<string> ReadEpubFileAsync(string filePath)
        {
            return await Task.Run(() => ReadEpubFile(filePath));
        }

        /// Читает EPUB файл

        private string ReadEpubFile(string filePath)
        {
            try
            {
                // EPUB файлы - это ZIP архивы с XML содержимым
                // Простое чтение как ZIP архива
                using (var archive = System.IO.Compression.ZipFile.OpenRead(filePath))
                {
                    var result = new StringBuilder();
                    
                    // Ищем файл с метаданными
                    var metadataEntry = archive.Entries.FirstOrDefault(e => e.Name == "metadata.opf");
                    if (metadataEntry != null)
                    {
                        using (var stream = metadataEntry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            string metadata = reader.ReadToEnd();
                            result.AppendLine(ExtractEpubMetadata(metadata));
                        }
                    }
                    
                    // Ищем файлы с содержимым (обычно в папке OEBPS)
                    var contentEntries = archive.Entries
                        .Where(e => e.FullName.StartsWith("OEBPS/") && 
                                   (e.Name.EndsWith(".html") || e.Name.EndsWith(".xhtml") || e.Name.EndsWith(".htm")))
                        .OrderBy(e => e.FullName)
                        .ToList();
                    
                    if (contentEntries.Count == 0)
                    {
                        // Если нет папки OEBPS, ищем HTML файлы в корне
                        contentEntries = archive.Entries
                            .Where(e => e.Name.EndsWith(".html") || e.Name.EndsWith(".xhtml") || e.Name.EndsWith(".htm"))
                            .OrderBy(e => e.FullName)
                            .ToList();
                    }
                    
                    result.AppendLine("\n📖 СОДЕРЖАНИЕ:\n");
                    
                    foreach (var entry in contentEntries)
                    {
                        try
                        {
                            using (var stream = entry.Open())
                            using (var reader = new StreamReader(stream))
                            {
                                string content = reader.ReadToEnd();
                                string cleanContent = CleanHtmlContent(content);
                                if (!string.IsNullOrWhiteSpace(cleanContent))
                                {
                                    result.AppendLine(cleanContent);
                                    result.AppendLine("\n");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            result.AppendLine($"❌ Ошибка при чтении файла {entry.Name}: {ex.Message}");
                        }
                    }
                    
                    return result.ToString();
                }
            }
            catch (Exception ex)
            {
                return $"❌ Ошибка при чтении EPUB файла: {ex.Message}\n\n" +
                       "Попробуйте проверить целостность файла.";
            }
        }

        /// Извлекает метаданные из EPUB файла

        private string ExtractEpubMetadata(string metadataXml)
        {
            try
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(metadataXml);
                
                string title = ExtractXmlValue(xmlDoc, "//dc:title") ?? 
                              ExtractXmlValue(xmlDoc, "//title") ?? 
                              "Неизвестно";
                
                string author = ExtractXmlValue(xmlDoc, "//dc:creator") ?? 
                               ExtractXmlValue(xmlDoc, "//creator") ?? 
                               "Неизвестно";
                
                string language = ExtractXmlValue(xmlDoc, "//dc:language") ?? 
                                 ExtractXmlValue(xmlDoc, "//language") ?? 
                                 "";
                
                string description = ExtractXmlValue(xmlDoc, "//dc:description") ?? 
                                    ExtractXmlValue(xmlDoc, "//description") ?? 
                                    "";
                
                var result = new StringBuilder();
                result.AppendLine($"📚 {title}");
                result.AppendLine($"✍️ Автор: {author}");
                
                if (!string.IsNullOrEmpty(language))
                {
                    result.AppendLine($"🌐 Язык: {language}");
                }
                
                if (!string.IsNullOrEmpty(description))
                {
                    result.AppendLine($"📝 Описание: {description}");
                }
                
                return result.ToString();
            }
            catch
            {
                return "📚 EPUB Книга\n✍️ Автор: Неизвестно";
            }
        }

        /// Очищает HTML содержимое для чтения

        private string CleanHtmlContent(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return htmlContent;

            // Убираем HTML теги
            string cleanContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, @"<[^>]+>", " ");
            
            // Декодируем HTML entities
            cleanContent = System.Net.WebUtility.HtmlDecode(cleanContent);
            
            // Убираем лишние пробелы и переносы строк
            cleanContent = System.Text.RegularExpressions.Regex.Replace(cleanContent, @"\s+", " ");
            
            // Убираем множественные переносы строк
            while (cleanContent.Contains("\n\n\n"))
            {
                cleanContent = cleanContent.Replace("\n\n\n", "\n\n");
            }
            
            return cleanContent.Trim();
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

            

            return content;
        }
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
            // Показываем ошибку в панели содержимого
            var contentPanel = GetBookContentPanel();
            if (contentPanel != null)
            {
                contentPanel.Children.Clear();
                contentPanel.Children.Add(new TextBlock
                {
                    Text = errorText,
                    FontSize = 18,
                    Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 28,
                    TextAlignment = TextAlignment.Justify,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, 0, 16)
                });
            }
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
                // Обновляем текущую страницу на основе видимости блоков страниц
                int newIndex = GetMostVisiblePageIndex(scrollViewer);
                if (newIndex >= 0 && newIndex < bookPages.Count && newIndex != currentPageIndex)
                {
                    currentPageIndex = newIndex;
                    UpdatePageInfo();
                    UpdateReadingProgressFromPage();
                }

                // Дополнительно обновляем прогресс от прокрутки, если нет страниц
                if (bookPages.Count == 0)
                {
                    double progress = 0;
                    if (scrollViewer.ExtentHeight > 0)
                    {
                        progress = (scrollViewer.VerticalOffset / (scrollViewer.ExtentHeight - scrollViewer.ViewportHeight)) * 100;
                        progress = Math.Max(0, Math.Min(100, progress));
                    }
                    UpdateReadingProgress(progress);
                    StatusText.Text = $"Прогресс чтения: {progress:F0}%";
                }
            }
        }

        // Определяет индекс страницы, которая сейчас наиболее видима в области прокрутки
        private int GetMostVisiblePageIndex(ScrollViewer scrollViewer)
        {
            var contentPanel = GetBookContentPanel();
            if (contentPanel == null || contentPanel.Children.Count == 0)
                return -1;

            // Границы видимой области внутри ScrollViewer
            double viewportTop = 0;
            double viewportBottom = scrollViewer.ViewportHeight;

            int bestIndex = -1;
            double bestVisible = -1;

            for (int i = 0; i < contentPanel.Children.Count; i++)
            {
                if (contentPanel.Children[i] is FrameworkElement fe)
                {
                    // Позиция элемента относительно ScrollViewer
                    GeneralTransform transform = fe.TransformToAncestor(scrollViewer);
                    Point topLeft = transform.Transform(new Point(0, 0));
                    double elemTop = topLeft.Y;
                    double elemBottom = elemTop + fe.RenderSize.Height;

                    // Пересечение с видимой областью
                    double visibleTop = Math.Max(elemTop, viewportTop);
                    double visibleBottom = Math.Min(elemBottom, viewportBottom);
                    double visibleHeight = Math.Max(0, visibleBottom - visibleTop);

                    if (visibleHeight > bestVisible)
                    {
                        bestVisible = visibleHeight;
                        bestIndex = i;
                    }
                }
            }

            return bestIndex;
        }

        /// Возвращает к главной панели библиотеки

        private void BackToLibrary_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Сброс состояния приложения и возврат на панель авторизации
            isLogin = false;
            currentUserId = 0;
            isAdmin = false;
            AdminPanelButton.Visibility = Visibility.Collapsed;
            books.Clear();
            readingProgress.Clear();
            catalogBooks.Clear();
            ClearAuthInputs();

            HideAllPanels();

            // Показываем авторизацию и скрываем навигацию
            AutorisationPanel.Visibility = Visibility.Visible;
            NavigationButtons.Visibility = Visibility.Collapsed;
            BooksButton.Visibility = Visibility.Collapsed;
            SettingsButton.Visibility = Visibility.Collapsed;

            // Очистка UI списков
            UpdateBooksDisplay();
        }

        private void ClearAuthInputs()
        {
            try
            {
                if (LoginTextBox != null) LoginTextBox.Text = string.Empty;
                if (PasswordTextBox != null) PasswordTextBox.Password = string.Empty;
                if (RegisterLoginTextBox != null) RegisterLoginTextBox.Text = string.Empty;
                if (RegisterPasswordTextBox != null) RegisterPasswordTextBox.Password = string.Empty;
                if (ConfirmPasswordTextBox != null) ConfirmPasswordTextBox.Password = string.Empty;
            }
            catch { }
        }

        /// Увеличивает размер шрифта

        private void FontSizeUp_Click(object sender, RoutedEventArgs e)
        {
            double currentSize = GetCurrentContentFontSize();
            if (currentSize < 32)
            {
                SetContentFontSize(currentSize + 2);
                StatusText.Text = $"Размер шрифта: {GetCurrentContentFontSize()}";
            }
        }

        /// Уменьшает размер шрифта

        private void FontSizeDown_Click(object sender, RoutedEventArgs e)
        {
            double currentSize = GetCurrentContentFontSize();
            if (currentSize > 12)
            {
                SetContentFontSize(currentSize - 2);
                StatusText.Text = $"Размер шрифта: {GetCurrentContentFontSize()}";
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
                // Прокручиваем к соответствующему блоку страницы
                var contentPanel = GetBookContentPanel();
                if (contentPanel != null && contentPanel.Children.Count == bookPages.Count)
                {
                    var pageElement = contentPanel.Children[currentPageIndex] as FrameworkElement;
                    pageElement?.BringIntoView();
                }
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
            if (book.BookFileId > 0 && readingProgress.ContainsKey(book.BookFileId))
            {
                var progress = readingProgress[book.BookFileId];

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
            if (currentBook != null && bookPages.Count > 0 && currentBook.BookFileId > 0)
            {
                double percentage = ((double)(currentPageIndex + 1) / bookPages.Count) * 100;
                
                if (readingProgress.ContainsKey(currentBook.BookFileId))
                {
                    // Обновляем существующий прогресс
                    var progress = readingProgress[currentBook.BookFileId];
                    progress.CurrentPage = currentPageIndex;
                    progress.TotalPages = bookPages.Count;
                    progress.ProgressPercent = percentage;
                    progress.LastReadAt = DateTime.Now;
                    SaveReadingProgressToDatabase(progress);
                }
                else
                {
                    // Создаем новый прогресс
                    var progress = new ReadingProgress(currentBook.BookFileId, currentUserId, currentPageIndex, bookPages.Count);
                    readingProgress[currentBook.BookFileId] = progress;
                    SaveReadingProgressToDatabase(progress);
                }
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
                case ".epub":
                    return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img\\epub.png");
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

            if (content.Length <= 2000)
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

        /// Рендерит все страницы в панель для вертикальной прокрутки
        private void RenderPagesToPanel()
        {
            var contentPanel = GetBookContentPanel();
            if (contentPanel == null)
                return;
            contentPanel.Children.Clear();
            for (int i = 0; i < bookPages.Count; i++)
            {
                var block = new TextBlock
                {
                    Text = bookPages[i],
                    FontSize = 18,
                    Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 28,
                    TextAlignment = TextAlignment.Justify,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, 0, 24)
                };
                contentPanel.Children.Add(block);
            }
            // Сбрасываем прокрутку вверх
            var sv = GetBookScrollViewer();
            sv?.ScrollToVerticalOffset(0);
        }

        private double GetCurrentContentFontSize()
        {
            var contentPanel = GetBookContentPanel();
            if (contentPanel != null && contentPanel.Children.Count > 0 && contentPanel.Children[0] is TextBlock tb)
            {
                return tb.FontSize;
            }
            return 18;
        }

        private void SetContentFontSize(double size)
        {
            var contentPanel = GetBookContentPanel();
            if (contentPanel == null) return;
            foreach (var child in contentPanel.Children)
            {
                if (child is TextBlock tb)
                {
                    tb.FontSize = size;
                    tb.LineHeight = Math.Round(size * 1.55);
                }
            }
        }

        private StackPanel? GetBookContentPanel()
        {
            return this.FindName("BookContentPanel") as StackPanel;
        }

        private ScrollViewer? GetBookScrollViewer()
        {
            return this.FindName("BookScrollViewer") as ScrollViewer;
        }

        /// Показывает панель с гридом книг

        private void BooksButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            // Показываем панель с гридом книг
            BooksGridPanel.Visibility = Visibility.Visible;

            // Обновляем отображение книг в гриде
            UpdateBooksDisplay();
        }

        /// Возврат к главной панели из грида книг

        private void BackToWelcome_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();

            // Показываем главную панель
            WelcomePanel.Visibility = Visibility.Visible;
        }

        /// Обновляет отображение книг в гриде

        private void UpdateBooksGridDisplay()
        {
            // Обновляем прогресс для каждой книги
            UpdateBooksProgress();

            // Привязываем список книг к ItemsControl
            BooksItemsControl.ItemsSource = null;
            BooksItemsControl.ItemsSource = books;
        }

        /// Обновляет прогресс чтения для всех книг

        private void UpdateBooksProgress()
        {
            foreach (var book in books)
            {
                if (book.BookFileId > 0 && readingProgress.ContainsKey(book.BookFileId))
                {
                    var progress = readingProgress[book.BookFileId];
                    book.ProgressWidth = (progress.ProgressPercent / 100.0) * 180;
                    book.ProgressText = $"{progress.ProgressPercent:F0}% ({progress.CurrentPage + 1}/{progress.TotalPages})";
                }
                else
                {
                    book.ProgressWidth = 0;
                    book.ProgressText = "Не читалось";
                }
            }
        }

        /// Чтение книги из грида

        private void ReadBookFromGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                ShowReadingPanel(book);
            }
        }

        /// Редактирование книги из грида

        private void EditBookFromGrid_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Book book)
            {
                ShowEditBookDialog(book);
            }
        }

        /// Показывает диалог редактирования книги

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

            // ScrollViewer для основной области
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            // Основная панель с полями
            var stackPanel = new StackPanel();

            // Группа "Информация о книге"
            var bookInfoBorder = new Border
            {
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(15)
            };

            var bookInfoGroup = new StackPanel();

            var bookInfoTitle = new TextBlock
            {
                Text = "📚 Информация о книге",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 10)
            };
            bookInfoGroup.Children.Add(bookInfoTitle);

            // Название книги
            var titlePanel = new StackPanel
            {
                Margin = new Thickness(0, 8, 0, 8)
            };

            var bookTitleLabel = new TextBlock
            {
                Text = "Название:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var titleTextBox = new TextBox
            {
                Text = book.Title,
                Height = 35,
                FontSize = 13,
                Padding = new Thickness(10, 5, 10, 5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush
            };

            titlePanel.Children.Add(bookTitleLabel);
            titlePanel.Children.Add(titleTextBox);
            bookInfoGroup.Children.Add(titlePanel);

            // Автор
            var authorPanel = new StackPanel
            {
                Margin = new Thickness(0, 8, 0, 8)
            };

            var authorLabel = new TextBlock
            {
                Text = "Автор:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var authorTextBox = new TextBox
            {
                Text = book.Author,
                FontSize = 13,
                Height = 35,
                Padding = new Thickness(10, 5, 10, 5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1)
            };

            authorPanel.Children.Add(authorLabel);
            authorPanel.Children.Add(authorTextBox);
            bookInfoGroup.Children.Add(authorPanel);

            bookInfoBorder.Child = bookInfoGroup;
            stackPanel.Children.Add(bookInfoBorder);

            // Группа "Дополнительная информация"
            var additionalInfoBorder = new Border
            {
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(15)
            };

            var additionalInfoGroup = new StackPanel();

            var additionalInfoTitle = new TextBlock
            {
                Text = "ℹ️ Дополнительная информация",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 10)
            };
            additionalInfoGroup.Children.Add(additionalInfoTitle);

            // Путь к файлу (только для чтения)
            var filePanel = new StackPanel
            {
                Margin = new Thickness(0, 8, 0, 8)
            };

            var fileLabel = new TextBlock
            {
                Text = "Файл:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var fileTextBox = new TextBox
            {
                Text = book.FilePath,
                FontSize = 13,
                Height = 60,
                Padding = new Thickness(10, 5, 10, 5),
                VerticalContentAlignment = VerticalAlignment.Top,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1),
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap
            };

            filePanel.Children.Add(fileLabel);
            filePanel.Children.Add(fileTextBox);
            additionalInfoGroup.Children.Add(filePanel);

            // Дата добавления (только для чтения)
            var datePanel = new StackPanel
            {
                Margin = new Thickness(0, 8, 0, 8)
            };

            var dateLabel = new TextBlock
            {
                Text = "Дата добавления:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 8)
            };

            var dateTextBox = new TextBox
            {
                Text = book.AddedDate.ToString("dd.MM.yyyy HH:mm"),
                FontSize = 13,
                Height = 35,
                Padding = new Thickness(10, 5, 10, 5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1),
                IsReadOnly = true
            };

            datePanel.Children.Add(dateLabel);
            datePanel.Children.Add(dateTextBox);
            additionalInfoGroup.Children.Add(datePanel);

            additionalInfoBorder.Child = additionalInfoGroup;
            stackPanel.Children.Add(additionalInfoBorder);

            scrollViewer.Content = stackPanel;
            Grid.SetRow(scrollViewer, 0);
            grid.Children.Add(scrollViewer);

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

                // Сохраняем в БД
                SaveBookToDatabase(book);
                LoadBooksFromDatabase();

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

        /// Удаление книги из грида

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
                    // Удаляем прогресс чтения из БД
                    if (book.BookFileId > 0)
                    {
                        readingProgress.Remove(book.BookFileId);
                    }
                    
                    DeleteBookFromDatabase(book);
                    LoadBooksFromDatabase();
                    UpdateBooksDisplay();
                    UpdateBooksGridDisplay();
                }
            }
        }

        /// Сброс прогресса чтения из грида

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
                    if (book.BookFileId > 0 && readingProgress.ContainsKey(book.BookFileId))
                    {
                        // Удаляем прогресс из БД
                        DeleteReadingProgressFromDatabase(book.BookFileId);
                        readingProgress.Remove(book.BookFileId);
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

        /// Обработчик кнопки "Предыдущая страница"

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            GoToPreviousPage();
        }

        /// Обработчик кнопки "Следующая страница"

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            GoToNextPage();
        }

        /// Показывает исходный XML для отладки

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
                xmlWindow.ShowDialog();

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

        /// Сброс прогресса чтения для всех книг

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
                    // Удаляем весь прогресс из БД
                    ClearAllReadingProgressFromDatabase();
                    readingProgress.Clear();
                    UpdateBooksDisplay();
                    UpdateBooksGridDisplay();

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
            LoginClick();
        }
        private void LoginClick()
        {
            // Получаем введённые логин и пароль
            string login = LoginTextBox.Text.Trim();
            string password = PasswordTextBox.Password.Trim();

            if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
            {
                int userId = CheckUserCredentials(login, password);
                if (userId > 0)
                {
                    if (isAdmin)
                    {
                        AdminPanelButton.Visibility = Visibility.Visible;
                    }
                    currentUserId = userId;
                    isLogin = true;
                    AfterLogin();
                    ClearAuthInputs();
                }
                else
                {
                    // Неправильные данные
                    MessageBox.Show("Вы неправильно ввели логин или пароль.");
                }
            }
        }
        private int CheckUserCredentials(string login, string password)
        {
            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();

                // Получаем хеш пароля из БД
                using (var command = new MySqlCommand(
                    "SELECT UID, User_password, Is_admin FROM users WHERE User_login = @login",
                    conn))
                {
                    command.Parameters.AddWithValue("@login", login);

                    // Выполнение команды и получение результата
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int userId = reader.GetInt32(0);
                            string passwordHash = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            isAdmin = !reader.IsDBNull(2) && reader.GetBoolean(2);
                            
                            // Проверяем пароль с помощью BCrypt
                            if (!string.IsNullOrEmpty(passwordHash) && BCrypt.Net.BCrypt.Verify(password, passwordHash))
                            {
                                conn.Close();
                                return userId;
                            }
                        }
                    }
                    conn.Close();
                    return 0;
                }
            }
        }

        private void RegisterSubmit_Click(object sender, RoutedEventArgs e)
        {
            RegisterClick();
        }
        private void RegisterClick()
        {
            // Получаем введённые логин и пароль
            string login = RegisterLoginTextBox.Text.Trim();
            string password = RegisterPasswordTextBox.Password.Trim();
            string again_password = ConfirmPasswordTextBox.Password.Trim();
            if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(password))
            {
                if (password == again_password)
                {
                    // Проверяем данные в базе
                    int userId = Register(login, password);
                    if (userId > 0)
                    {
                        currentUserId = userId;
                        isLogin = true;
                        AfterLogin();
                        ClearAuthInputs();
                    }
                    else
                    {
                        // Неправильные данные
                        MessageBox.Show("Такой пользователь уже существует.");
                    }
                }
                else
                {
                    MessageBox.Show("Пароли не совпадают.");
                }
            }
        }
        private int Register(string login, string password)
        {
            try
        {
            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();

                // Хешируем пароль с помощью BCrypt (автоматически генерирует соль)
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                using (var command = new MySqlCommand("INSERT INTO users (User_login, User_password, Is_admin) VALUES (@login, @password, false)", conn))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", passwordHash);
                    command.ExecuteNonQuery();
                        return (int)command.LastInsertedId;
                    }
                }
            }
            catch (Exception ex)
            {
                // Если пользователь уже существует или другая ошибка
                return 0;
            }
        }
        private void GuestLogin_Click(object sender, RoutedEventArgs e)
        {
            // Гостевой режим: не авторизуемся и не загружаем локальные книги
            isLogin = false;
            currentUserId = 0;
            // Покажем минимальный UI без загрузки книг
            BooksButton.Visibility = Visibility.Collapsed;
            NavigationButtons.Visibility = Visibility.Collapsed;
            SettingsButton.Visibility = Visibility.Collapsed;

            HideAllPanels();

            // Очищаем локальные данные и обновляем приветственную панель
            books.Clear();
            readingProgress.Clear();
            catalogBooks.Clear();
            WelcomePanel.Visibility = Visibility.Visible;
            UpdateBooksDisplay();

        }
        private void AfterLogin()
        {
            BooksButton.Visibility = Visibility.Visible;
            NavigationButtons.Visibility = Visibility.Visible;
            SettingsButton.Visibility = Visibility.Visible;
            HideAllPanels();

            // Загружаем настройки после логина
            LoadSettings();

            // Перезагружаем данные из БД только если есть авторизованный пользователь
            books.Clear();
            readingProgress.Clear();
            catalogBooks.Clear();
            if (currentUserId > 0)
            {
                LoadBooksFromDatabase();
                LoadReadingProgressFromDatabase();
            }

            
            WelcomePanel.Visibility = Visibility.Visible;
            UpdateBooksDisplay();
        }

        /// <summary>
        /// Добавляет книгу в список чтения пользователя
        /// </summary>
        private void AddUserBook(int userId, int bookId, string status = "planned")
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();

                    using (var command = new MySqlCommand(
                        "INSERT INTO user_books (user_id, book_id, status) VALUES (@user_id, @book_id, @status) ON DUPLICATE KEY UPDATE status = @status",
                        conn))
                    {
                        command.Parameters.AddWithValue("@user_id", userId);
                        command.Parameters.AddWithValue("@book_id", bookId);
                        command.Parameters.AddWithValue("@status", status);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении книги в список чтения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// Обработчик кнопки настроек
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSettingsWindow();
        }

        /// <summary>
        /// Показывает окно настроек
        /// </summary>
        private void ShowSettingsWindow()
        {
            HideAllPanels();

            // Показываем панель настроек
            SettingsPanel.Visibility = Visibility.Visible;
            
            // Инициализируем настройки
            InitializeSettings();
        }
        
        /// <summary>
        /// Инициализирует значения настроек при открытии панели
        /// </summary>
        private void InitializeSettings()
        {
            // Устанавливаем текущую тему
            if (DarkThemeToggle != null)
            {
                DarkThemeToggle.IsChecked = isDarkTheme;
            }
            
            // Устанавливаем прозрачность окна
            if (OpacitySlider != null)
            {
                OpacitySlider.Value = this.Opacity;
                if (OpacityValueText != null)
                {
                    OpacityValueText.Text = $"{(int)(this.Opacity * 100)}%";
                }
            }
            
            // Устанавливаем информацию о пользователе
            if (UserInfoText != null)
            {
                UserInfoText.Text = currentUserId > 0 ? $"UID: {currentUserId}" : "Гость";
            }
            
            // Устанавливаем размер шрифта из текущего чтения (если есть)
            if (FontSizeSlider != null)
            {
                double currentFontSize = GetCurrentContentFontSize();
                FontSizeSlider.Value = currentFontSize;
                if (FontSizeValueText != null)
                {
                    FontSizeValueText.Text = $"{(int)currentFontSize}px";
                }
            }
            
            // Показываем/скрываем админские настройки
            if (AdminSettingsBorder != null)
            {
                AdminSettingsBorder.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }
            
            // Загружаем путь к папке DB
            if (DbFolderPathTextBox != null)
            {
                DbFolderPathTextBox.Text = dbFolderPath;
            }
        }
        
        /// <summary>
        /// Обработчик кнопки выбора папки DB
        /// </summary>
        private void BrowseDbFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Создаем простое окно для выбора папки
            var folderDialog = new Window
            {
                Title = "Выберите папку DB с серверными книгами",
                Width = 600,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = this.Resources["WindowBackgroundBrush"] as SolidColorBrush
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.Margin = new Thickness(20);

            var stackPanel = new StackPanel();
            
            var label = new TextBlock
            {
                Text = "Введите путь к папке DB:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var pathTextBox = new TextBox
            {
                Height = 35,
                FontSize = 13,
                Padding = new Thickness(0,0,10, 5),
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(2),
                Foreground = new SolidColorBrush(Colors.Black),
                Text = dbFolderPath
            };
            
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(pathTextBox);
            Grid.SetRow(stackPanel, 0);
            grid.Children.Add(stackPanel);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Style = this.Resources["RoundedButtonStyle"] as Style
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 35,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Style = this.Resources["RoundedButtonStyle"] as Style
            };

            okButton.Click += (s, args) =>
            {
                string selectedPath = pathTextBox.Text?.Trim() ?? "";
                
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (!Directory.Exists(selectedPath))
                    {
                        MessageBox.Show("Указанная папка не существует. Пожалуйста, укажите корректный путь.", 
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                
                dbFolderPath = selectedPath;
                
                if (DbFolderPathTextBox != null)
                {
                    DbFolderPathTextBox.Text = dbFolderPath;
                }
                
                // Сохраняем настройку
                SaveSettings();
                
                // Перезагружаем книги, чтобы применить новый путь
                if (currentUserId > 0)
                {
                    LoadBooksFromDatabase();
                    UpdateBooksDisplay();
                }
                
                // Если открыт каталог, перезагружаем его
                if (CatalogPanel != null && CatalogPanel.Visibility == Visibility.Visible)
                {
                    LoadServerBooks();
                }
                
                folderDialog.DialogResult = true;
                folderDialog.Close();
            };

            cancelButton.Click += (s, args) =>
            {
                folderDialog.DialogResult = false;
                folderDialog.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            folderDialog.Content = grid;
            
            // Фокус на текстовое поле и Enter для подтверждения
            pathTextBox.Focus();
            pathTextBox.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    okButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                else if (args.Key == Key.Escape)
                {
                    folderDialog.Close();
                }
            };

            folderDialog.ShowDialog();
        }
        
        /// <summary>
        /// Обработчик потери фокуса текстового поля пути к папке DB
        /// </summary>
        private void DbFolderPathTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string newPath = textBox.Text?.Trim() ?? "";
                
                // Проверяем, изменился ли путь
                if (newPath != dbFolderPath)
                {
                    // Проверяем существование папки, если путь не пустой
                    if (!string.IsNullOrEmpty(newPath) && !Directory.Exists(newPath))
                    {
                        MessageBox.Show("Указанная папка не существует. Пожалуйста, укажите корректный путь.", 
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        textBox.Text = dbFolderPath; // Возвращаем старое значение
                        return;
                    }
                    
                    dbFolderPath = newPath;
                    SaveSettings();
                    
                    // Перезагружаем книги, чтобы применить новый путь
                    if (currentUserId > 0)
                    {
                        LoadBooksFromDatabase();
                        UpdateBooksDisplay();
                    }
                    
                    // Если открыт каталог, перезагружаем его
                    if (CatalogPanel != null && CatalogPanel.Visibility == Visibility.Visible)
                    {
                        LoadServerBooks();
                    }
                }
            }
        }


        /// <summary>
        /// Обработчик кнопки "Назад" из настроек
        /// </summary>
        private void BackFromSettings_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            WelcomePanel.Visibility = Visibility.Visible;
        }
        
        /// <summary>
        /// Обработчик переключения тёмной темы
        /// </summary>
        private void DarkThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            isDarkTheme = true;
            ApplyTheme();
        }
        
        /// <summary>
        /// Обработчик переключения светлой темы
        /// </summary>
        private void DarkThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            isDarkTheme = false;
            ApplyTheme();
        }
        
        /// <summary>
        /// Обработчик изменения прозрачности окна
        /// </summary>
        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValueText != null)
            {
                OpacityValueText.Text = $"{(int)(e.NewValue * 100)}%";
            }
            this.Opacity = e.NewValue;
        }
        
        /// <summary>
        /// Обработчик изменения размера шрифта
        /// </summary>
        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (FontSizeValueText != null)
            {
                FontSizeValueText.Text = $"{(int)e.NewValue}px";
            }
            
            // Применяем размер шрифта к текущему чтению, если открыта книга
            if (ReadingPanel.Visibility == Visibility.Visible)
            {
                SetContentFontSize(e.NewValue);
            }
        }
        
        /// <summary>
        /// Обработчик изменения межстрочного интервала
        /// </summary>
        private void LineHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LineHeightValueText != null)
            {
                LineHeightValueText.Text = $"{e.NewValue:F1}x";
            }
            
            // Применяем межстрочный интервал к текущему чтению, если открыта книга
            if (ReadingPanel.Visibility == Visibility.Visible)
            {
                var contentPanel = GetBookContentPanel();
                if (contentPanel != null)
                {
                    foreach (var child in contentPanel.Children)
                    {
                        if (child is TextBlock tb)
                        {
                            tb.LineHeight = Math.Round(tb.FontSize * e.NewValue);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Обработчик кнопки выхода из аккаунта в настройках
        /// </summary>
        private void SettingsLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            BackFromSettings_Click(sender, e);
            LogoutButton_Click(sender, e);
        }
        
        /// <summary>
        /// Обработчик кнопки сброса настроек
        /// </summary>
        private void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите сбросить все настройки к значениям по умолчанию?",
                "Сброс настроек", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                ResetSettings();
                InitializeSettings();
                MessageBox.Show("Настройки сброшены к значениям по умолчанию.", "Настройки сброшены", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        /// <summary>
        /// Обработчик кнопки экспорта настроек
        /// </summary>
        private void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Экспорт настроек будет доступен в следующих версиях.", "Экспорт", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        /// <summary>
        /// Обработчик кнопки закрытия настроек
        /// </summary>
        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            BackFromSettings_Click(sender, e);
        }
        
        /// <summary>
        /// Обработчик кнопки проверки обновлений
        /// </summary>
        private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Вы используете последнюю версию приложения!", "Обновления", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }


        /// Применяет выбранную тему

        private void ApplyTheme()
        {
            if (isDarkTheme)
            {
                // Переключение на тёмную тему
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                this.Resources["TextBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                this.Resources["ButtonBorderBrush"] = new SolidColorBrush(Color.FromRgb(130, 130, 130));
            }
            else
            {
                // Переключение на светлую тему
                this.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                this.Resources["TextBrush"] = new SolidColorBrush(Colors.Black);
                this.Resources["ButtonBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 218, 185));
                this.Resources["ButtonBorderBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            }
            this.InvalidateVisual();
        }
        
        /// Сбрасывает настройки к значениям по умолчанию
        private void ResetSettings()
        {
            isDarkTheme = false;
            ApplyTheme();
            
            // Сбрасываем значения в UI элементах
            if (DarkThemeToggle != null)
            {
                DarkThemeToggle.IsChecked = false;
            }
            
            if (OpacitySlider != null)
            {
                OpacitySlider.Value = 1.0;
                this.Opacity = 1.0;
            }
            
            if (FontSizeSlider != null)
            {
                FontSizeSlider.Value = 18;
            }
            
            if (LineHeightSlider != null)
            {
                LineHeightSlider.Value = 1.5;
            }
            
            if (AutoSaveToggle != null)
            {
                AutoSaveToggle.IsChecked = true;
            }
            
            if (AutoScrollToggle != null)
            {
                AutoScrollToggle.IsChecked = false;
            }
            
            if (ShowProgressToggle != null)
            {
                ShowProgressToggle.IsChecked = true;
            }
            
            if (CompactModeToggle != null)
            {
                CompactModeToggle.IsChecked = false;
            }
            
            if (AnimationsToggle != null)
            {
                AnimationsToggle.IsChecked = true;
            }
            
            if (AutoAddToggle != null)
            {
                AutoAddToggle.IsChecked = false;
            }
            
            if (ShowHiddenToggle != null)
            {
                ShowHiddenToggle.IsChecked = false;
            }
            
            if (SoundNotificationsToggle != null)
            {
                SoundNotificationsToggle.IsChecked = true;
            }
            
            if (ProgressNotificationsToggle != null)
            {
                ProgressNotificationsToggle.IsChecked = true;
            }
            
            if (SyncProgressToggle != null)
            {
                SyncProgressToggle.IsChecked = true;
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            LoadingPanel.Visibility = Visibility.Visible;


            // Эффект задержки
            await Task.Delay(1000);

            // Основная инициализация
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() => LoadBooksFromDatabase());
                Dispatcher.Invoke(() => LoadReadingProgressFromDatabase());
            });

            HideAllPanels();
            AutorisationPanel.Visibility = Visibility.Visible; 

            
        }

        private void BookScrollViewer_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }

        private void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                LoginClick();
            }
        }


        private void RegisterLoginTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                RegisterClick();
            }
        }
        
        /// <summary>
        /// Загружает настройки из базы данных
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    // Проверяем, существует ли таблица settings
                    using (var checkTable = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'Paradise' AND table_name = 'settings'",
                        conn))
                    {
                        int tableExists = Convert.ToInt32(checkTable.ExecuteScalar());
                        
                        if (tableExists > 0)
                        {
                            // Загружаем настройку пути к папке DB
                            using (var command = new MySqlCommand(
                                "SELECT setting_value FROM settings WHERE setting_key = 'db_folder_path'",
                                conn))
                            {
                                var result = command.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    dbFolderPath = result.ToString();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Если таблицы нет или произошла ошибка, используем значение по умолчанию
                dbFolderPath = "";
            }
        }
        
        /// <summary>
        /// Сохраняет настройки в базу данных
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();
                    
                    // Создаем таблицу settings, если её нет
                    using (var createTable = new MySqlCommand(
                        @"CREATE TABLE IF NOT EXISTS settings (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            setting_key VARCHAR(100) UNIQUE NOT NULL,
                            setting_value TEXT
                        )",
                        conn))
                    {
                        createTable.ExecuteNonQuery();
                    }
                    
                    // Сохраняем настройку пути к папке DB
                    using (var command = new MySqlCommand(
                        "INSERT INTO settings (setting_key, setting_value) VALUES ('db_folder_path', @value) " +
                        "ON DUPLICATE KEY UPDATE setting_value = @value",
                        conn))
                    {
                        command.Parameters.AddWithValue("@value", dbFolderPath ?? "");
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Объединяет путь к папке DB с путем из БД для серверных книг
        /// </summary>
        private string GetServerBookPath(string serverUri)
        {
            if (string.IsNullOrWhiteSpace(serverUri))
                return serverUri;
            
            if (string.IsNullOrWhiteSpace(dbFolderPath))
                return serverUri;
            
            // Если путь уже абсолютный, возвращаем как есть
            if (System.IO.Path.IsPathRooted(serverUri))
                return serverUri;
            
            // Объединяем путь к папке DB с путем из БД
            return System.IO.Path.Combine(dbFolderPath, serverUri.Replace('/', '\\'));
        }
        // === ОБРАБОТЧИКИ АДМИНИСТРАТИВНОЙ ПАНЕЛИ ===

        // Переход в административную панель
        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminPanel();
            LoadAdminStatistics();
        }

        // Назад из административной панели
        private void BackFromAdmin_Click(object sender, RoutedEventArgs e)
        {
            HideAllPanels();
            WelcomePanel.Visibility = Visibility.Visible;
        }

        // === НАВИГАЦИЯ ПО РАЗДЕЛАМ АДМИНКИ ===

        private void AdminMainButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminMainContent();
            LoadAdminStatistics();
        }

        private void AdminBooksButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("Books");
            LoadBooksData();
        }

        private void AdminBookFilesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("BookFiles");
            LoadBookFilesData();
        }

        private void AdminUsersButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("Users");
            LoadUsersData();
        }

        private void AdminReadingStatsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("ReadingStats");
            LoadReadingStatistics();
        }

        private void AdminUserBooksButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("UserBooks");
            LoadUserBooksData();
        }

        private void AdminProgressButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("Progress");
            LoadReadingProgressData();
        }

        

        private void AdminBackupButton_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("Backup");
            LoadBackupData();
        }

        // === БЫСТРЫЕ ДЕЙСТВИЯ НА ГЛАВНОЙ ПАНЕЛИ ===

        private void AdminQuickAddBook_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("Books");
            LoadBooksData();
            // Здесь можно добавить логику для быстрого добавления книги
            ShowAddBookDialog();
        }

        private void AdminQuickUsers_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("Users");
            LoadUsersData();
        }

        private void AdminQuickStats_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("ReadingStats");
            LoadReadingStatistics();
        }

        private void AdminQuickBackup_Click(object sender, RoutedEventArgs e)
        {
            ShowAdminContent("Backup");
            LoadBackupData();
            CreateBackup();
        }

        // === МЕТОДЫ ДЛЯ РАБОТЫ С БАЗОЙ ДАННЫХ ===

        private void LoadAdminStatistics(bool showInGrid = false)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(conectionString))
                {
                    connection.Open();

                    // Загрузка общей статистики
                    string statsQuery = @"
                SELECT 
                    (SELECT COUNT(*) FROM books) AS 'Всего книг',
                    (SELECT COUNT(*) FROM users) AS 'Пользователей',
                    (SELECT COUNT(*) FROM reading_progress) AS 'Активных чтений',
                    (SELECT COUNT(*) FROM book_files) AS 'Файлов книг'";

                    using (MySqlCommand cmd = new MySqlCommand(statsQuery, connection))
                    {
                        DataTable table = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            table.Load(reader);
                        }

                        if (table.Rows.Count > 0)
                        {
                            DataRow row = table.Rows[0];
                            AdminTotalBooksText.Text = row["Всего книг"].ToString();
                            AdminTotalUsersText.Text = row["Пользователей"].ToString();
                            AdminActiveReadingsText.Text = row["Активных чтений"].ToString();
                            AdminBookFilesText.Text = row["Файлов книг"].ToString();
                        }
                        if (showInGrid)
                        {
                            DisplayAdminTable(table);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBooksData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(conectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    id AS 'ID',
                    title AS 'Название',
                    author AS 'Автор',
                    published_year AS 'Год издания',
                    language AS 'Язык',
                    series AS 'Серия'
                FROM books
                ORDER BY id DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        DataTable table = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            table.Load(reader);
                        }
                        DisplayAdminTable(table);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBookFilesData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(conectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    bf.id AS 'ID',
                    bf.book_id AS 'ID книги',
                    b.title AS 'Название книги',
                    b.author AS 'Автор',
                    bf.format AS 'Формат',
                    bf.source_type AS 'Источник',
                    bf.local_path AS 'Локальный путь',
                    bf.server_uri AS 'URL',
                    bf.file_name AS 'Имя файла',
                    bf.cover_image_uri AS 'Обложка'
                FROM book_files bf 
                JOIN books b ON bf.book_id = b.id 
                ORDER BY bf.id DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        DataTable table = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            table.Load(reader);
                        }
                        DisplayAdminTable(table);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файлов книг: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUsersData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(conectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    UID AS 'ID',
                    User_login AS 'Логин',
                    Is_admin AS 'Администратор'
                FROM users
                ORDER BY UID";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        DataTable table = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            table.Load(reader);
                        }
                        DisplayAdminTable(table);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadReadingStatistics()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(conectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    COUNT(DISTINCT rp.user_id) AS 'Активных пользователей',
                    COUNT(DISTINCT bf.book_id) AS 'Активных книг',
                    AVG(rp.progress_percent) AS 'Средний прогресс',
                    MAX(rp.last_read_at) AS 'Последняя активность'
                FROM reading_progress rp
                LEFT JOIN book_files bf ON rp.book_file_id = bf.id";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        DataTable table = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            table.Load(reader);
                        }
                        DisplayAdminTable(table);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики чтения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserBooksData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(conectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    u.User_login AS 'Логин',
                    ub.user_id AS 'ID пользователя',
                    b.title AS 'Название книги',
                    b.author AS 'Автор',
                    ub.book_id AS 'ID книги',
                    ub.status AS 'Статус',
                    ub.added_at AS 'Добавлено',
                    bf.file_name AS 'Имя файла',
                    bf.format AS 'Формат'
                FROM user_books ub
                JOIN users u ON ub.user_id = u.UID
                JOIN books b ON ub.book_id = b.id
                LEFT JOIN book_files bf ON b.id = bf.book_id
                ORDER BY ub.added_at DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        DataTable table = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            table.Load(reader);
                        }
                        DisplayAdminTable(table);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки книг пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadReadingProgressData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(    conectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    rp.id AS 'ID',
                    u.User_login AS 'Логин',
                    rp.user_id AS 'ID пользователя',
                    b.title AS 'Книга',
                    b.author AS 'Автор',
                    bf.file_name AS 'Файл',
                    bf.format AS 'Формат',
                    rp.book_file_id AS 'ID файла',
                    rp.current_page AS 'Текущая страница',
                    rp.total_pages AS 'Всего страниц',
                    rp.progress_percent AS 'Прогресс %',
                    rp.last_read_at AS 'Последнее чтение'
                FROM reading_progress rp
                JOIN users u ON rp.user_id = u.UID
                JOIN book_files bf ON rp.book_file_id = bf.id
                JOIN books b ON bf.book_id = b.id
                ORDER BY rp.last_read_at DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        DataTable table = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            table.Load(reader);
                        }
                        DisplayAdminTable(table);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки прогресса чтения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSettingsData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(conectionString))
                {
                    connection.Open();
                    string query = @"
                SELECT 
                    setting_key AS 'Ключ',
                    setting_value AS 'Значение'
                FROM settings";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        DataTable table = new DataTable();
                        using (var reader = cmd.ExecuteReader())
                        {
                            table.Load(reader);
                        }
                        DisplayAdminTable(table);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBackupData()
        {
            try
            {
                string backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                DataTable table = new DataTable();
                table.Columns.Add("Имя файла");
                table.Columns.Add("Размер (KB)");
                table.Columns.Add("Дата создания");
                table.Columns.Add("Путь");

                var files = Directory.GetFiles(backupDir, "*.sql")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                foreach (var file in files)
                {
                    var row = table.NewRow();
                    row["Имя файла"] = file.Name;
                    row["Размер (KB)"] = (file.Length / 1024.0).ToString("F2");
                    row["Дата создания"] = file.CreationTime.ToString("dd.MM.yyyy HH:mm:ss");
                    row["Путь"] = file.FullName;
                    table.Rows.Add(row);
                }

                DisplayAdminTable(table);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки списка резервных копий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateBackup()
        {
            try
            {
                string backupDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                string backupFile = System.IO.Path.Combine(backupDir, $"backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.sql");

                using (MySqlConnection connection = new MySqlConnection(conectionString))
                {
                    connection.Open();

                    // Получаем список таблиц
                    List<string> tables = new List<string>();
                    using (MySqlCommand cmd = new MySqlCommand("SHOW TABLES", connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }

                    StringBuilder output = new StringBuilder();

                    foreach (var table in tables)
                    {
                        // Структура таблицы
                        using (MySqlCommand createCmd = new MySqlCommand($"SHOW CREATE TABLE `{table}`", connection))
                        using (var reader = createCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                output.AppendLine();
                                output.AppendLine(reader.GetString(1) + ";");
                                output.AppendLine();
                            }
                        }

                        // Данные
                        using (MySqlCommand dataCmd = new MySqlCommand($"SELECT * FROM `{table}`", connection))
                        using (var dataReader = dataCmd.ExecuteReader())
                        {
                            while (dataReader.Read())
                            {
                                StringBuilder insert = new StringBuilder();
                                insert.Append($"INSERT INTO `{table}` VALUES(");
                                for (int i = 0; i < dataReader.FieldCount; i++)
                                {
                                    if (i > 0) insert.Append(", ");
                                    if (dataReader.IsDBNull(i))
                                    {
                                        insert.Append("NULL");
                                    }
                                    else
                                    {
                                        string val = dataReader.GetValue(i).ToString();
                                        val = val.Replace("\\", "\\\\").Replace("'", "\\'");
                                        insert.Append($"'{val}'");
                                    }
                                }
                                insert.Append(");");
                                output.AppendLine(insert.ToString());
                            }
                        }
                    }

                    File.WriteAllText(backupFile, output.ToString(), Encoding.UTF8);
                }

                MessageBox.Show($"Резервная копия создана: {backupFile}", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadBackupData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании резервной копии: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AdminAddRecord_Click(object sender, RoutedEventArgs e)
        {
            if (currentAdminContentType == "Backup")
            {
                CreateBackup();
                return;
            }

            switch (currentAdminContentType)
            {
                case "Books":
                    AddBookRecord();
                    break;
                case "BookFiles":
                    AddBookFileRecord();
                    break;
                case "Users":
                    AddUserRecord();
                    break;
                case "UserBooks":
                    AddUserBookRecord();
                    break;
                case "Progress":
                    AddReadingProgressRecord();
                    break;
                default:
                    MessageBox.Show("Добавление для этого раздела пока не реализовано.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        private void AdminEditRecord_Click(object sender, RoutedEventArgs e)
        {
            if (currentAdminContentType == "Backup")
            {
                var row = AdminDataGrid.SelectedItem as DataRowView;
                if (row == null)
                {
                    MessageBox.Show("Выберите резервную копию для переименования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string path = row["Путь"]?.ToString();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    MessageBox.Show("Неверный файл резервной копии.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string newName = PromptForText("Переименование бэкапа", "Новое имя файла (без пути):", System.IO.Path.GetFileName(path));
                if (string.IsNullOrWhiteSpace(newName))
                    return;

                string dir = System.IO.Path.GetDirectoryName(path) ?? "";
                string newPath = System.IO.Path.Combine(dir, newName);

                try
                {
                    File.Move(path, newPath);
                    LoadBackupData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось переименовать файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            switch (currentAdminContentType)
            {
                case "Books":
                    EditBookRecord();
                    break;
                case "BookFiles":
                    EditBookFileRecord();
                    break;
                case "Users":
                    EditUserRecord();
                    break;
                case "UserBooks":
                    EditUserBookRecord();
                    break;
                case "Progress":
                    EditReadingProgressRecord();
                    break;
                default:
                    MessageBox.Show("Редактирование для этого раздела пока не реализовано.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        private void AdminDeleteRecord_Click(object sender, RoutedEventArgs e)
        {
            if (currentAdminContentType == "Backup")
            {
                var row = AdminDataGrid.SelectedItem as DataRowView;
                if (row == null)
                {
                    MessageBox.Show("Выберите резервную копию для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string path = row["Путь"]?.ToString();
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    MessageBox.Show("Неверный файл резервной копии.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var confirm = MessageBox.Show($"Удалить резервную копию?\n{System.IO.Path.GetFileName(path)}", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        File.Delete(path);
                        LoadBackupData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не удалось удалить файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                return;
            }

            switch (currentAdminContentType)
            {
                case "Books":
                    DeleteBookRecord();
                    break;
                case "BookFiles":
                    DeleteBookFileRecord();
                    break;
                case "Users":
                    DeleteUserRecord();
                    break;
                case "UserBooks":
                    DeleteUserBookRecord();
                    break;
                case "Progress":
                    DeleteReadingProgressRecord();
                    break;
                default:
                    MessageBox.Show("Удаление для этого раздела пока не реализовано.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
            }
        }

        private DataRowView? GetSelectedRow()
        {
            return AdminDataGrid.SelectedItem as DataRowView;
        }

        private void RefreshCurrentAdminContent()
        {
            switch (currentAdminContentType)
            {
                case "Books": LoadBooksData(); break;
                case "BookFiles": LoadBookFilesData(); break;
                case "Users": LoadUsersData(); break;
                case "UserBooks": LoadUserBooksData(); break;
                case "Progress": LoadReadingProgressData(); break;
                case "Backup": LoadBackupData(); break;
                case "ReadingStats": LoadReadingStatistics(); break;
                default: break;
            }
        }

        private void AddBookRecord()
        {
            var form = PromptForForm("Добавить книгу", new List<FormField>
            {
                new FormField{ Key="title", Label="Название", DefaultValue="" },
                new FormField{ Key="author", Label="Автор", DefaultValue="" },
                new FormField{ Key="year", Label="Год издания", DefaultValue="" , Type="number"},
                new FormField{ Key="language", Label="Язык", DefaultValue="" },
                new FormField{ Key="series", Label="Серия", DefaultValue="" }
            });
            if (form == null) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "INSERT INTO books (title, author, published_year, language, series) VALUES (@title, @author, @year, @language, @series)",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@title", form["title"]);
                    cmd.Parameters.AddWithValue("@author", string.IsNullOrWhiteSpace(form["author"]) ? (object)DBNull.Value : form["author"]);
                    if (int.TryParse(form["year"], out int year))
                        cmd.Parameters.AddWithValue("@year", year);
                    else
                        cmd.Parameters.AddWithValue("@year", DBNull.Value);
                    cmd.Parameters.AddWithValue("@language", string.IsNullOrWhiteSpace(form["language"]) ? (object)DBNull.Value : form["language"]);
                    cmd.Parameters.AddWithValue("@series", string.IsNullOrWhiteSpace(form["series"]) ? (object)DBNull.Value : form["series"]);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void EditBookRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var form = PromptForForm("Редактировать книгу", new List<FormField>
            {
                new FormField{ Key="title", Label="Название", DefaultValue=row["Название"].ToString() },
                new FormField{ Key="author", Label="Автор", DefaultValue=row["Автор"].ToString() },
                new FormField{ Key="year", Label="Год издания", DefaultValue=row["Год издания"].ToString(), Type="number" },
                new FormField{ Key="language", Label="Язык", DefaultValue=row["Язык"].ToString() },
                new FormField{ Key="series", Label="Серия", DefaultValue=row["Серия"].ToString() }
            });
            if (form == null) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "UPDATE books SET title=@title, author=@author, published_year=@year, language=@language, series=@series WHERE id=@id",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@title", form["title"]);
                    cmd.Parameters.AddWithValue("@author", string.IsNullOrWhiteSpace(form["author"]) ? (object)DBNull.Value : form["author"]);
                    if (int.TryParse(form["year"], out int year))
                        cmd.Parameters.AddWithValue("@year", year);
                    else
                        cmd.Parameters.AddWithValue("@year", DBNull.Value);
                    cmd.Parameters.AddWithValue("@language", string.IsNullOrWhiteSpace(form["language"]) ? (object)DBNull.Value : form["language"]);
                    cmd.Parameters.AddWithValue("@series", string.IsNullOrWhiteSpace(form["series"]) ? (object)DBNull.Value : form["series"]);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void DeleteBookRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var confirm = MessageBox.Show($"Удалить книгу \"{row["Название"]}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM books WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void AddBookFileRecord()
        {
            var form = PromptForForm("Добавить файл книги", new List<FormField>
            {
                new FormField{ Key="book_id", Label="ID книги", DefaultValue="", Type="number" },
                new FormField{ Key="format", Label="Формат", DefaultValue="" },
                new FormField{ Key="source_type", Label="Источник (local/server)", DefaultValue="local" },
                new FormField{ Key="local_path", Label="Локальный путь", DefaultValue="" },
                new FormField{ Key="server_uri", Label="URL", DefaultValue="" },
                new FormField{ Key="file_name", Label="Имя файла", DefaultValue="" },
                new FormField{ Key="cover", Label="Обложка (uri)", DefaultValue="" }
            });
            if (form == null) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "INSERT INTO book_files (book_id, format, source_type, local_path, server_uri, file_name, cover_image_uri) VALUES (@book_id, @format, @source_type, @local_path, @server_uri, @file_name, @cover)",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@book_id", int.TryParse(form["book_id"], out int bid) ? bid : 0);
                    cmd.Parameters.AddWithValue("@format", form["format"]);
                    cmd.Parameters.AddWithValue("@source_type", form["source_type"]);
                    cmd.Parameters.AddWithValue("@local_path", form["local_path"]);
                    cmd.Parameters.AddWithValue("@server_uri", form["server_uri"]);
                    cmd.Parameters.AddWithValue("@file_name", form["file_name"]);
                    cmd.Parameters.AddWithValue("@cover", string.IsNullOrWhiteSpace(form["cover"]) ? (object)DBNull.Value : form["cover"]);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void EditBookFileRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var form = PromptForForm("Редактировать файл книги", new List<FormField>
            {
                new FormField{ Key="book_id", Label="ID книги", DefaultValue=row["ID книги"].ToString(), Type="number" },
                new FormField{ Key="format", Label="Формат", DefaultValue=row["Формат"].ToString() },
                new FormField{ Key="source_type", Label="Источник", DefaultValue=row["Источник"].ToString() },
                new FormField{ Key="local_path", Label="Локальный путь", DefaultValue=row["Локальный путь"].ToString() },
                new FormField{ Key="server_uri", Label="URL", DefaultValue=row["URL"].ToString() },
                new FormField{ Key="file_name", Label="Имя файла", DefaultValue=row["Имя файла"].ToString() },
                new FormField{ Key="cover", Label="Обложка", DefaultValue=row["Обложка"].ToString() }
            });
            if (form == null) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    @"UPDATE book_files SET 
                        book_id=@book_id,
                        format=@format,
                        source_type=@source_type,
                        local_path=@local_path,
                        server_uri=@server_uri,
                        file_name=@file_name,
                        cover_image_uri=@cover
                      WHERE id=@id",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@book_id", int.TryParse(form["book_id"], out int bid) ? bid : 0);
                    cmd.Parameters.AddWithValue("@format", form["format"]);
                    cmd.Parameters.AddWithValue("@source_type", form["source_type"]);
                    cmd.Parameters.AddWithValue("@local_path", form["local_path"]);
                    cmd.Parameters.AddWithValue("@server_uri", form["server_uri"]);
                    cmd.Parameters.AddWithValue("@file_name", form["file_name"]);
                    cmd.Parameters.AddWithValue("@cover", string.IsNullOrWhiteSpace(form["cover"]) ? (object)DBNull.Value : form["cover"]);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void DeleteBookFileRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var confirm = MessageBox.Show($"Удалить файл книги \"{row["Имя файла"]}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM book_files WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void AddUserRecord()
        {
            var form = PromptForForm("Добавить пользователя", new List<FormField>
            {
                new FormField{ Key="login", Label="Логин", DefaultValue="" },
                new FormField{ Key="password", Label="Пароль", DefaultValue="" },
                new FormField{ Key="is_admin", Label="Администратор", DefaultValue="0", Type="bool" }
            });
            if (form == null) return;
            if (string.IsNullOrWhiteSpace(form["password"]))
            {
                MessageBox.Show("Пароль не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "INSERT INTO users (User_login, User_password, Is_admin) VALUES (@login, @password, @is_admin)",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@login", form["login"]);
                    cmd.Parameters.AddWithValue("@password", form["password"]);
                    cmd.Parameters.AddWithValue("@is_admin", form["is_admin"] == "1");
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void EditUserRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var form = PromptForForm("Редактировать пользователя", new List<FormField>
            {
                new FormField{ Key="login", Label="Логин", DefaultValue=row["Логин"].ToString() },
                new FormField{ Key="password", Label="Пароль (оставьте пустым чтобы не менять)", DefaultValue="" },
                new FormField{ Key="is_admin", Label="Администратор", DefaultValue=(row["Администратор"].ToString() == "True" || row["Администратор"].ToString() == "1") ? "1" : "0", Type="bool" }
            });
            if (form == null) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                string sql = string.IsNullOrWhiteSpace(form["password"])
                    ? "UPDATE users SET User_login=@login, Is_admin=@is_admin WHERE UID=@id"
                    : "UPDATE users SET User_login=@login, User_password=@password, Is_admin=@is_admin WHERE UID=@id";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@login", form["login"]);
                    cmd.Parameters.AddWithValue("@is_admin", form["is_admin"] == "1");
                    if (!string.IsNullOrWhiteSpace(form["password"]))
                        cmd.Parameters.AddWithValue("@password", form["password"]);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void DeleteUserRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var confirm = MessageBox.Show($"Удалить пользователя \"{row["Логин"]}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM users WHERE UID=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void AddUserBookRecord()
        {
            var form = PromptForForm("Добавить книгу пользователю", new List<FormField>
            {
                new FormField{ Key="user_id", Label="ID пользователя", DefaultValue="", Type="number" },
                new FormField{ Key="book_id", Label="ID книги", DefaultValue="", Type="number" },
                new FormField{ Key="status", Label="Статус", DefaultValue="planned" }
            });
            if (form == null) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "INSERT INTO user_books (user_id, book_id, status) VALUES (@user_id, @book_id, @status)",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", int.TryParse(form["user_id"], out int uid) ? uid : 0);
                    cmd.Parameters.AddWithValue("@book_id", int.TryParse(form["book_id"], out int bid) ? bid : 0);
                    cmd.Parameters.AddWithValue("@status", form["status"]);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void EditUserBookRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var form = PromptForForm("Редактировать запись пользователя-книги", new List<FormField>
            {
                new FormField{ Key="user_id", Label="ID пользователя", DefaultValue=row["ID пользователя"].ToString(), Type="number" },
                new FormField{ Key="book_id", Label="ID книги", DefaultValue=row["ID книги"].ToString(), Type="number" },
                new FormField{ Key="status", Label="Статус", DefaultValue=row["Статус"].ToString() }
            });
            if (form == null) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "UPDATE user_books SET user_id=@user_id, book_id=@book_id, status=@status WHERE id=@id",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@user_id", int.TryParse(form["user_id"], out int uid) ? uid : 0);
                    cmd.Parameters.AddWithValue("@book_id", int.TryParse(form["book_id"], out int bid) ? bid : 0);
                    cmd.Parameters.AddWithValue("@status", form["status"]);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void DeleteUserBookRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var confirm = MessageBox.Show($"Удалить связь пользователя \"{row["Логин"]}\" с книгой \"{row["Название книги"]}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM user_books WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void AddReadingProgressRecord()
        {
            var form = PromptForForm("Добавить прогресс чтения", new List<FormField>
            {
                new FormField{ Key="user_id", Label="ID пользователя", DefaultValue="", Type="number" },
                new FormField{ Key="book_file_id", Label="ID файла", DefaultValue="", Type="number" },
                new FormField{ Key="current_page", Label="Текущая страница", DefaultValue="0", Type="number" },
                new FormField{ Key="total_pages", Label="Всего страниц", DefaultValue="0", Type="number" },
                new FormField{ Key="progress_percent", Label="Прогресс %", DefaultValue="0", Type="number" }
            });
            if (form == null) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    "INSERT INTO reading_progress (user_id, book_file_id, current_page, total_pages, progress_percent, last_read_at) VALUES (@user_id, @book_file_id, @current_page, @total_pages, @progress_percent, NOW())",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", int.TryParse(form["user_id"], out int uid) ? uid : 0);
                    cmd.Parameters.AddWithValue("@book_file_id", int.TryParse(form["book_file_id"], out int bf) ? bf : 0);
                    cmd.Parameters.AddWithValue("@current_page", int.TryParse(form["current_page"], out int cp) ? cp : 0);
                    cmd.Parameters.AddWithValue("@total_pages", int.TryParse(form["total_pages"], out int tp) ? tp : 0);
                    cmd.Parameters.AddWithValue("@progress_percent", double.TryParse(form["progress_percent"], out double pr) ? pr : 0);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void EditReadingProgressRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var form = PromptForForm("Редактировать прогресс чтения", new List<FormField>
            {
                new FormField{ Key="user_id", Label="ID пользователя", DefaultValue=row["ID пользователя"].ToString(), Type="number" },
                new FormField{ Key="book_file_id", Label="ID файла", DefaultValue=row["ID файла"].ToString(), Type="number" },
                new FormField{ Key="current_page", Label="Текущая страница", DefaultValue=row["Текущая страница"].ToString(), Type="number" },
                new FormField{ Key="total_pages", Label="Всего страниц", DefaultValue=row["Всего страниц"].ToString(), Type="number" },
                new FormField{ Key="progress_percent", Label="Прогресс %", DefaultValue=row["Прогресс %"].ToString(), Type="number" }
            });
            if (form == null) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(
                    @"UPDATE reading_progress 
                      SET user_id=@user_id, book_file_id=@book_file_id, current_page=@current_page, total_pages=@total_pages, progress_percent=@progress_percent, last_read_at=NOW()
                      WHERE id=@id",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@user_id", int.TryParse(form["user_id"], out int uid) ? uid : 0);
                    cmd.Parameters.AddWithValue("@book_file_id", int.TryParse(form["book_file_id"], out int bf) ? bf : 0);
                    cmd.Parameters.AddWithValue("@current_page", int.TryParse(form["current_page"], out int cp) ? cp : 0);
                    cmd.Parameters.AddWithValue("@total_pages", int.TryParse(form["total_pages"], out int tp) ? tp : 0);
                    cmd.Parameters.AddWithValue("@progress_percent", double.TryParse(form["progress_percent"], out double pr) ? pr : 0);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private void DeleteReadingProgressRecord()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите запись.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!int.TryParse(row["ID"].ToString(), out int id)) return;

            var confirm = MessageBox.Show($"Удалить прогресс пользователя \"{row["Логин"]}\" по книге \"{row["Книга"]}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            using (var conn = new MySqlConnection(conectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM reading_progress WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
            RefreshCurrentAdminContent();
        }

        private string PromptForText(string title, string label, string defaultValue = "")
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = this.Resources["WindowBackgroundBrush"] as SolidColorBrush
            };

            var grid = new Grid { Margin = new Thickness(15) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lbl = new TextBlock
            {
                Text = label,
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = this.Resources["TextBrush"] as SolidColorBrush
            };
            Grid.SetRow(lbl, 0);

            var tb = new TextBox
            {
                Text = defaultValue,
                Height = 32,
                Padding = new Thickness(8, 4, 8, 4),
                Background = Brushes.White,
                Foreground = Brushes.Black
            };
            Grid.SetRow(tb, 1);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var ok = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 8, 0),
                Style = this.Resources["RoundedButtonStyle"] as Style,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush
            };
            var cancel = new Button
            {
                Content = "Отмена",
                Width = 80,
                Height = 30,
                Style = this.Resources["RoundedButtonStyle"] as Style,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush
            };

            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            Grid.SetRow(buttons, 2);

            grid.Children.Add(lbl);
            grid.Children.Add(tb);
            grid.Children.Add(buttons);

            dialog.Content = grid;

            string result = null;
            ok.Click += (s, args) => { result = tb.Text?.Trim(); dialog.DialogResult = true; dialog.Close(); };
            cancel.Click += (s, args) => { dialog.DialogResult = false; dialog.Close(); };

            tb.KeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    result = tb.Text?.Trim();
                    dialog.DialogResult = true;
                    dialog.Close();
                }
                if (args.Key == Key.Escape)
                {
                    dialog.DialogResult = false;
                    dialog.Close();
                }
            };

            dialog.ShowDialog();
            return result;
        }

        private Dictionary<string, string>? PromptForForm(string title, List<FormField> fields)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 420,
                Height = 280 + Math.Max(0, fields.Count - 4) * 40,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = this.Resources["WindowBackgroundBrush"] as SolidColorBrush
            };

            var grid = new Grid { Margin = new Thickness(15) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            var inputs = new Dictionary<string, FrameworkElement>();

            foreach (var field in fields)
            {
                var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 10) };
                panel.Children.Add(new TextBlock
                {
                    Text = field.Label,
                    Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                    Margin = new Thickness(0, 0, 0, 4)
                });

                FrameworkElement input;
                if (field.Type == "bool")
                {
                    var cb = new CheckBox
                    {
                        IsChecked = field.DefaultValue == "1" || field.DefaultValue.Equals("true", StringComparison.OrdinalIgnoreCase),
                        Foreground = this.Resources["TextBrush"] as SolidColorBrush
                    };
                    input = cb;
                }
                else
                {
                    var tb = new TextBox
                    {
                        Text = field.DefaultValue,
                        Height = 28,
                        Padding = new Thickness(6, 3, 6, 3),
                        Background = Brushes.White,
                        Foreground = Brushes.Black
                    };
                    input = tb;
                }

                inputs[field.Key] = input;
                panel.Children.Add(input);
                stack.Children.Add(panel);
            }

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = stack
            };
            Grid.SetRow(scroll, 0);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var ok = new Button
            {
                Content = "OK",
                Width = 90,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0),
                Style = this.Resources["RoundedButtonStyle"] as Style,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush
            };
            var cancel = new Button
            {
                Content = "Отмена",
                Width = 90,
                Height = 32,
                Style = this.Resources["RoundedButtonStyle"] as Style,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush
            };

            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            Grid.SetRow(buttons, 1);

            grid.Children.Add(scroll);
            grid.Children.Add(buttons);

            dialog.Content = grid;

            Dictionary<string, string>? result = null;

            ok.Click += (s, e) =>
            {
                var values = new Dictionary<string, string>();
                foreach (var f in fields)
                {
                    if (!inputs.ContainsKey(f.Key)) continue;
                    var ctrl = inputs[f.Key];
                    string val = "";
                    if (ctrl is TextBox tb) val = tb.Text.Trim();
                    if (ctrl is CheckBox cb) val = cb.IsChecked == true ? "1" : "0";
                    values[f.Key] = val;
                }
                result = values;
                dialog.DialogResult = true;
                dialog.Close();
            };

            cancel.Click += (s, e) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            dialog.ShowDialog();
            return result;
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

        private void ShowAdminPanel()
        {
            HideAllPanels();
            AdminPanel.Visibility = Visibility.Visible;
            AdminLoadingPanel.Visibility = Visibility.Visible;
            AdminMainContent.Visibility = Visibility.Visible;
            AdminContentControl.Visibility = Visibility.Collapsed;
            ShowAdminMainContent();
        }

        private void ShowAdminMainContent()
        {
            HideAllPanels();
            AdminPanel.Visibility = Visibility.Visible;
            AdminMainContent.Visibility = Visibility.Visible;
            AdminContentControl.Visibility = Visibility.Collapsed;
            AdminLoadingPanel.Visibility = Visibility.Collapsed;
            if (AdminActionsPanel != null) AdminActionsPanel.Visibility = Visibility.Collapsed;
        }

        private async void ShowAdminContent(string contentType)
        {
            currentAdminContentType = contentType;
            TextBoxLoading.Text = contentType;
            HideAllPanels();
            AdminPanel.Visibility = Visibility.Visible;
            AdminLoadingPanel.Visibility = Visibility.Visible;
            AdminContentControl.Visibility = Visibility.Collapsed;
            AdminDataGrid.ItemsSource = null;

            // Здесь можно динамически загружать соответствующий контент
            // в зависимости от contentType
            if (contentType == "Books")
            {
                await Task.Delay(1000);
                AdminLoadingPanel.Visibility = Visibility.Collapsed;
                LoadBooksData();
            }
            if (contentType == "BookFiles")
            {
                await Task.Delay(1000);
                AdminLoadingPanel.Visibility = Visibility.Collapsed;
                LoadBookFilesData();
            }
            if (contentType == "Users")
            {
                await Task.Delay(1000);
                AdminLoadingPanel.Visibility = Visibility.Collapsed;
                LoadUsersData();
            }
            if (contentType == "ReadingStats")
            {
                await Task.Delay(1000);
                AdminLoadingPanel.Visibility = Visibility.Collapsed;
                LoadReadingStatistics();
            }
            if (contentType == "UserBooks")
            {
                await Task.Delay(1000);
                AdminLoadingPanel.Visibility = Visibility.Collapsed;
                LoadUserBooksData();
            }
            if (contentType == "Progress")
            {
                await Task.Delay(1000);
                AdminLoadingPanel.Visibility = Visibility.Collapsed;
                LoadReadingProgressData();
            }
            if (contentType == "Backup")
            {
                await Task.Delay(1000);
                AdminLoadingPanel.Visibility = Visibility.Collapsed;
                LoadBackupData();
            }
        }

        private void DisplayAdminTable(DataTable table)
        {
            AdminDataGrid.ItemsSource = table?.DefaultView;
            AdminLoadingPanel.Visibility = Visibility.Collapsed;
            AdminContentControl.Visibility = Visibility.Visible;
            if (AdminActionsPanel != null) AdminActionsPanel.Visibility = Visibility.Visible;

            // Скрываем ID-колонки
            if (AdminDataGrid != null && AdminDataGrid.Columns.Count > 0)
            {
                foreach (var col in AdminDataGrid.Columns)
                {
                    string header = col.Header?.ToString() ?? "";
                    if (header.ToLower().Contains("id"))
                    {
                        col.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void ShowAddBookDialog()
        {
                LoadBooksData();
                LoadAdminStatistics();
            
        }
        private void HideAllPanels()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            AutorisationPanel.Visibility = Visibility.Collapsed;
            WelcomePanel.Visibility = Visibility.Collapsed;
            BooksGridPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            CatalogPanel.Visibility = Visibility.Collapsed;
            ReadingPanel.Visibility = Visibility.Collapsed;
            BackToLibraryButton.Visibility = Visibility.Collapsed;
            AdminPanel.Visibility = Visibility.Collapsed;
            AdminLoadingPanel.Visibility = Visibility.Collapsed;
            AdminMainContent.Visibility = Visibility.Collapsed;
            AdminContentControl.Visibility = Visibility.Collapsed; 

            if (AdminActionsPanel != null) AdminActionsPanel.Visibility = Visibility.Collapsed;
        }

        
    }
}