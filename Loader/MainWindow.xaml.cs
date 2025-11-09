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
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;

namespace Loader
{
    // Модели данных для работы с базой данных
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public int? SeriesIndex { get; set; }
        public string Language { get; set; } = "ru";
        public int? PublishedYear { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class BookFile
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string Format { get; set; } = "txt";
        public string SourceType { get; set; } = "local";
        public string LocalPath { get; set; } = string.Empty;
        public string ServerUri { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long? FileSizeBytes { get; set; }
        public int? PageCount { get; set; }
        public string ContentHash { get; set; } = string.Empty;
        public string CoverImageUri { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; } = DateTime.Now;
        public bool Available { get; set; } = true;
    }

    // Сервис для работы с базой данных
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = "server=localhost;database=Paradise;uid=root;pwd=root;";
        }

        public async Task<int> AddBookAsync(Book book)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO books (title, author, series, series_index, language, published_year, description)
                VALUES (@title, @author, @series, @seriesIndex, @language, @publishedYear, @description);
                SELECT LAST_INSERT_ID();";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@title", book.Title);
            command.Parameters.AddWithValue("@author", book.Author);
            command.Parameters.AddWithValue("@series", book.Series);
            command.Parameters.AddWithValue("@seriesIndex", book.SeriesIndex);
            command.Parameters.AddWithValue("@language", book.Language);
            command.Parameters.AddWithValue("@publishedYear", book.PublishedYear);
            command.Parameters.AddWithValue("@description", book.Description);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task AddBookFileAsync(BookFile bookFile)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                INSERT INTO book_files (book_id, format, source_type, local_path, server_uri, file_name, 
                                      file_size_bytes, page_count, content_hash, cover_image_uri, added_at, available)
                VALUES (@bookId, @format, @sourceType, @localPath, @serverUri, @fileName, 
                        @fileSizeBytes, @pageCount, @contentHash, @coverImageUri, @addedAt, @available)";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@bookId", bookFile.BookId);
            command.Parameters.AddWithValue("@format", bookFile.Format);
            command.Parameters.AddWithValue("@sourceType", bookFile.SourceType);
            command.Parameters.AddWithValue("@localPath", bookFile.LocalPath);
            command.Parameters.AddWithValue("@serverUri", bookFile.ServerUri);
            command.Parameters.AddWithValue("@fileName", bookFile.FileName);
            command.Parameters.AddWithValue("@fileSizeBytes", bookFile.FileSizeBytes);
            command.Parameters.AddWithValue("@pageCount", bookFile.PageCount);
            command.Parameters.AddWithValue("@contentHash", bookFile.ContentHash);
            command.Parameters.AddWithValue("@coverImageUri", bookFile.CoverImageUri);
            command.Parameters.AddWithValue("@addedAt", bookFile.AddedAt);
            command.Parameters.AddWithValue("@available", bookFile.Available);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private readonly string _projectRoot;
        private readonly string _booksFolderPath;
        private readonly string _imagesFolderPath;
        private readonly Dictionary<int, (string Author, string Title)> _bookMetadata;

        public MainWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            
            // Пути к папкам с книгами и обложками
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Пробуем разные способы найти корневую папку проекта
            string? projectRoot = null;
            
            // Способ 1: Ищем папку LIB в пути
            var libIndex = baseDir.IndexOf("LIB", StringComparison.OrdinalIgnoreCase);
            if (libIndex >= 0)
            {
                projectRoot = baseDir.Substring(0, libIndex + 3);
            }
            else
            {
                // Способ 2: Используем относительный путь от исполняемого файла
                var currentDir = Directory.GetCurrentDirectory();
                if (currentDir.Contains("LIB", StringComparison.OrdinalIgnoreCase))
                {
                    libIndex = currentDir.IndexOf("LIB", StringComparison.OrdinalIgnoreCase);
                    if (libIndex >= 0)
                    {
                        projectRoot = currentDir.Substring(0, libIndex + 3);
                    }
                }
                else
                {
                    // Способ 3: Пробуем найти корень репозитория
                    var dir = new DirectoryInfo(baseDir);
                    while (dir != null)
                    {
                        if (dir.Name.Equals("LIB", StringComparison.OrdinalIgnoreCase))
                        {
                            projectRoot = dir.FullName;
                            break;
                        }
                        dir = dir.Parent;
                    }
                }
            }
            
            // Если не нашли, используем fallback
            if (string.IsNullOrEmpty(projectRoot))
            {
                projectRoot = baseDir;
            }
            
            _projectRoot = projectRoot;
            _booksFolderPath = System.IO.Path.Combine(projectRoot, "DB", "ServerBooks", "Books");
            _imagesFolderPath = System.IO.Path.Combine(projectRoot, "DB", "ServerBooks", "Images");
            
            // Загружаем метаданные книг
            _bookMetadata = LoadBookMetadata();
            
            // Логируем пути для отладки после инициализации компонентов
            Loaded += (s, e) => LogInitialization();
        }

        private void LogInitialization()
        {
            // Логируем пути для отладки
            AppendLog($"BaseDir: {AppDomain.CurrentDomain.BaseDirectory}");
            AppendLog($"BooksPath: {_booksFolderPath}");
            AppendLog($"ImagesPath: {_imagesFolderPath}");
            AppendLog($"Books exists: {Directory.Exists(_booksFolderPath)}");
            AppendLog($"Images exists: {Directory.Exists(_imagesFolderPath)}");
            AppendLog($"Metadata loaded: {_bookMetadata.Count} books");
        }

        private Dictionary<int, (string Author, string Title)> LoadBookMetadata()
        {
            var metadata = new Dictionary<int, (string, string)>();
            var metadataFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_booksFolderPath)!, "Текстовый документ.txt");
            
            if (!File.Exists(metadataFile))
            {
                AppendLog("Файл с метаданными не найден!");
                return metadata;
            }

            var lines = File.ReadAllLines(metadataFile);
            foreach (var line in lines)
            {
                // Парсим строки вида "1.  Александр Пушкин - "Евгений Онегин""
                var match = Regex.Match(line, @"^(\d+)\.\s+(.+?)\s+-\s+""(.+?)""");
                if (match.Success)
                {
                    var id = int.Parse(match.Groups[1].Value);
                    var author = match.Groups[2].Value.Trim();
                    var title = match.Groups[3].Value.Trim();
                    metadata[id] = (author, title);
                }
            }
            
            return metadata;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadButton.IsEnabled = false;
            StatusText.Text = "Проверка подключения к БД...";
            LogItems.Items.Clear();
            CurrentItemText.Text = string.Empty;
            OverallProgress.Value = 0;

            try
            {
                // Проверяем подключение к базе данных
                if (!await _databaseService.TestConnectionAsync())
                {
                    StatusText.Text = "Ошибка подключения к БД";
                    AppendLog("Не удается подключиться к базе данных. Проверьте настройки подключения.");
                    return;
                }

                // Проверяем существование папок
                if (!Directory.Exists(_booksFolderPath))
                {
                    AppendLog($"Папка с книгами не найдена: {_booksFolderPath}");
                    StatusText.Text = "Папка с книгами не найдена";
                    return;
                }

                if (!Directory.Exists(_imagesFolderPath))
                {
                    AppendLog($"Папка с обложками не найдена: {_imagesFolderPath}");
                    StatusText.Text = "Папка с обложками не найдена";
                    return;
                }

                StatusText.Text = "Чтение файлов...";

                // Получаем список всех файлов книг
                var bookFiles = Directory.GetFiles(_booksFolderPath, "*.fb2")
                    .Select(f => new { FileName = f, Number = ExtractNumber(System.IO.Path.GetFileName(f)) })
                    .Where(f => f.Number.HasValue)
                    .OrderBy(f => f.Number)
                    .ToList();

                int total = bookFiles.Count;
                if (total == 0)
                {
                    StatusText.Text = "Книги не найдены";
                    AppendLog("В папке Books не найдено файлов .fb2");
                    return;
                }

                int completed = 0;
                StatusText.Text = $"Обработка {total} книг...";

                foreach (var bookFile in bookFiles)
                {
                    int bookNumber = bookFile.Number!.Value;
                    CurrentItemText.Text = $"Обрабатывается: {bookNumber}.fb2";

                    try
                    {
                        // Получаем метаданные книги
                        string author = "Неизвестный автор";
                        string title = $"Книга {bookNumber}";

                        if (_bookMetadata.TryGetValue(bookNumber, out var meta))
                        {
                            author = meta.Author;
                            title = meta.Title;
                        }

                        AppendLog($"Обработка: {author} - {title}");

                        // Создаем объект книги
                        var book = new Book
                        {
                            Title = title,
                            Author = author,
                            Language = "ru",
                            Description = $"Добавлено из локальной библиотеки"
                        };

                        // Добавляем книгу в базу данных
                        int bookId = await _databaseService.AddBookAsync(book);
                        AppendLog($"Книга добавлена в БД (ID: {bookId})");

                        // Получаем информацию о файле
                        var fileInfo = new FileInfo(bookFile.FileName);
                        string contentHash = await CalculateFileHash(bookFile.FileName);

                        // Создаем относительные пути от корня проекта
                        string relativeBookPath = System.IO.Path.GetRelativePath(_projectRoot, bookFile.FileName).Replace('\\', '/');
                        
                        // Путь к обложке
                        string coverPath = System.IO.Path.Combine(_imagesFolderPath, $"{bookNumber}.jpg");
                        string relativeCoverPath = File.Exists(coverPath) 
                            ? System.IO.Path.GetRelativePath(_projectRoot, coverPath).Replace('\\', '/') 
                            : string.Empty;

                        // Создаем объект файла книги
                        var bf = new BookFile
                        {
                            BookId = bookId,
                            Format = "fb2",
                            SourceType = "server",
                            LocalPath = relativeBookPath,  // Относительный путь для server
                            ServerUri = relativeBookPath,  // URI для доступа через сервер
                            FileName = System.IO.Path.GetFileName(bookFile.FileName),
                            FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
                            ContentHash = contentHash,
                            CoverImageUri = relativeCoverPath,  // Относительный путь к обложке
                            AddedAt = DateTime.Now,
                            Available = true
                        };

                        // Добавляем файл книги в базу данных
                        await _databaseService.AddBookFileAsync(bf);
                        AppendLog($"Файл добавлен в БД");

                        completed++;
                        OverallProgress.Value = completed * 100.0 / total;
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"Ошибка обработки {bookNumber}.fb2: {ex.Message}");
                    }
                }

                StatusText.Text = "Завершено";
                CurrentItemText.Text = $"Обработано {completed} из {total} книг";
                AppendLog($"\n=== Завершено успешно ===");
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Сбой: {ex.Message}";
                AppendLog($"Критическая ошибка: {ex.Message}");
            }
            finally
            {
                DownloadButton.IsEnabled = true;
            }
        }

        private int? ExtractNumber(string fileName)
        {
            var match = Regex.Match(fileName, @"^(\d+)\.fb2$");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return null;
        }

        private async Task<string> CalculateFileHash(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return string.Empty;

                using var stream = File.OpenRead(filePath);
                using var sha256 = SHA256.Create();
                var hash = await Task.Run(() => sha256.ComputeHash(stream));
                return Convert.ToHexString(hash);
            }
            catch
            {
                return string.Empty;
            }
        }

        private void AppendLog(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogItems.Items.Add(new TextBlock { Text = $"[{DateTime.Now:HH:mm:ss}] {message}" });
            });
        }
    }
}