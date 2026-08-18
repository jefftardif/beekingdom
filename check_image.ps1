Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile('Assets/Experiments/Environment2D5D/Textures/ReferenceImage.png')
$w = $img.Width
$h = $img.Height
$a = [math]::Round($w/$h, 3)
Write-Host "Width: $w Height: $h Aspect: $a"
$img.Dispose()