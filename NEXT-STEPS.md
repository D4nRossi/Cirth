# Cirth — Próximos passos

Esse arquivo guia o que fazer depois de descompactar o pacote inicial. Mantém-se curto. Quando terminar o bootstrap inicial, pode apagar este arquivo.

## 1. Subir o repositório

```bash
cd cirth
git init
git add .
git commit -m "chore: bootstrap inicial do Cirth V1"
git branch -M main
git remote add origin git@github.com:D4nRossi/cirth.git
git push -u origin main
```

## 2. Configurar segredos

```bash
cp .env.example .env
chmod 600 .env
# Edite .env com:
#   - Postgres, Redis, Qdrant, MinIO passwords
#   - Azure AI Foundry endpoint + key
#   - Entra ID client_id + client_secret (app registration nova)
#   - Backblaze B2 credentials (para backup)
```

App registration no Entra ID:
- **Single-tenant**, tenant `c050c98c-b463-4591-ac3b-deb782c0ba6e`
- Redirect URI: `https://cirth.local/signin-oidc`
- Logout URI: `https://cirth.local/signout-callback-oidc`
- API permissions: `User.Read` (delegated)
- Gerar client secret

## 3. Certificado local

```bash
# Usar mkcert para HTTPS dev
mkcert -install
mkcert -cert-file nginx/certs/cirth.local.crt -key-file nginx/certs/cirth.local.key cirth.local mcp.cirth.local

# Adicionar ao /etc/hosts
echo "127.0.0.1 cirth.local mcp.cirth.local" | sudo tee -a /etc/hosts
```

## 4. Primeiro build

```bash
docker compose build
docker compose up -d postgres redis qdrant minio
# aguarda containers de dados subirem (~30s)

# rodar migrations (depois do Claude Code criar o DbContext + entidades)
dotnet ef database update --project src/Cirth.Infrastructure --startup-project src/Cirth.Web
```

## 5. Roteiro no Claude Code

Abra o Claude Code no diretório `cirth/`. O `CLAUDE.md` é lido automaticamente. Sugestão de prompts iniciais, na ordem:

1. *"Leia `docs/SPEC-V1.md` e `CLAUDE.md`. Em seguida, gere o `CirthDbContext` em `Cirth.Infrastructure`, mapeie todas as entidades do domínio descritas na seção 7 da SPEC, com global query filter por TenantId, e crie a primeira migration."*

2. *"Implemente a feature `Documents/UploadDocument` em `Cirth.Application`, seguindo o padrão MediatR + FluentValidation. O handler deve validar quota, salvar o arquivo no MinIO via `IObjectStorage`, criar Document + DocumentVersion + Job(type=ProcessDocument)."*

3. *"Implemente o `JobPollingService` em `Cirth.Worker` que polleia a tabela `jobs` a cada 5s, processa jobs `ProcessDocument`: baixa do MinIO, parseia, faz chunk, embeda via Azure AI, upserta no Qdrant."*

4. *"Implemente `HybridSearchQuery` handler usando BM25 do Postgres FTS + Qdrant via RRF (k=60). Retorna top 8 com highlights via ts_headline."*

5. *"Implemente `SendMessageCommand` com streaming via `IAsyncEnumerable<string>` para o chat RAG, usando Semantic Kernel + Azure AI Foundry."*

6. *"Crie as páginas Blazor: `/documents` (upload + listagem), `/chat` (conversa com streaming via SignalR), `/saved` (respostas salvas)."*

7. *"Configure o `Cirth.Mcp` com as 6 tools listadas no ADR-0006, todas wrapper finos de handlers MediatR da Application."*

Vá uma feature por vez. Não tente fazer tudo num prompt só.

## 6. Quando subir para produção

- Provisionar VM (Hetzner CX22 ou Oracle Free Tier ARM).
- Instalar Docker + docker compose.
- Configurar Cloudflare Tunnel para expor `cirth.local` na internet (sem abrir porta no firewall da VM).
- Trocar `cirth.local` por domínio real em `nginx/conf.d/cirth.conf`.
- Configurar cron do `backup.sh` (semanal, 3h da madrugada de domingo).
- Configurar webhook deploy via systemd unit.

## 7. Boa jornada

Os arquivos repousam. Inscreva o conhecimento.
