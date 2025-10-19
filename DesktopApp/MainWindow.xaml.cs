﻿using System.Text;
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
using MySqlConnector;
using Microsoft.Extensions.Configuration;

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
        private string currentXmlContent = "";
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

        public MainWindow()
        {

            index_found();
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;

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
                if (currentUserId <= 0)
                {
                    MessageBox.Show("Чтобы добавлять книги, войдите в аккаунт.", "Требуется вход", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
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
                                 bf.id as file_id, bf.source_type, bf.local_path, bf.file_name, bf.cover_image_uri
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
                                    book.FilePath = reader.GetString(7);
                                    book.FileName = reader.GetString(8);
                                    string format = System.IO.Path.GetExtension(book.FilePath).ToLower().TrimStart('.');
                                    if (string.IsNullOrEmpty(format)) format = "unknown";
                                    book.CoverImageSource = GetCoverPlaceholder(format);
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
                            
                            // Удаляем файлы книги
                            if (book.BookId > 0)
                            {
                                using (var command = new MySqlCommand(
                                    "DELETE FROM book_files WHERE book_id = @book_id",
                                    conn, transaction))
                                {
                                    command.Parameters.AddWithValue("@book_id", book.BookId);
                                    command.ExecuteNonQuery();
                                }
                                
                                // Удаляем саму книгу
                                using (var command = new MySqlCommand(
                                    "DELETE FROM books WHERE id = @id",
                                    conn, transaction))
                                {
                                    command.Parameters.AddWithValue("@id", book.BookId);
                                    command.ExecuteNonQuery();
                                }
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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Сброс состояния приложения и возврат на панель авторизации
            isLogin = false;
            currentUserId = 0;
            books.Clear();
            readingProgress.Clear();
            ClearAuthInputs();

            // Скрываем рабочие панели
            ReadingPanel.Visibility = Visibility.Collapsed;
            BooksGridPanel.Visibility = Visibility.Collapsed;
            WelcomePanel.Visibility = Visibility.Collapsed;
            BackToLibraryButton.Visibility = Visibility.Collapsed;

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

        /// Обработчик нажатий клавиш для навигации по страницам

        //private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        //{
        //    if (ReadingPanel.Visibility == Visibility.Visible && bookPages.Count > 0)
        //    {
        //        switch (e.Key)
        //        {
        //            case Key.Left:
        //            case Key.PageUp:
        //                GoToPreviousPage();
        //                e.Handled = true;
        //                break;
        //            case Key.Right:
        //            case Key.PageDown:
        //            case Key.Space:
        //                GoToNextPage();
        //                e.Handled = true;
        //                break;
        //            case Key.Home:
        //                GoToFirstPage();
        //                e.Handled = true;
        //                break;
        //            case Key.End:
        //                GoToLastPage();
        //                e.Handled = true;
        //                break;
        //        }
        //    }
        //}

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
            // Скрываем все панели
            WelcomePanel.Visibility = Visibility.Collapsed;
            ReadingPanel.Visibility = Visibility.Collapsed;
            BackToLibraryButton.Visibility = Visibility.Collapsed;
            // Показываем панель с гридом книг
            BooksGridPanel.Visibility = Visibility.Visible;

            // Обновляем отображение книг в гриде
            UpdateBooksDisplay();
        }

        /// Возврат к главной панели из грида книг

        private void BackToWelcome_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем панель с гридом
            BooksGridPanel.Visibility = Visibility.Collapsed;

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

                // Подготовленная команда для защиты от SQL инъекций
                using (var command = new MySqlCommand(
                    "SELECT UID FROM users WHERE User_login = @login AND User_password = @password",
                    conn))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);

                    // Выполнение команды и получение результата
                    var result = command.ExecuteScalar();
                    conn.Close();
                    
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
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

                using (var command = new MySqlCommand("INSERT INTO users (User_login, User_password, Is_admin) VALUES (@login, @password, false)", conn))
                {
                    command.Parameters.AddWithValue("@login", login);
                    command.Parameters.AddWithValue("@password", password);
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

            AutorisationPanel.Visibility = Visibility.Collapsed;
            BackToLibraryButton.Visibility = Visibility.Collapsed;
            BooksGridPanel.Visibility = Visibility.Collapsed;
            ReadingPanel.Visibility = Visibility.Collapsed;

            // Очищаем локальные данные и обновляем приветственную панель
            books.Clear();
            readingProgress.Clear();
            WelcomePanel.Visibility = Visibility.Visible;
            UpdateBooksDisplay();

        }
        private void AfterLogin()
        {
            BooksButton.Visibility = Visibility.Visible;
            NavigationButtons.Visibility = Visibility.Visible;
            SettingsButton.Visibility = Visibility.Visible;
            // Скрываем все панели
            AutorisationPanel.Visibility = Visibility.Collapsed;
            BackToLibraryButton.Visibility = Visibility.Collapsed;
            BooksGridPanel.Visibility = Visibility.Collapsed;
            ReadingPanel.Visibility = Visibility.Collapsed;

            // Перезагружаем данные из БД только если есть авторизованный пользователь
            books.Clear();
            readingProgress.Clear();
            if (currentUserId > 0)
            {
                LoadBooksFromDatabase();
                LoadReadingProgressFromDatabase();
            }

            // Показываем главную панель
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

        /// <summary>
        /// Обновляет статус книги в списке чтения пользователя
        /// </summary>
        private void UpdateUserBookStatus(int userId, int bookId, string status)
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();

                    using (var command = new MySqlCommand(
                        "UPDATE user_books SET status = @status WHERE user_id = @user_id AND book_id = @book_id",
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
                MessageBox.Show($"Ошибка при обновлении статуса книги: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Получает список книг пользователя с их статусами
        /// </summary>
        private List<UserBook> GetUserBooks(int userId)
        {
            var userBooks = new List<UserBook>();
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();

                    using (var command = new MySqlCommand(
                        "SELECT * FROM user_books WHERE user_id = @user_id",
                        conn))
                    {
                        command.Parameters.AddWithValue("@user_id", userId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                userBooks.Add(new UserBook
                                {
                                    UserId = reader.GetInt32("user_id"),
                                    BookId = reader.GetInt32("book_id"),
                                    Status = reader.GetString("status")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка чтения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return userBooks;
        }

        /// <summary>
        /// Удаляет книгу из списка чтения пользователя
        /// </summary>
        private void RemoveUserBook(int userId, int bookId)
        {
            try
            {
                using (var conn = new MySqlConnection(conectionString))
                {
                    conn.Open();

                    using (var command = new MySqlCommand(
                        "DELETE FROM user_books WHERE user_id = @user_id AND book_id = @book_id",
                        conn))
                    {
                        command.Parameters.AddWithValue("@user_id", userId);
                        command.Parameters.AddWithValue("@book_id", bookId);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении книги из списка чтения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
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
            var settingsWindow = new Window
            {
                Title = "⚙️ Настройки",
                Width = 800,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = this.Resources["WindowBackgroundBrush"] as SolidColorBrush,
                ResizeMode = ResizeMode.CanResize,
                WindowStyle = WindowStyle.None,
                MinWidth = 600,
                MinHeight = 500
            };

            // Основной контейнер в стиле приложения
            var mainContainer = new Border
            {
                Background = this.Resources["WindowBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(15)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Заголовок в стиле приложения
            var headerBorder = new Border
            {
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(25, 20, 25, 20)
            };

            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var titleLabel = new TextBlock
            {
                Text = "⚙️ Настройки приложения",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var subtitleLabel = new TextBlock
            {
                Text = "Персонализируйте ваш опыт работы с Paradise",
                FontSize = 14,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Opacity = 0.8,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var titleStack = new StackPanel();
            titleStack.Children.Add(titleLabel);
            titleStack.Children.Add(subtitleLabel);
            headerPanel.Children.Add(titleStack);
            headerBorder.Child = headerPanel;
            grid.Children.Add(headerBorder);
            Grid.SetRow(headerBorder, 0);

            // Основная панель с настройками
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(25, 20, 25, 10),
                Background = Brushes.Transparent
            };

            var stackPanel = new StackPanel();
            
            // Раздел "Внешний вид" с улучшенным дизайном
            var appearanceGroup = CreateModernSettingsGroup("🎨 Внешний вид", stackPanel);
            
            // Переключатель темы с современным дизайном
            var themePanel = CreateModernSettingRow("🌙 Тёмная тема", "Переключение между светлой и тёмной темой", appearanceGroup);
            var themeToggle = CreateModernToggle(isDarkTheme);
            themeToggle.Checked += (s, e) => { isDarkTheme = true; ApplyTheme(); };
            themeToggle.Unchecked += (s, e) => { isDarkTheme = false; ApplyTheme(); };
            themePanel.Children.Add(themeToggle);

            // Прозрачность окна с красивым слайдером
            var opacityPanel = CreateModernSettingRow("🔍 Прозрачность окна", "Настройка прозрачности основного окна", appearanceGroup);
            var opacityContainer = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            var opacitySlider = CreateModernSlider(0.7, 1.0, 1.0, 200);
            var opacityValue = new TextBlock
            {
                Text = "100%",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 50,
                Margin = new Thickness(15, 0, 0, 0)
            };
            opacitySlider.ValueChanged += (s, e) => 
            {
                opacityValue.Text = $"{(int)(e.NewValue * 100)}%";
                this.Opacity = e.NewValue;
            };
            
            opacityContainer.Children.Add(opacitySlider);
            opacityContainer.Children.Add(opacityValue);
            opacityPanel.Children.Add(opacityContainer);

            // Раздел "Чтение" с улучшенным дизайном
            var readingGroup = CreateModernSettingsGroup("📖 Настройки чтения", stackPanel);
            
            // Размер шрифта
            var fontSizePanel = CreateModernSettingRow("📝 Размер шрифта", "Настройка размера текста для комфортного чтения", readingGroup);
            var fontSizeContainer = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            var fontSizeSlider = CreateModernSlider(10, 32, 18, 250);
            var fontSizeValue = new TextBlock
            {
                Text = "18px",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 60,
                Margin = new Thickness(15, 0, 0, 0)
            };
            fontSizeSlider.ValueChanged += (s, e) => fontSizeValue.Text = $"{(int)e.NewValue}px";
            
            fontSizeContainer.Children.Add(fontSizeSlider);
            fontSizeContainer.Children.Add(fontSizeValue);
            fontSizePanel.Children.Add(fontSizeContainer);

            // Межстрочный интервал
            var lineHeightPanel = CreateModernSettingRow("📏 Межстрочный интервал", "Настройка расстояния между строками", readingGroup);
            var lineHeightContainer = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            var lineHeightSlider = CreateModernSlider(1.0, 2.5, 1.5, 250);
            var lineHeightValue = new TextBlock
            {
                Text = "1.5x",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 50,
                Margin = new Thickness(15, 0, 0, 0)
            };
            lineHeightSlider.ValueChanged += (s, e) => lineHeightValue.Text = $"{e.NewValue:F1}x";
            
            lineHeightContainer.Children.Add(lineHeightSlider);
            lineHeightContainer.Children.Add(lineHeightValue);
            lineHeightPanel.Children.Add(lineHeightContainer);

            // Автосохранение прогресса
            var autoSavePanel = CreateModernSettingRow("💾 Автосохранение прогресса", "Автоматическое сохранение прогресса чтения", readingGroup);
            var autoSaveToggle = CreateModernToggle(true);
            autoSavePanel.Children.Add(autoSaveToggle);

            // Автопрокрутка
            var autoScrollPanel = CreateModernSettingRow("🔄 Автопрокрутка при чтении", "Автоматическая прокрутка текста во время чтения", readingGroup);
            var autoScrollToggle = CreateModernToggle(false);
            autoScrollPanel.Children.Add(autoScrollToggle);

            // Раздел "Интерфейс"
            var interfaceGroup = CreateModernSettingsGroup("🖥️ Интерфейс", stackPanel);
            
            // Показывать прогресс чтения
            var showProgressPanel = CreateModernSettingRow("📊 Показывать прогресс чтения", "Отображение индикатора прогресса чтения", interfaceGroup);
            var showProgressToggle = CreateModernToggle(true);
            showProgressPanel.Children.Add(showProgressToggle);

            // Компактный режим
            var compactModePanel = CreateModernSettingRow("📱 Компактный режим", "Уменьшенный интерфейс для экономии места", interfaceGroup);
            var compactModeToggle = CreateModernToggle(false);
            compactModePanel.Children.Add(compactModeToggle);

            // Анимации
            var animationsPanel = CreateModernSettingRow("✨ Анимации интерфейса", "Плавные переходы и анимации элементов", interfaceGroup);
            var animationsToggle = CreateModernToggle(true);
            animationsPanel.Children.Add(animationsToggle);

            // Раздел "Файлы"
            var filesGroup = CreateModernSettingsGroup("📁 Файлы", stackPanel);
            
            // Автоматическое добавление в библиотеку
            var autoAddPanel = CreateModernSettingRow("📥 Автоматически добавлять новые файлы", "Автоматическое добавление новых файлов в библиотеку", filesGroup);
            var autoAddToggle = CreateModernToggle(false);
            autoAddPanel.Children.Add(autoAddToggle);

            // Показывать скрытые файлы
            var showHiddenPanel = CreateModernSettingRow("👁️ Показывать скрытые файлы", "Отображение скрытых файлов в списке", filesGroup);
            var showHiddenToggle = CreateModernToggle(false);
            showHiddenPanel.Children.Add(showHiddenToggle);

            // Раздел "Уведомления"
            var notificationsGroup = CreateModernSettingsGroup("🔔 Уведомления", stackPanel);
            
            // Звуковые уведомления
            var soundPanel = CreateModernSettingRow("🔊 Звуковые уведомления", "Воспроизведение звуков при уведомлениях", notificationsGroup);
            var soundToggle = CreateModernToggle(true);
            soundPanel.Children.Add(soundToggle);

            // Уведомления о прогрессе
            var progressNotifPanel = CreateModernSettingRow("📈 Уведомления о прогрессе", "Уведомления о достижении целей чтения", notificationsGroup);
            var progressNotifToggle = CreateModernToggle(true);
            progressNotifPanel.Children.Add(progressNotifToggle);

            // Раздел "Аккаунт"
            var accountGroup = CreateModernSettingsGroup("👤 Аккаунт", stackPanel);

            // Показать текущего пользователя (ID)
            var userInfoPanel = CreateModernSettingRow("Текущий пользователь", "Идентификатор активного аккаунта", accountGroup);
            var userInfoText = new TextBlock
            {
                Text = currentUserId > 0 ? $"UID: {currentUserId}" : "Гость",
                FontSize = 14,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            userInfoPanel.Children.Add(userInfoText);

            // Автосинхронизация прогресса (пример настройки аккаунта)
            var syncPanel = CreateModernSettingRow("Синхронизация прогресса", "Автоматически сохранять прогресс в аккаунте", accountGroup);
            var syncToggle = CreateModernToggle(true);
            syncPanel.Children.Add(syncToggle);

            // Кнопка выхода из аккаунта
            var logoutPanel = CreateModernSettingRow("Выход из аккаунта", "Завершить сеанс и вернуться к авторизации", accountGroup);
            var logoutButton = CreateModernButton("🚪 Выйти", 120, 35);
            logoutButton.Click += (s, e) =>
            {
                settingsWindow.Close();
                LogoutButton_Click(s, e);
            };
            logoutPanel.Children.Add(logoutButton);

            scrollViewer.Content = stackPanel;
            Grid.SetRow(scrollViewer, 1);
            grid.Children.Add(scrollViewer);
            // Раздел "О программе"
            var aboutGroup = CreateModernSettingsGroup("ℹ️ О программе", stackPanel);
            
            var aboutText = new TextBlock
            {
                Text = "📚 Paradise Library Manager\nВерсия 1.0.0\n\nСистема управления личной библиотекой с поддержкой различных форматов книг и отслеживанием прогресса чтения.\n\nПоддерживаемые форматы:\n• TXT, MD, RTF\n• FB2, XML\n• PDF\n• DOC, DOCX",
                FontSize = 13,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 10),
                LineHeight = 20
            };
            aboutGroup.Children.Add(aboutText);

            // Кнопка "Проверить обновления"
            var checkUpdatesButton = CreateModernButton("🔄 Проверить обновления", 180, 35);
            checkUpdatesButton.Click += (s, e) => 
            {
                MessageBox.Show("Вы используете последнюю версию приложения!", "Обновления", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            };
            aboutGroup.Children.Add(checkUpdatesButton);

            

            // Панель кнопок в стиле приложения
            var buttonPanel = new Border
            {
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                CornerRadius = new CornerRadius(0, 0, 8, 8),
                Padding = new Thickness(25, 15, 25, 15),
                Margin = new Thickness(0, 15, 0, 0)
            };

            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var resetButton = CreateModernButton("🔄 Сбросить", 130, 45);
            var exportButton = CreateModernButton("📤 Экспорт", 130, 45);
            var closeButton = CreateModernButton("✅ Закрыть", 130, 45);

            resetButton.Margin = new Thickness(0, 0, 15, 0);
            exportButton.Margin = new Thickness(0, 0, 15, 0);

            buttonStack.Children.Add(resetButton);
            buttonStack.Children.Add(exportButton);
            buttonStack.Children.Add(closeButton);

            buttonPanel.Child = buttonStack;
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            mainContainer.Child = grid;
            settingsWindow.Content = mainContainer;

            // Обработчики кнопок
            resetButton.Click += (s, e) =>
            {
                var result = MessageBox.Show("Вы уверены, что хотите сбросить все настройки к значениям по умолчанию?",
                    "Сброс настроек", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ResetSettings();
                    settingsWindow.Close();
                    MessageBox.Show("Настройки сброшены к значениям по умолчанию.", "Настройки сброшены", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };

            exportButton.Click += (s, e) =>
            {
                MessageBox.Show("Экспорт настроек будет доступен в следующих версиях.", "Экспорт", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            };

            closeButton.Click += (s, e) => settingsWindow.Close();

            // Показываем окно
            settingsWindow.ShowDialog();
        }


        /// Создает группу настроек с заголовком

        private StackPanel CreateSettingsGroup(string title, StackPanel parent)
        {
            var group = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 25)
            };

            var titleLabel = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 15)
            };

            group.Children.Add(titleLabel);
            parent.Children.Add(group);
            return group;
        }


        /// Создает строку настройки с лейблом

        private StackPanel CreateSettingRow(string labelText, StackPanel parent)
        {
            var rowPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(0, 8, 0, 8),
                VerticalAlignment = VerticalAlignment.Center
            };

            var label = new TextBlock
            {
                Text = labelText,
                FontSize = 14,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 200,
                TextWrapping = TextWrapping.Wrap
            };

            rowPanel.Children.Add(label);
            parent.Children.Add(rowPanel);
            return rowPanel;
        }

        /// <summary>
        /// Создает группу настроек в стиле приложения
        /// </summary>
        private StackPanel CreateModernSettingsGroup(string title, StackPanel parent)
        {
            var groupBorder = new Border
            {
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(15, 12, 15, 12)
            };

            var group = new StackPanel();

            var titleLabel = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Margin = new Thickness(0, 0, 0, 10)
            };

            group.Children.Add(titleLabel);
            groupBorder.Child = group;
            parent.Children.Add(groupBorder);
            return group;
        }

        /// <summary>
        /// Создает строку настройки в стиле приложения
        /// </summary>
        private StackPanel CreateModernSettingRow(string title, string description, StackPanel parent)
        {
            var rowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 8),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var titleLabel = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 250,
                TextWrapping = TextWrapping.Wrap
            };

            var descriptionLabel = new TextBlock
            {
                Text = description,
                FontSize = 11,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 200,
                Margin = new Thickness(10, 0, 0, 0)
            };

            var controlPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            };

            rowPanel.Children.Add(titleLabel);
            rowPanel.Children.Add(descriptionLabel);
            rowPanel.Children.Add(controlPanel);
            parent.Children.Add(rowPanel);
            return controlPanel;
        }

        /// <summary>
        /// Создает современный переключатель в стиле приложения
        /// </summary>
        private CheckBox CreateModernToggle(bool isChecked)
        {
            var toggle = new CheckBox
            {
                IsChecked = isChecked,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 20,
                Height = 20,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush
            };

            return toggle;
        }

        /// <summary>
        /// Создает современный слайдер
        /// </summary>
        private Slider CreateModernSlider(double minimum, double maximum, double value, double width)
        {
            var slider = new Slider
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = value,
                Width = width,
                Height = 25,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Стилизация слайдера
            var style = new Style(typeof(Slider));
            
            slider.Style = style;
            return slider;
        }

        /// <summary>
        /// Создает кнопку в стиле приложения
        /// </summary>
        private Button CreateModernButton(string content, double width, double height)
        {
            var button = new Button
            {
                Content = content,
                Width = width,
                Height = height,
                Background = this.Resources["ButtonBackgroundBrush"] as SolidColorBrush,
                BorderBrush = this.Resources["ButtonBorderBrush"] as SolidColorBrush,
                Foreground = this.Resources["TextBrush"] as SolidColorBrush,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Style = this.Resources["RoundedButtonStyle"] as Style
            };

            return button;
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
            // Здесь можно добавить сброс других настроек
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            isDarkTheme = true;
            ApplyTheme();
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
    }
}