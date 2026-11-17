# Sobe o SmartGrão inteiro e abre o navegador. Dê dois cliques em start.cmd, na raiz.
#
# São três programas: o serviço de análise (Python, porta 8000), o frontend (porta 5173) e a API
# (porta 5148). Os dois primeiros rodam escondidos; a API fica nesta janela, mostrando o log.
# Fechar esta janela (ou Ctrl+C) derruba os três.
#
# Se algum já estiver no ar (alguém subiu à mão num terminal), ele é reaproveitado.

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Fail($text) {
    Write-Host ''
    Write-Host "PROBLEMA: $text" -ForegroundColor Red
    Write-Host ''
    Read-Host 'Aperte Enter para fechar'
    exit 1
}

function PortInUse($port) {
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $client.ConnectAsync('localhost', $port).Wait(300) | Out-Null
        return $client.Connected
    } catch {
        return $false
    } finally {
        $client.Dispose()
    }
}

$service = Join-Path $root 'analytics\service'
$venvPython = Join-Path $service '.venv\Scripts\python.exe'
$vite = Join-Path $root 'frontend\node_modules\vite\bin\vite.js'
$settings = Join-Path $root 'backend\src\SmartGrao.WebApi\appsettings.Development.json'

# Falta preparo: em vez de um erro críptico lá na frente, roda o setup primeiro.
if (-not (Test-Path $venvPython) -or -not (Test-Path $vite) -or -not (Test-Path $settings)) {
    Write-Host 'Primeira vez nesta máquina: preparando antes de abrir...' -ForegroundColor Yellow
    $env:SMARTGRAO_NO_PAUSE = '1'
    & (Join-Path $PSScriptRoot 'setup.ps1')
    if (-not (Test-Path $venvPython) -or -not (Test-Path $vite) -or -not (Test-Path $settings)) {
        Fail 'O preparo não terminou. Rode setup.cmd e leia a mensagem de erro.'
    }
}

$started = @()
try {
    if (PortInUse 8000) {
        Write-Host 'Serviço de análise já está no ar (porta 8000).'
    } else {
        Write-Host 'Subindo o serviço de análise (porta 8000)...'
        $started += Start-Process -FilePath $venvPython -WorkingDirectory $service -WindowStyle Hidden -PassThru `
            -ArgumentList '-m', 'uvicorn', 'smartgrao_analytics.app:app', '--port', '8000'
    }

    if (PortInUse 5173) {
        Write-Host 'Frontend já está no ar (porta 5173).'
    } else {
        Write-Host 'Subindo o frontend (porta 5173)...'
        $started += Start-Process -FilePath 'node' -WorkingDirectory (Join-Path $root 'frontend') -WindowStyle Hidden -PassThru `
            -ArgumentList "`"$vite`""
    }

    # Abre o navegador assim que a API responder, sem segurar esta janela.
    Start-Job -ScriptBlock {
        for ($i = 0; $i -lt 120; $i++) {
            try {
                Invoke-WebRequest -Uri 'http://localhost:5148/api/farms' -UseBasicParsing -TimeoutSec 2 | Out-Null
                Start-Process 'http://localhost:5173'
                return
            } catch {
                Start-Sleep -Seconds 1
            }
        }
    } | Out-Null

    Write-Host ''
    Write-Host 'Subindo a API (porta 5148). O navegador abre sozinho quando tudo estiver pronto.' -ForegroundColor Green
    Write-Host 'Para desligar o sistema, feche esta janela.' -ForegroundColor Green
    Write-Host ''

    # Só HTTP: o perfil padrão também abre HTTPS, que exige um certificado de desenvolvimento que
    # máquina nova não tem — e falha com uma mensagem que ninguém entende.
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = 'http://localhost:5148'
    dotnet run --project (Join-Path $root 'backend\src\SmartGrao.WebApi') --no-launch-profile
} finally {
    foreach ($process in $started) {
        if ($process -and -not $process.HasExited) {
            # /T derruba também os filhos (o uvicorn e o node podem abrir subprocessos).
            taskkill /T /F /PID $process.Id 2>$null | Out-Null
        }
    }
    Get-Job | Remove-Job -Force -ErrorAction SilentlyContinue
}
