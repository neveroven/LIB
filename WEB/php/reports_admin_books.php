<?php
session_start();
require_once 'db.php';
require_once __DIR__ . '/lib/simple_pdf.php';

if (empty($_SESSION['is_admin']) || !$_SESSION['is_admin']) {
    header('Location: login.php');
    exit();
}

// Запрос для получения книг, добавленных администратором
// Предполагаем, что книги с source_type='server' добавлены администратором
$query = "
    SELECT 
        bf.added_at as date_added,
        b.title,
        b.author,
        b.series as category,
        bf.format,
        bf.file_name
    FROM book_files bf
    JOIN books b ON bf.book_id = b.id
    WHERE bf.source_type = 'server'
    ORDER BY bf.added_at DESC
";

$result = mysqli_query($connect, $query);
$admin_books = [];

if ($result) {
    while ($row = mysqli_fetch_assoc($result)) {
        $admin_books[] = $row;
    }
} else {
    $error = "Ошибка выполнения запроса: " . mysqli_error($connect);
}

// === EXPORT ===
if (isset($_GET['export']) && $_GET['export'] === 'pdf') {
    try {
        // Используем TCPDF напрямую, чтобы сделать нормальную табличную верстку
        $tcpdf_path = spdf_find_tcpdf();
        if (!$tcpdf_path) {
            throw new RuntimeException(
                'TCPDF не найден. Установите его через "composer install" в каталоге WEB ' .
                'или распакуйте TCPDF в WEB/php/lib/TCPDF/.'
            );
        }

        require_once $tcpdf_path;

        $pdf = new TCPDF(PDF_PAGE_ORIENTATION, PDF_UNIT, PDF_PAGE_FORMAT, true, 'UTF-8', false);
        $pdf->SetCreator('Paradise Library');
        $pdf->SetTitle('Отчёт: Книги администратора');
        $pdf->SetMargins(15, 20, 15);
        $pdf->SetAutoPageBreak(true, 20);

        $pdf->AddPage();

        // Заголовок
        $pdf->SetFont('dejavusans', 'B', 16);
        $pdf->Cell(0, 10, 'Отчёт: Книги администратора', 0, 1, 'L');

        // Подзаголовок / дата
        $pdf->SetFont('dejavusans', '', 11);
        $pdf->Ln(2);
        $pdf->Cell(0, 6, 'Книги, добавленные администратором (source_type=server)', 0, 1, 'L');
        $pdf->Cell(0, 6, 'Дата формирования: ' . date('d.m.Y H:i'), 0, 1, 'L');
        $pdf->Ln(4);

        // Таблица
        $pdf->SetFont('dejavusans', '', 9);

        // Ширины колонок: №, Дата, Название, Автор, Категория, Формат, Файл
        $w = [8, 26, 55, 35, 30, 14, 32];

        // Заголовок таблицы
        $pdf->SetFillColor(52, 58, 64);   // тёмно-серый фон
        $pdf->SetTextColor(255, 255, 255);
        $pdf->SetDrawColor(200, 200, 200);
        $pdf->SetLineWidth(0.2);
        $pdf->SetFont('dejavusans', 'B', 9);

        $headers = ['№', 'Дата', 'Название', 'Автор', 'Категория', 'Формат', 'Файл'];
        foreach ($headers as $i => $h) {
            $pdf->Cell($w[$i], 7, $h, 1, 0, 'C', 1);
        }
        $pdf->Ln();

        // Строки таблицы
        $pdf->SetFont('dejavusans', '', 9);
        $pdf->SetTextColor(0, 0, 0);
        $fill = 0;
        $rowIndex = 1;

        foreach ($admin_books as $b) {
            $date = !empty($b['date_added']) ? date('d.m.Y H:i', strtotime($b['date_added'])) : '-';
            $title = $b['title'] ?? '';
            $author = !empty($b['author']) ? $b['author'] : 'Не указан';
            $cat = !empty($b['category']) ? $b['category'] : 'Без категории';
            $format = strtoupper($b['format'] ?? '');
            $fileName = $b['file_name'] ?? '';

            $pdf->SetFillColor($fill ? 245 : 255, $fill ? 245 : 255, $fill ? 245 : 255);

            $pdf->Cell($w[0], 6, $rowIndex, 1, 0, 'C', 1);
            $pdf->Cell($w[1], 6, $date, 1, 0, 'C', 1);
            $pdf->Cell($w[2], 6, $title, 1, 0, 'L', 1);
            $pdf->Cell($w[3], 6, $author, 1, 0, 'L', 1);
            $pdf->Cell($w[4], 6, $cat, 1, 0, 'L', 1);
            $pdf->Cell($w[5], 6, $format, 1, 0, 'C', 1);
            $pdf->Cell($w[6], 6, $fileName, 1, 0, 'L', 1);
            $pdf->Ln();

            $fill = !$fill;
            $rowIndex++;
        }

        $pdfContent = $pdf->Output('', 'S');

        header('Content-Type: application/pdf');
        header('Content-Disposition: attachment; filename="admin_books_' . date('Y-m-d') . '.pdf"');
        header('Content-Length: ' . strlen($pdfContent));
        echo $pdfContent;
    } catch (RuntimeException $e) {
        $_SESSION['pdf_error'] = $e->getMessage();
        header('Location: reports_admin_books.php');
        exit();
    }
    exit();
}
?>

<!DOCTYPE html>
<html lang="ru">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Отчёт: Книги администратора - Paradise Library Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.7.2/font/bootstrap-icons.css">
</head>
<body>
    <div class="container-fluid py-3">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2><i class="bi bi-book-fill"></i> Отчёт: Книги администратора</h2>
            <a class="btn btn-danger" href="?export=pdf">
                <i class="bi bi-file-earmark-pdf"></i> Скачать PDF
            </a>
        </div>

        <!-- Таблица отчёта -->
        <div class="card">
            <div class="card-header">
                <h5 class="mb-0">Книги, добавленные в библиотеку администратором</h5>
            </div>
            <div class="card-body">
                <?php if (!empty($error)): ?>
                    <div class="alert alert-danger"><?= htmlspecialchars($error) ?></div>
                <?php endif; ?>
                <?php if (!empty($_SESSION['pdf_error'])): ?>
                    <div class="alert alert-warning"><?= htmlspecialchars($_SESSION['pdf_error']) ?></div>
                    <?php unset($_SESSION['pdf_error']); ?>
                <?php endif; ?>
                
                <div class="table-responsive">
                    <table class="table table-striped table-hover">
                        <thead class="table-dark">
                            <tr>
                                <th>№</th>
                                <th>Дата добавления</th>
                                <th>Название</th>
                                <th>Автор</th>
                                <th>Категория (Серия)</th>
                                <th>Формат</th>
                                <th>Имя файла</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php if (!empty($admin_books)): ?>
                                <?php $index = 1; ?>
                                <?php foreach ($admin_books as $book): ?>
                                    <tr>
                                        <td><?= $index++ ?></td>
                                        <td>
                                            <?php if (!empty($book['date_added'])): ?>
                                                <?= date('d.m.Y H:i', strtotime($book['date_added'])) ?>
                                            <?php else: ?>
                                                <span class="text-muted">-</span>
                                            <?php endif; ?>
                                        </td>
                                        <td><strong><?= htmlspecialchars($book['title']) ?></strong></td>
                                        <td><?= htmlspecialchars($book['author'] ?: 'Не указан') ?></td>
                                        <td>
                                            <?php if (!empty($book['category'])): ?>
                                                <span class="badge bg-info"><?= htmlspecialchars($book['category']) ?></span>
                                            <?php else: ?>
                                                <span class="text-muted">-</span>
                                            <?php endif; ?>
                                        </td>
                                        <td>
                                            <span class="badge bg-secondary"><?= strtoupper(htmlspecialchars($book['format'])) ?></span>
                                        </td>
                                        <td>
                                            <small class="text-muted"><?= htmlspecialchars($book['file_name']) ?></small>
                                        </td>
                                    </tr>
                                <?php endforeach; ?>
                            <?php else: ?>
                                <tr>
                                    <td colspan="7" class="text-center text-muted">
                                        <i class="bi bi-inbox"></i> Нет данных
                                    </td>
                                </tr>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>

                <!-- Итоговая статистика -->
                <?php if (!empty($admin_books)): ?>
                    <div class="mt-4 p-3 bg-light rounded">
                        <h6>Итоговая статистика:</h6>
                        <div class="row">
                            <div class="col-md-4">
                                <strong>Всего книг:</strong> <?= count($admin_books) ?>
                            </div>
                            <div class="col-md-4">
                                <strong>Уникальных авторов:</strong> 
                                <?= count(array_unique(array_filter(array_column($admin_books, 'author')))) ?>
                            </div>
                            <div class="col-md-4">
                                <strong>Уникальных категорий:</strong> 
                                <?= count(array_unique(array_filter(array_column($admin_books, 'category')))) ?>
                            </div>
                        </div>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.1/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>

