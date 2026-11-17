# Prepara a máquina para rodar o SmartGrão. Rode uma vez depois do clone (ou dê dois cliques em
# setup.cmd, na raiz). Pode rodar de novo sem medo: cada passo pula o que já está feito.
#
# Passos: confere as ferramentas, o modelo (Git LFS), a conexão com o banco, instala as
# dependências do frontend e do serviço de análise e aplica as migrations.

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Step($text) { Write-Host ''; Write-Host "==> $text" -ForegroundColor Cyan }
function Ok($text) { Write-Host "    ok: $text" -ForegroundColor Green }
function Fail($text) {
    Write-Host ''
    Write-Host "    PROBLEMA: $text" -ForegroundColor Red
    Write-Host ''
    Read-Host 'Aperte Enter para fechar'
    exit 1
}

function Require($command, $name, $url) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        Fail "$name não está instalado. Baixe em $url, instale e rode este script de novo."
    }
    Ok "$name encontrado"
}

Step 'Conferindo as ferramentas'
Require 'dotnet' '.NET SDK 10' 'https://dotnet.microsoft.com/download'
Require 'node' 'Node.js 20+' 'https://nodejs.org'
Require 'npm' 'npm (vem com o Node.js)' 'https://nodejs.org'
$python = Get-Command 'python' -ErrorAction SilentlyContinue
if (-not $python) { $python = Get-Command 'py' -ErrorAction SilentlyContinue }
if (-not $python) { Fail 'Python 3.13 não está instalado. Baixe em https://www.python.org/downloads e marque "Add python.exe to PATH" na instalação.' }
Ok 'Python encontrado'

Step 'Conferindo o modelo de IA (Git LFS)'
$models = Get-ChildItem -Path (Join-Path $root 'analytics\models') -Filter 'model.onnx' -Recurse -ErrorAction SilentlyContinue
$broken = $models | Where-Object { $_.Length -lt 100000 }
if ($broken) {
    if (Get-Command 'git-lfs' -ErrorAction SilentlyContinue) {
        Push-Location $root
        git lfs install | Out-Null
        git lfs pull
        Pop-Location
        Ok 'modelos baixados pelo Git LFS'
    } else {
        Fail 'O modelo veio como arquivo-texto (clone sem Git LFS). Instale o Git LFS (https://git-lfs.com) e rode este script de novo.'
    }
} else {
    Ok 'modelos presentes'
}

Step 'Conferindo a conexão com o banco'
$settings = Join-Path $root 'backend\src\SmartGrao.WebApi\appsettings.Development.json'
if (-not (Test-Path $settings)) {
    Write-Host '    Falta o arquivo com a senha do banco (ele não vem no clone, de propósito).'
    Write-Host '    Peça a "connection string" do Neon para quem administra o projeto e cole abaixo.'
    Write-Host '    Ela começa com "Host=" e termina com ";".'
    $connection = Read-Host '    Connection string'
    if ([string]::IsNullOrWhiteSpace($connection)) { Fail 'Sem a connection string o sistema não acha o banco.' }
    $json = @{ ConnectionStrings = @{ SmartGrao = $connection.Trim() } } | ConvertTo-Json
    [System.IO.File]::WriteAllText($settings, $json, (New-Object System.Text.UTF8Encoding $false))
    Ok 'appsettings.Development.json criado'
} else {
    Ok 'appsettings.Development.json já existe'
}

Step 'Instalando o frontend (pode levar alguns minutos na primeira vez)'
Push-Location (Join-Path $root 'frontend')
npm install --no-fund --no-audit
if ($LASTEXITCODE -ne 0) { Pop-Location; Fail 'npm install falhou. Veja a mensagem acima.' }
Pop-Location
Ok 'frontend pronto'

Step 'Instalando o serviço de análise (Python)'
$service = Join-Path $root 'analytics\service'
$venvPython = Join-Path $service '.venv\Scripts\python.exe'
if (-not (Test-Path $venvPython)) {
    & $python.Source -m venv (Join-Path $service '.venv')
    if ($LASTEXITCODE -ne 0) { Fail 'Não deu para criar o ambiente Python (.venv).' }
}
& $venvPython -m pip install --disable-pip-version-check -q -r (Join-Path $service 'requirements.txt')
if ($LASTEXITCODE -ne 0) { Fail 'pip install falhou. Veja a mensagem acima.' }
Ok 'serviço de análise pronto'

Step 'Atualizando o banco (migrations)'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project (Join-Path $root 'backend\src\SmartGrao.WebApi') --no-launch-profile -- migrate
if ($LASTEXITCODE -ne 0) { Fail 'As migrations falharam. Confira a connection string em appsettings.Development.json.' }
Ok 'banco atualizado'

Write-Host ''
Write-Host 'Tudo pronto. Para abrir o sistema, dê dois cliques em start.cmd (na raiz do projeto).' -ForegroundColor Green
Write-Host ''
if ($Host.Name -eq 'ConsoleHost' -and -not $env:SMARTGRAO_NO_PAUSE) { Read-Host 'Aperte Enter para fechar' | Out-Null }
