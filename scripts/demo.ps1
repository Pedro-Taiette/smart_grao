# A demonstração do SmartGrão em Docker: banco com os dados da demo, API, serviço de análise e
# frontend, tudo em containers. Não precisa de .NET, Node nem Python — só do Docker Desktop.
#
#   demo.cmd         sobe (a primeira vez constrói as imagens, alguns minutos) e abre o navegador
#   demo-stop.cmd    desliga, guardando o que foi feito
#   demo-reset.cmd   volta a demo ao estado inicial (apaga o que foi feito nela)
#
# Nada daqui mexe no banco compartilhado (Neon). Ver docs/docker.md.
param([ValidateSet('start', 'stop', 'reset')] [string]$Action = 'start')

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

# Docker instalado mas desligado: liga e espera, em vez de mostrar um erro de "pipe" que ninguém entende.
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

# Os dados da demo (nomes reais da equipe) ficam fora do repositorio publico e vem num zip a parte.
# Se o zip estiver na pasta do projeto, descompacta sozinho.
$seed = Join-Path $root 'docker\db\20-smartgrao-demo.sql.gz'
$package = Join-Path $root 'smartgrao-demo-data.zip'
if ($Action -ne 'stop' -and -not (Test-Path $seed)) {
    if (Test-Path $package) {
        Write-Host 'Descompactando os dados da demonstração...' -ForegroundColor Yellow
        try {
            Expand-Archive -Path $package -DestinationPath $root -Force
        } catch {
            Fail 'O arquivo smartgrao-demo-data.zip está corrompido ou incompleto. Baixe de novo e rode outra vez.'
        }
        if (-not (Test-Path $seed)) {
            Fail 'O smartgrao-demo-data.zip não tem os dados esperados (docker/db/20-smartgrao-demo.sql.gz). Peça o arquivo de novo.'
        }
        # Banco ja criado sem os dados: precisa recomecar para a copia entrar.
        $Action = 'reset'
    } else {
        Fail 'Faltam os dados da demonstração. Peça o arquivo smartgrao-demo-data.zip para a equipe, coloque na pasta do projeto (ao lado do demo.cmd) e rode de novo.'
    }
}

if ($Action -eq 'stop') {
    docker compose stop
    Write-Host 'SmartGrão desligado. O que foi feito na demo continua guardado.' -ForegroundColor Green
    exit 0
}

if ($Action -eq 'reset') {
    Write-Host 'Voltando a demo ao estado inicial...' -ForegroundColor Yellow
    docker compose down -v
}

Write-Host 'Subindo o SmartGrão (a primeira vez constrói as imagens e leva alguns minutos)...' -ForegroundColor Cyan
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
Write-Host 'Pronto: http://localhost:8080' -ForegroundColor Green
# O adaptador com gateway e endereco de rede local (192.168, 10, 172.16-31) e o Wi-Fi ou cabo de
# verdade; WSL e Hyper-V nao tem gateway, e VPN costuma usar outra faixa.
$ip = (Get-NetIPConfiguration -ErrorAction SilentlyContinue |
    Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' } |
    ForEach-Object { $_.IPv4Address.IPAddress } |
    Where-Object { $_ -match '^(192\.168\.|10\.|172\.(1[6-9]|2\d|3[01])\.)' } |
    Select-Object -First 1)
if ($ip) { Write-Host "No celular, na mesma rede: http://${ip}:8080" }
Write-Host 'Para desligar: demo-stop.cmd. Para voltar a demo ao início: demo-reset.cmd.'
Write-Host ''
if (-not $env:SMARTGRAO_NO_PAUSE) { Read-Host 'Aperte Enter para fechar esta janela (o sistema continua no ar)' | Out-Null }
