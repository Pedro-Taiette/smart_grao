# Rodando o SmartGrão

## O jeito fácil (Windows)

1. Instale uma vez: [.NET SDK 10](https://dotnet.microsoft.com/download),
   [Node.js 20+](https://nodejs.org), [Python 3.13](https://www.python.org/downloads) (marque
   **Add python.exe to PATH**) e [Git LFS](https://git-lfs.com).
2. Clone com o LFS ativo:

   ```powershell
   git lfs install
   git clone https://github.com/Pedro-Taiette/smart_grao
   ```

3. Dois cliques em **`setup.cmd`**. Ele confere as ferramentas, pede a connection string do banco
   (só na primeira vez), instala tudo e atualiza o banco. Pode rodar de novo quando quiser — depois de
   um `git pull`, por exemplo.
4. Dois cliques em **`start.cmd`**. Sobe os três programas e abre o navegador em
   `http://localhost:5173`. Para desligar, feche a janela.

Fechou a janela no "X" e alguma coisa ficou presa na porta? Dois cliques em **`stop.cmd`**.

### Quando algo dá errado

| O que aparece | O que fazer |
|---|---|
| "X não está instalado" | Instale o que a mensagem pede e rode `setup.cmd` de novo |
| "O modelo veio como arquivo-texto" | Instale o Git LFS e rode `setup.cmd` de novo |
| "As migrations falharam" | A connection string está errada: apague `backend/src/SmartGrao.WebApi/appsettings.Development.json` e rode `setup.cmd` para colar de novo |
| Tela "Desempenho da IA" diz "serviço de análise fora do ar" | O serviço Python não subiu: rode `stop.cmd` e depois `start.cmd` |
| A porta já está em uso | Rode `stop.cmd` |

## O jeito manual (cada parte num terminal)

Útil para depurar uma parte só. Pré-requisito extra: `dotnet tool install --global dotnet-ef --version 10.0.9`
se for criar migrations.

**Banco.** Crie `backend/src/SmartGrao.WebApi/appsettings.Development.json` — ele está no
`.gitignore` porque carrega a senha, então não vem no clone:

```json
{
  "ConnectionStrings": {
    "SmartGrao": "Host=<endpoint>.neon.tech; Database=neondb; Username=<user>; Password=<senha>; SSL Mode=VerifyFull; Channel Binding=Require;"
  }
}
```

No Neon, habilite a extensão uma vez: `CREATE EXTENSION IF NOT EXISTS postgis;`

**Esquema.** Migrations são um passo explícito, nunca automáticas na subida:

```powershell
dotnet run --project backend/src/SmartGrao.WebApi -- migrate
```

**Backend** em `http://localhost:5148` (documentação interativa em `/scalar/v1`):

```powershell
dotnet run --project backend/src/SmartGrao.WebApi
```

O perfil padrão também abre HTTPS na 7148, que precisa do certificado de desenvolvimento
(`dotnet dev-certs https --trust`). Os scripts sobem só HTTP para não depender dele.

**Frontend** em `http://localhost:5173`:

```powershell
cd frontend && npm install && npm run dev
```

**Serviço de análise** (satélite e modelo de imagem) em `http://localhost:8000`:

```powershell
cd analytics/service
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
.venv/Scripts/python -m uvicorn smartgrao_analytics.app:app --port 8000
```

Sem ele, clima e vistoria funcionam; o satélite não atualiza e a análise das fotos falha depois das
retentativas (dá para reenfileirar com o serviço de pé).

Se o clone foi feito sem o LFS, o `.onnx` chega como um arquivo-texto de poucos bytes e o serviço não
carrega o modelo. Corrija com `git lfs install` e `git lfs pull`.
