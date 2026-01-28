<?php
/**
 * Minimal PDF generator for reports (single page, text-only).
 * Uses TCPDF when available for full UTF-8 / Cyrillic support.
 *
 * Installation (choose one):
 *   1. Composer: run "composer install" in WEB directory.
 *   2. Manual:   download TCPDF from https://github.com/tecnickcom/TCPDF/releases,
 *               extract to WEB/php/lib/TCPDF/ (so that tcpdf.php exists at lib/TCPDF/tcpdf.php).
 */

/**
 * Load TCPDF if available (Composer or manual).
 *
 * @return string|null Path to tcpdf.php if found, null otherwise.
 */
function spdf_find_tcpdf() {
    $lib = __DIR__;
    $web = dirname(dirname($lib));

    $candidates = [
        $lib . DIRECTORY_SEPARATOR . 'TCPDF' . DIRECTORY_SEPARATOR . 'tcpdf.php',
        $web . DIRECTORY_SEPARATOR . 'vendor' . DIRECTORY_SEPARATOR . 'tecnickcom' . DIRECTORY_SEPARATOR . 'tcpdf' . DIRECTORY_SEPARATOR . 'tcpdf.php',
    ];

    foreach ($candidates as $path) {
        if (is_file($path)) {
            return $path;
        }
    }

    return null;
}

/**
 * Generate PDF via TCPDF (UTF-8 / Cyrillic).
 *
 * @param string $title
 * @param array  $lines
 * @return string PDF bytes
 */
function spdf_generate_with_tcpdf($title, $lines) {
    $tcpdf_path = spdf_find_tcpdf();
    if (!$tcpdf_path) {
        throw new RuntimeException(
            'TCPDF not found. Install it via "composer install" in WEB or extract TCPDF to WEB/php/lib/TCPDF/. ' .
            'See WEB/php/lib/simple_pdf.php header for details.'
        );
    }

    require_once $tcpdf_path;

    $pdf = new TCPDF(PDF_PAGE_ORIENTATION, PDF_UNIT, PDF_PAGE_FORMAT, true, 'UTF-8', false);
    $pdf->SetCreator('Paradise Library');
    $pdf->SetTitle($title);
    $pdf->SetAutoPageBreak(false);
    $pdf->SetMargins(50, 40, 50);
    $pdf->SetFont('dejavusans', '', 12);

    $pdf->AddPage();
    $y = 40;
    $lineHeight = 8;
    $yMax = 270;

    $pdf->SetXY(50, $y);
    $pdf->Cell(0, $lineHeight, $title, 0, 1, 'L', false, '', 0, false, 'L', 'T');
    $y += $lineHeight * 2;

    foreach ($lines as $line) {
        if ($y > $yMax) {
            break;
        }
        $pdf->SetXY(50, $y);
        $pdf->Cell(0, $lineHeight, $line, 0, 1, 'L', false, '', 0, false, 'L', 'T');
        $y += $lineHeight;
    }

    return $pdf->Output('', 'S');
}

/**
 * Returns PDF bytes. Uses TCPDF for full UTF-8 / Cyrillic support.
 * TCPDF must be installed (Composer or manual). Throws if not found.
 *
 * @param string   $title
 * @param string[] $lines
 * @return string PDF bytes
 */
function simple_pdf_from_lines($title, $lines) {
    $lines = is_array($lines) ? $lines : [];
    return spdf_generate_with_tcpdf($title, $lines);
}
