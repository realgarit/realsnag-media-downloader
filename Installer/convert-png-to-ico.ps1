Add-Type -AssemblyName System.Drawing

$pngPath = "realsnag-media-downloader.png"
$icoPath = "realsnag-media-downloader.ico"

if (-not (Test-Path $pngPath)) {
    Write-Error "PNG file not found: $pngPath"
    exit 1
}

try {
    $originalImage = [System.Drawing.Image]::FromFile((Resolve-Path $pngPath))
    
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $bitmaps = @()
    
    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        
        $graphics.DrawImage($originalImage, 0, 0, $size, $size)
        
        $bitmaps += $bitmap
        $graphics.Dispose()
    }
    
    $highResBitmap = $bitmaps[-1]  # Use the 256x256 version
    
    $icon = [System.Drawing.Icon]::FromHandle($highResBitmap.GetHicon())
    $fileStream = New-Object System.IO.FileStream($icoPath, [System.IO.FileMode]::Create)
    $icon.Save($fileStream)
    $fileStream.Close()
    
    $originalImage.Dispose()
    foreach ($bitmap in $bitmaps) {
        $bitmap.Dispose()
    }
    $icon.Dispose()
    $fileStream.Dispose()
    
    Write-Host "Successfully created ICO file: $icoPath"
    Write-Host "ICO file size: $((Get-Item $icoPath).Length) bytes"
    
} catch {
    Write-Error "Failed to create ICO file: $($_.Exception.Message)"
    exit 1
}
