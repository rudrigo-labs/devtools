Param(
    [string]$OutPath = "Presentation/DevTools.Presentation.Wpf/Assets/application.ico",
    [string]$BgColor = "#1E1E1E",
    [string]$BorderColor = "#00D1B2",
    [string]$Text = "DT",
    [string]$TextColor = "#FFFFFF"
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

function New-RoundedRect {
    param([System.Drawing.Rectangle]$Rect, [int]$Radius)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $Radius * 2
    $path.AddArc($Rect.X, $Rect.Y, $d, $d, 180, 90)                       | Out-Null
    $path.AddArc($Rect.Right - $d, $Rect.Y, $d, $d, 270, 90)              | Out-Null
    $path.AddArc($Rect.Right - $d, $Rect.Bottom - $d, $d, $d, 0, 90)      | Out-Null
    $path.AddArc($Rect.X, $Rect.Bottom - $d, $d, $d, 90, 90)              | Out-Null
    $path.CloseFigure()                                                   | Out-Null
    return $path
}

function New-Png {
    param([int]$Size)
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = "AntiAlias"
    $g.TextRenderingHint = "AntiAlias"
    $g.Clear([System.Drawing.Color]::FromArgb(0,0,0,0))

    $radius = [int][Math]::Round($Size * 0.18)
    $margin = [int][Math]::Round($Size * 0.08)
    $rectWidth = [int]($Size - (2 * $margin))
    $rectHeight = [int]($Size - (2 * $margin))
    $rect = New-Object System.Drawing.Rectangle([int]$margin, [int]$margin, $rectWidth, $rectHeight)

    $bg = [System.Drawing.ColorTranslator]::FromHtml($BgColor)
    $border = [System.Drawing.ColorTranslator]::FromHtml($BorderColor)
    $fg = [System.Drawing.ColorTranslator]::FromHtml($TextColor)

    $path = New-RoundedRect -Rect $rect -Radius $radius
    $bgBrush = New-Object System.Drawing.SolidBrush($bg)
    $g.FillPath($bgBrush, $path)

    $pen = New-Object System.Drawing.Pen($border, [int]([Math]::Max(1, [Math]::Round($Size * 0.04))))
    $g.DrawPath($pen, $path)

    # Draw text
    $fontSize = [int][Math]::Round($Size * 0.46)
    try {
        $font = New-Object System.Drawing.Font("Segoe UI Black", $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    } catch {
        $font = New-Object System.Drawing.Font([System.Drawing.FontFamily]::GenericSansSerif, $fontSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    }
    $stringSize = $g.MeasureString($Text, $font)
    $tx = [float](($Size - $stringSize.Width) / 2)
    $ty = [float]((($Size - $stringSize.Height) / 2) - ($Size * 0.03))
    $sb = New-Object System.Drawing.SolidBrush($fg)
    $g.DrawString($Text, $font, $sb, $tx, $ty)

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $ms.ToArray()
    $g.Dispose(); $bmp.Dispose(); $ms.Dispose()
    return $bytes
}

function Write-Ico {
    param([string]$Path, [byte[][]]$Images)
    $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    $bw = New-Object System.IO.BinaryWriter($fs)
    # ICONDIR
    $bw.Write([UInt16]0)     # reserved
    $bw.Write([UInt16]1)     # type = icon
    $bw.Write([UInt16]$Images.Length)  # count

    $dirEntrySize = 16
    $offset = 6 + ($Images.Length * $dirEntrySize)

    # Prepare directory entries (we'll write after computing sizes)
    $entries = @()
    $sizes = @(256,128,64,48,32,16) | Select-Object -First $Images.Length
    for ($i=0; $i -lt $Images.Length; $i++) {
        $img = $Images[$i]
        $w = $sizes[$i]
        $h = $sizes[$i]
        $widthByte = if ($w -ge 256) { 0 } else { [byte]$w }
        $heightByte = if ($h -ge 256) { 0 } else { [byte]$h }
        $entry = New-Object psobject -Property @{
            Width = $widthByte
            Height = $heightByte
            Colors = 0
            Reserved = 0
            Planes = [UInt16]1
            BitCount = [UInt16]32
            Size = [UInt32]$img.Length
            Offset = [UInt32]$offset
            Data = $img
        }
        $entries += $entry
        $offset += $img.Length
    }

    foreach ($e in $entries) {
        $bw.Write([byte]$e.Width)     # width
        $bw.Write([byte]$e.Height)    # height
        $bw.Write([byte]$e.Colors)    # color count
        $bw.Write([byte]$e.Reserved)  # reserved
        $bw.Write([UInt16]$e.Planes)  # planes
        $bw.Write([UInt16]$e.BitCount)# bit count
        $bw.Write([UInt32]$e.Size)    # bytes in res
        $bw.Write([UInt32]$e.Offset)  # image offset
    }

    foreach ($e in $entries) {
        $bw.Write($e.Data)
    }
    $bw.Flush(); $bw.Dispose(); $fs.Dispose()
}

Write-Host "Gerando imagens..." -ForegroundColor Cyan
$sizes = 256,128,64,48,32,16
$imgs = @()
foreach ($s in $sizes) { $imgs += ,(New-Png -Size $s) }

$outFull = [System.IO.Path]::Combine((Get-Location).Path, $OutPath)
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outFull)) | Out-Null
Write-Host "Escrevendo ICO em $OutFull" -ForegroundColor Cyan
Write-Ico -Path $outFull -Images $imgs
Write-Host "Ícone gerado com sucesso." -ForegroundColor Green
