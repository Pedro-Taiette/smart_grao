# A demonstração do SmartGrão em Docker. Dois cliques em demo.cmd: sobe tudo, abre o navegador e,
# quando você apertar Enter nesta janela, desliga. Só precisa do Docker Desktop.
#
# Para começar do zero (apaga o que foi feito na demo e volta ao starter pack): demo.cmd reset
#
# Nada daqui mexe no banco compartilhado (Neon). Ver docs/docker.md.
param([string]$Action = 'start')

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Fail($text) {
    Write-Host ''
    Write-Host "PROBLEMA: $text" -ForegroundColor Red
    Write-Host ''
    Read-Host 'Aperte Enter para fechar' | Out-Null
    exit 1
}

if (-not (Get-Command 'docker' -ErrorAction SilentlyContinue)) {
    Fail 'O Docker Desktop não está instalado. Baixe em https://www.docker.com/products/docker-desktop e rode de novo.'
}

# Docker instalado mas desligado: liga e espera, em vez de mostrar um erro que ninguém entende.
cmd /c "docker info >nul 2>&1"
if ($LASTEXITCODE -ne 0) {
    $desktop = Join-Path $env:ProgramFiles 'Docker\Docker\Docker Desktop.exe'
    if (Test-Path $desktop) {
        Write-Host 'Ligando o Docker Desktop...' -ForegroundColor Yellow
        Start-Process $desktop
    }
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 3
        cmd /c "docker info >nul 2>&1"
        if ($LASTEXITCODE -eq 0) { break }
    }
    if ($LASTEXITCODE -ne 0) { Fail 'O Docker não respondeu. Abra o Docker Desktop, espere ele dizer "Engine running" e rode de novo.' }
}

if ($Action -eq 'reset') {
    Write-Host 'Começando do zero: apagando os dados da demo...' -ForegroundColor Yellow
    docker compose down -v
}

Write-Host 'Subindo o SmartGrão (a primeira vez monta as imagens e leva alguns minutos)...' -ForegroundColor Cyan
docker compose up -d --build
if ($LASTEXITCODE -ne 0) { Fail 'Não deu para subir os containers. Veja a mensagem acima.' }

Write-Host 'Esperando o sistema responder...'
$ready = $false
for ($i = 0; $i -lt 100; $i++) {
    try {
        Invoke-WebRequest -Uri 'http://localhost:8080/api/farms' -UseBasicParsing -TimeoutSec 3 | Out-Null
        $ready = $true
        break
    } catch {
        Start-Sleep -Seconds 3
    }
}
if (-not $ready) { Fail 'O sistema não respondeu em 5 minutos. Rode "docker compose logs api" para ver o motivo.' }

Start-Process 'http://localhost:8080'
Write-Host ''
Write-Host 'SmartGrão no ar: http://localhost:8080' -ForegroundColor Green
# O adaptador com gateway e endereço de rede local é o Wi-Fi ou cabo de verdade; WSL e Hyper-V não
# têm gateway, e VPN costuma usar outra faixa.
$ip = (Get-NetIPConfiguration -ErrorAction SilentlyContinue |
    Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' } |
    ForEach-Object { $_.IPv4Address.IPAddress } |
    Where-Object { $_ -match '^(192\.168\.|10\.|172\.(1[6-9]|2\d|3[01])\.)' } |
    Select-Object -First 1)
if ($ip) { Write-Host "No celular, na mesma rede Wi-Fi: http://${ip}:8080" }
Write-Host ''

if ($env:SMARTGRAO_NO_PAUSE) { exit 0 }
Read-Host 'Aperte Enter para DESLIGAR o sistema' | Out-Null
docker compose stop
Write-Host 'Desligado. O que foi feito na demo fica guardado para a próxima vez.' -ForegroundColor Green
Start-Sleep -Seconds 2
