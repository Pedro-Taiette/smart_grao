# Desliga o SmartGrão: derruba o que estiver escutando nas portas do sistema (API 5148, frontend
# 5173, serviço de análise 8000). Serve para quando a janela do start.cmd foi fechada no "X" e
# algum programa ficou rodando escondido.

$ports = 5148, 5173, 8000
$stopped = 0
foreach ($port in $ports) {
    $owners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($owner in $owners) {
        if ($owner -gt 0) {
            taskkill /T /F /PID $owner 2>$null | Out-Null
            Write-Host "Porta ${port}: programa encerrado."
            $stopped++
        }
    }
}
if ($stopped -eq 0) { Write-Host 'Nada do SmartGrão estava rodando.' }
Write-Host ''
Read-Host 'Aperte Enter para fechar' | Out-Null
