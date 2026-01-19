# Script pour creer une icone ICO pour l'application Collectivite

Add-Type -AssemblyName System.Drawing

try {
    # Creer plusieurs tailles pour l'icone (16, 32, 48, 256)
    $sizes = @(16, 32, 48, 256)
    $images = @()
    
    foreach ($size in $sizes) {
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        
        # Couleurs Material Design : DeepPurple et Lime
        $primaryColor = [System.Drawing.Color]::FromArgb(103, 58, 183)
        $secondaryColor = [System.Drawing.Color]::FromArgb(205, 220, 57)
        $white = [System.Drawing.Color]::White
        
        # Calculer les proportions selon la taille
        $scale = $size / 256.0
        $buildingX = 64 * $scale
        $buildingY = 120 * $scale
        $buildingWidth = 128 * $scale
        $buildingHeight = 96 * $scale
        $roofTopY = 64 * $scale
        
        # Corps du batiment
        $buildingBrush = New-Object System.Drawing.SolidBrush($primaryColor)
        $buildingRect = New-Object System.Drawing.RectangleF($buildingX, $buildingY, $buildingWidth, $buildingHeight)
        $graphics.FillRectangle($buildingBrush, $buildingRect)
        
        # Toit (triangle)
        $roofBrush = New-Object System.Drawing.SolidBrush($secondaryColor)
        $roofPoints = @(
            [System.Drawing.PointF]::new($buildingX, $buildingY),
            [System.Drawing.PointF]::new($size / 2, $roofTopY),
            [System.Drawing.PointF]::new($buildingX + $buildingWidth, $buildingY)
        )
        $graphics.FillPolygon($roofBrush, $roofPoints)
        
        # Porte (seulement pour les grandes tailles)
        if ($size -ge 32) {
            $doorWidth = 32 * $scale
            $doorHeight = 56 * $scale
            $doorX = ($size - $doorWidth) / 2
            $doorY = ($buildingY + $buildingHeight) - $doorHeight
            $doorRect = New-Object System.Drawing.RectangleF($doorX, $doorY, $doorWidth, $doorHeight)
            $graphics.FillRectangle($buildingBrush, $doorRect)
            
            # Fenetres (seulement pour les grandes tailles)
            if ($size -ge 48) {
                $windowBrush = New-Object System.Drawing.SolidBrush($white)
                $windowSize = 24 * $scale
                $windowY = $buildingY + (16 * $scale)
                
                $window1X = $buildingX + (16 * $scale)
                $window1Rect = New-Object System.Drawing.RectangleF($window1X, $windowY, $windowSize, $windowSize)
                $graphics.FillRectangle($windowBrush, $window1Rect)
                
                $window2X = ($buildingX + $buildingWidth) - (16 * $scale) - $windowSize
                $window2Rect = New-Object System.Drawing.RectangleF($window2X, $windowY, $windowSize, $windowSize)
                $graphics.FillRectangle($windowBrush, $window2Rect)
            }
        }
        
        $graphics.Dispose()
        $images += $bitmap
    }
    
    # Sauvegarder la plus grande taille comme PNG
    $images[$images.Count - 1].Save("app_icon_256.png", [System.Drawing.Imaging.ImageFormat]::Png)
    
    # Nettoyer
    foreach ($img in $images) {
        $img.Dispose()
    }
    
    Write-Host "Image creee: app_icon_256.png"
    Write-Host ""
    Write-Host "Pour creer le fichier ICO, vous pouvez:"
    Write-Host "1. Utiliser ImageMagick: magick convert app_icon_256.png -define icon:auto-resize=256,128,64,48,32,16 app.ico"
    Write-Host "2. Utiliser un convertisseur en ligne: https://convertio.co/png-ico/"
    Write-Host "3. Utiliser GIMP ou un autre editeur d images"
    
} catch {
    Write-Host "Erreur: $_" -ForegroundColor Red
    Write-Host "Assurez-vous que System.Drawing est disponible"
}
