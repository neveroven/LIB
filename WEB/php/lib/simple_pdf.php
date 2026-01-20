<?php
// Very small PDF generator (single page, text-only).
// Supports basic Latin/Cyrillic only if client has font support; for robust Cyrillic you need embedded fonts.
// Here we keep it minimal: values are transliterated-safe by stripping control chars.

function spdf_escape($s) {
    $s = (string)$s;
    $s = preg_replace('/[\x00-\x08\x0B\x0C\x0E-\x1F]/u', '', $s);
    $s = str_replace(['\\', '(', ')'], ['\\\\', '\\(', '\\)'], $s);
    return $s;
}

// Returns PDF bytes.
function simple_pdf_from_lines($title, $lines) {
    $title = spdf_escape($title);
    $lines = is_array($lines) ? $lines : [];

    $yStart = 800;
    $lineHeight = 14;
    $x = 50;

    $content = "BT\n/F1 12 Tf\n";
    $y = $yStart;
    $content .= sprintf("1 0 0 1 %d %d Tm\n", $x, $y);
    $content .= "(" . spdf_escape($title) . ") Tj\n";

    $y -= ($lineHeight * 2);
    foreach ($lines as $line) {
        if ($y < 60) break; // single page limit
        $content .= sprintf("1 0 0 1 %d %d Tm\n", $x, $y);
        $content .= "(" . spdf_escape($line) . ") Tj\n";
        $y -= $lineHeight;
    }
    $content .= "ET\n";

    $objects = [];
    $objects[] = "<< /Type /Catalog /Pages 2 0 R >>";
    $objects[] = "<< /Type /Pages /Kids [3 0 R] /Count 1 >>";
    $objects[] = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>";
    $objects[] = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";
    $objects[] = "<< /Length " . strlen($content) . " >>\nstream\n" . $content . "endstream";

    $pdf = "%PDF-1.4\n";
    $xref = [];
    for ($i = 0; $i < count($objects); $i++) {
        $xref[$i + 1] = strlen($pdf);
        $pdf .= ($i + 1) . " 0 obj\n" . $objects[$i] . "\nendobj\n";
    }

    $xrefPos = strlen($pdf);
    $pdf .= "xref\n0 " . (count($objects) + 1) . "\n";
    $pdf .= "0000000000 65535 f \n";
    for ($i = 1; $i <= count($objects); $i++) {
        $pdf .= sprintf("%010d 00000 n \n", $xref[$i]);
    }
    $pdf .= "trailer\n<< /Size " . (count($objects) + 1) . " /Root 1 0 R >>\n";
    $pdf .= "startxref\n" . $xrefPos . "\n%%EOF";

    return $pdf;
}


