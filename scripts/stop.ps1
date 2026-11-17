# Desliga o SmartGrão: encerra a API, o frontend (Vite) e o serviço de análise (uvicorn) deste
# projeto. Serve para quando a janela do start.cmd foi fechada no "X" e algum programa ficou rodando
# escondido.
#
# Procura pela linha de comando de cada processo, e não pela porta: a consulta de portas do Windows
# não achava o Vite em algumas máquinas, e o programa seguia preso na 5173.

$targets = @(
    @{ Name = 'node.exe';   Match = 'vite';                Label = 'Frontend' },
    @{ Name = 'python.exe'; Match = 'smartgrao_analytics'; Label = 'Serviço de análise' },
    @{ Name = 'dotnet.exe'; Match = 'SmartGrao.WebApi';    Label = 'API' },
    @{ Name = 'SmartGrao.WebApi.exe'; Match = '';          Label = 'API' }
)

$stopped = 0
foreach ($target in $targets) {
    $processes = Get-CimInstance Win32_Process -Filter "Name='$($target.Name)'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine -match 'smart_grao' -and ($target.Match -eq '' -or $_.CommandLine -match $target.Match) }
    foreach ($process in $processes) {
        taskkill /T /F /PID $process.ProcessId 2>$null | Out-Null
        Write-Host "$($target.Label) encerrado."
        $stopped++
    }
}
if ($stopped -eq 0) { Write-Host 'Nada do SmartGrão estava rodando.' }
Write-Host ''
Read-Host 'Aperte Enter para fechar' | Out-Null
