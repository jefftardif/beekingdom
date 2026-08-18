Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile('C:\projets\beekingdom\images exemples\ruche\upscale_4096\HiveMap_4096_SharpLight.png')
$w = $img.Width
$h = $img.Height
$a = [math]::Round($w/$h, 3)
Write-Host "Width: $w Height: $h Aspect: $a"
$img.Dispose()