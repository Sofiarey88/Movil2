Add-Type -AssemblyName System.Drawing

New-Item -ItemType Directory -Force -Path 'Assets/AR/Images' | Out-Null
New-Item -ItemType Directory -Force -Path 'PrintableTargets' | Out-Null

function Generate-TargetAlpha {
    param([string]$path)
    $bmp = New-Object System.Drawing.Bitmap 1024, 1024
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::White)

    $blackBrush = [System.Drawing.Brushes]::Black
    $whiteBrush = [System.Drawing.Brushes]::White
    $blackPen = New-Object System.Drawing.Pen([System.Drawing.Color]::Black, 28)
    $thickPen = New-Object System.Drawing.Pen([System.Drawing.Color]::Black, 40)
    $thinPen = New-Object System.Drawing.Pen([System.Drawing.Color]::Black, 14)

    # Outer border
    $g.DrawRectangle($thickPen, 40, 40, 944, 944)
    $g.DrawRectangle($thinPen, 75, 75, 874, 874)

    # 4 Corner Fiducial Markers
    $g.FillRectangle($blackBrush, 95, 95, 140, 140)
    $g.FillRectangle($whiteBrush, 125, 125, 80, 80)
    $g.FillRectangle($blackBrush, 145, 145, 40, 40)

    $g.FillRectangle($blackBrush, 789, 95, 140, 140)
    $g.FillRectangle($whiteBrush, 819, 125, 80, 80)
    $g.FillRectangle($blackBrush, 839, 145, 40, 40)

    $g.FillRectangle($blackBrush, 95, 789, 140, 140)
    $g.FillRectangle($whiteBrush, 125, 819, 80, 80)
    $g.FillRectangle($blackBrush, 145, 839, 40, 40)

    $g.FillRectangle($blackBrush, 789, 789, 140, 140)
    $g.FillEllipse($whiteBrush, 809, 809, 100, 100)
    $g.FillRectangle($blackBrush, 839, 839, 40, 40)

    # Central geometric patterns (Target Alpha - Cyber Core)
    $g.DrawEllipse($thickPen, 262, 262, 500, 500)
    $g.DrawEllipse($thinPen, 312, 312, 400, 400)
    $g.FillPie($blackBrush, 362, 362, 300, 300, 0, 45)
    $g.FillPie($blackBrush, 362, 362, 300, 300, 90, 45)
    $g.FillPie($blackBrush, 362, 362, 300, 300, 180, 45)
    $g.FillPie($blackBrush, 362, 362, 300, 300, 270, 45)
    $g.FillEllipse($whiteBrush, 437, 437, 150, 150)
    $g.FillEllipse($blackBrush, 477, 477, 70, 70)

    # Feature blocks (dots and bars for ORB feature matching)
    for ($i = 0; $i -lt 8; $i++) {
        $x = 280 + ($i * 60)
        $g.FillRectangle($blackBrush, $x, 180, 35, 35)
        $g.FillRectangle($blackBrush, $x, 810, 35, 35)
    }
    for ($j = 0; $j -lt 8; $j++) {
        $y = 280 + ($j * 60)
        $g.FillRectangle($blackBrush, 180, $y, 35, 35)
        $g.FillRectangle($blackBrush, 810, $y, 35, 35)
    }

    # Text markings
    $font = New-Object System.Drawing.Font('Arial', 30, [System.Drawing.FontStyle]::Bold)
    $smallFont = New-Object System.Drawing.Font('Arial', 20, [System.Drawing.FontStyle]::Bold)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString('TARGET ALPHA :: CYBER CORE', $font, $blackBrush, 512, 105, $format)
    $g.DrawString('UAI - SIMULACIONES AR LAB', $smallFont, $blackBrush, 512, 880, $format)

    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Output "Generated $path"
}

function Generate-TargetBeta {
    param([string]$path)
    $bmp = New-Object System.Drawing.Bitmap 1024, 1024
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::White)

    $blackBrush = [System.Drawing.Brushes]::Black
    $whiteBrush = [System.Drawing.Brushes]::White
    $blackPen = New-Object System.Drawing.Pen([System.Drawing.Color]::Black, 28)
    $thickPen = New-Object System.Drawing.Pen([System.Drawing.Color]::Black, 40)
    $thinPen = New-Object System.Drawing.Pen([System.Drawing.Color]::Black, 14)

    # Outer border
    $g.DrawRectangle($thickPen, 40, 40, 944, 944)
    $g.DrawRectangle($thinPen, 75, 75, 874, 874)

    # 4 Corner Diamond Markers
    function DrawDiamond($cx, $cy, $size) {
        $half = $size / 2
        $pts = @(
            (New-Object System.Drawing.PointF ($cx), ($cy - $half)),
            (New-Object System.Drawing.PointF ($cx + $half), ($cy)),
            (New-Object System.Drawing.PointF ($cx), ($cy + $half)),
            (New-Object System.Drawing.PointF ($cx - $half), ($cy))
        )
        $g.FillPolygon($blackBrush, $pts)
    }

    DrawDiamond 165 165 150
    DrawDiamond 859 165 150
    DrawDiamond 165 859 150
    DrawDiamond 859 859 150

    $g.FillEllipse($whiteBrush, 140, 140, 50, 50)
    $g.FillEllipse($whiteBrush, 834, 140, 50, 50)
    $g.FillEllipse($whiteBrush, 140, 834, 50, 50)
    $g.FillRectangle($whiteBrush, 839, 839, 40, 40)

    # Central geometric patterns (Target Beta - Quantum Beacon - Hexagonal & Triangles)
    $radius = 240
    $hexPts = @()
    for ($k = 0; $k -lt 6; $k++) {
        $angle = $k * 60 * [Math]::PI / 180.0
        $hx = 512 + ($radius * [Math]::Cos($angle))
        $hy = 512 + ($radius * [Math]::Sin($angle))
        $hexPts += (New-Object System.Drawing.PointF $hx, $hy)
    }
    $g.DrawPolygon($thickPen, $hexPts)

    $innerRadius = 150
    $innerPts = @()
    for ($k = 0; $k -lt 6; $k++) {
        $angle = ($k * 60 + 30) * [Math]::PI / 180.0
        $hx = 512 + ($innerRadius * [Math]::Cos($angle))
        $hy = 512 + ($innerRadius * [Math]::Sin($angle))
        $innerPts += (New-Object System.Drawing.PointF $hx, $hy)
    }
    $g.FillPolygon($blackBrush, $innerPts)
    $g.FillEllipse($whiteBrush, 452, 452, 120, 120)
    $g.FillEllipse($blackBrush, 482, 482, 60, 60)

    # Feature blocks
    for ($c = 0; $c -lt 6; $c++) {
        $angle = $c * 60 * [Math]::PI / 180.0
        $bx = 512 + (310 * [Math]::Cos($angle))
        $by = 512 + (310 * [Math]::Sin($angle))
        $g.FillRectangle($blackBrush, ($bx - 20), ($by - 20), 40, 40)
    }

    # Text markings
    $font = New-Object System.Drawing.Font('Arial', 30, [System.Drawing.FontStyle]::Bold)
    $smallFont = New-Object System.Drawing.Font('Arial', 20, [System.Drawing.FontStyle]::Bold)
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString('TARGET BETA :: QUANTUM BEACON', $font, $blackBrush, 512, 105, $format)
    $g.DrawString('UAI - SIMULACIONES AR LAB', $smallFont, $blackBrush, 512, 880, $format)

    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Output "Generated $path"
}

Generate-TargetAlpha 'Assets/AR/Images/Target_Alpha.png'
Generate-TargetAlpha 'PrintableTargets/Target_Alpha.png'
Generate-TargetBeta 'Assets/AR/Images/Target_Beta.png'
Generate-TargetBeta 'PrintableTargets/Target_Beta.png'
