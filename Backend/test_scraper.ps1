$ErrorActionPreference = 'Stop'
$codigo = "202512445"

$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$session.UserAgent = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120'

# PASO 1: Login
Write-Host "`n=== PASO 1: Login ===" -ForegroundColor Cyan
$body = "USER=medicina&PASS=biblioteca1&DH=/abnet"
$r1 = Invoke-WebRequest -Uri 'https://biblioteca.urp.edu.pe/abnet/abnetcl.exe' -Method POST -Body $body -ContentType 'application/x-www-form-urlencoded' -WebSession $session -MaximumRedirection 5
Write-Host "Status: $($r1.StatusCode)"
$html1 = $r1.Content
Write-Host "HTML (primeros 600 chars):"
Write-Host $html1.Substring(0, [Math]::Min(600, $html1.Length))

# Buscar UD path
if ($html1 -match 'abnetcl\.exe(/X\d+/UD\w+)') {
    $udPath = $Matches[1]
    Write-Host "`n>>> UD Path: $udPath" -ForegroundColor Green
} else {
    Write-Host "`n>>> NO SE ENCONTRO UD PATH - abortando" -ForegroundColor Red
    exit 1
}

# PASO 2: Sesion definitiva
Write-Host "`n=== PASO 2: Session ID ===" -ForegroundColor Cyan
$r2 = Invoke-WebRequest -Uri "https://biblioteca.urp.edu.pe/abnet/abnetcl.exe${udPath}?ACC=1111" -WebSession $session -MaximumRedirection 5
$html2 = $r2.Content
Write-Host "HTML (primeros 600 chars):"
Write-Host $html2.Substring(0, [Math]::Min(600, $html2.Length))

if ($html2 -match 'abnetcl\.exe(/X\d+/ID\w+/)') {
    $sid = $Matches[1]
    Write-Host "`n>>> Session ID: $sid" -ForegroundColor Green
} else {
    Write-Host "`n>>> NO SE ENCONTRO SESSION ID - abortando" -ForegroundColor Red
    exit 1
}

# PASO 3: Buscar lector
Write-Host "`n=== PASO 3: Busqueda de lector: $codigo ===" -ForegroundColor Cyan
$encodedCodigo = [Uri]::EscapeDataString($codigo)
$searchUrl = "https://biblioteca.urp.edu.pe/abnet/abnetcl.exe${sid}NT119?ACC=110&NV=1&AV=1&TBV=2&SF=NUM_LECTOR&SFT=CLAVE_BARRAS&TQ=$encodedCodigo"
Write-Host "URL: $searchUrl"
$r3 = Invoke-WebRequest -Uri $searchUrl -WebSession $session -MaximumRedirection 5
$html3 = $r3.Content
Write-Host "HTML (primeros 800 chars):"
Write-Host $html3.Substring(0, [Math]::Min(800, $html3.Length))

if ($html3 -match 'abnetcl\.exe(/X\d+/ID\w+/NT\d+)') {
    $ntPath = $Matches[1]
    Write-Host "`n>>> NT Path: $ntPath" -ForegroundColor Green
} else {
    Write-Host "`n>>> NO SE ENCONTRO NT PATH" -ForegroundColor Red
    Write-Host "HTML completo:"
    Write-Host $html3
    exit 1
}

# PASO 4: Frameset
Write-Host "`n=== PASO 4: Frameset ===" -ForegroundColor Cyan
$r4 = Invoke-WebRequest -Uri "https://biblioteca.urp.edu.pe/abnet/abnetcl.exe${ntPath}?ACC=1111" -WebSession $session -MaximumRedirection 5
$html4 = $r4.Content
Write-Host "HTML completo del frameset:"
Write-Host $html4

if ($html4 -match "WpGetFrameset\('/abnet/abnetcl\.exe(/X\d+/ID\w+/NT(\d+))'\)") {
    $ntFull = $Matches[1]
    $ntNum = $Matches[2]
    Write-Host "`n>>> NT Ficha num: $ntNum" -ForegroundColor Green
} else {
    Write-Host "`n>>> NO SE ENCONTRO FRAMESET DE DATOS" -ForegroundColor Red
    exit 1
}

# PASO 5: Ficha del lector
Write-Host "`n=== PASO 5: Ficha del lector ===" -ForegroundColor Cyan
$fichaUrl = "https://biblioteca.urp.edu.pe/abnet/abnetcl.exe${sid}NT${ntNum}?ACC=104"
Write-Host "URL: $fichaUrl"
$r5 = Invoke-WebRequest -Uri $fichaUrl -WebSession $session -MaximumRedirection 5
$html5 = $r5.Content
Write-Host "HTML completo de la ficha:"
Write-Host $html5
