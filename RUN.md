# Cirth — Guia de execução local

## 1. Primeira vez (one-time)

> **Status:** ✅ Concluído — secrets setados, App Registration criada no Entra ID, Postgres com migrations aplicadas.

```bash
# 1. (opcional) .env pra docker compose full (Web+Worker+Mcp em containers).
#    Pra dev local com `make watch`, dá pra ignorar — secrets cobrem.
cp .env.example .env && chmod 600 .env

# 2. Segredos do dev (ficam em ~/.microsoft/usersecrets/, nunca no repo)
#    Endpoints duais — chat usa Foundry /openai/v1, embedding usa cognitiveservices.
dotnet user-secrets set AzureAi:Chat:Endpoint      "https://<resource>.openai.azure.com/openai/v1"   --project src/Cirth.Web
dotnet user-secrets set AzureAi:Chat:ApiKey        "<chave>"                                         --project src/Cirth.Web
dotnet user-secrets set AzureAi:Embedding:Endpoint "https://<resource>.cognitiveservices.azure.com/" --project src/Cirth.Web
dotnet user-secrets set AzureAi:Embedding:ApiKey   "<chave>"                                         --project src/Cirth.Web
dotnet user-secrets set EntraId:ClientId           "<client-id>"                                     --project src/Cirth.Web
dotnet user-secrets set EntraId:ClientSecret       "<client-secret>"                                 --project src/Cirth.Web

# 3. Confiar no certificado HTTPS local (uma vez)
dotnet dev-certs https --trust

# 4. Subir infra local
docker compose -f docker-compose.infra.yml up -d

# 5. Aplicar migrations
make db-update

# 6. Servidor
make watch          # https://localhost:5001
make worker         # Worker (em outro terminal) — processa jobs de ingestão
```

**App Registration no Entra ID** (já configurada):

| Campo | Valor |
|---|---|
| Redirect URI | `https://localhost:5001/signin-oidc` |
| Front-channel logout URL | `https://localhost:5001/signout-callback-oidc` |
| Tenant ID | `c050c98c-b463-4591-ac3b-deb782c0ba6e` |

**Serviços disponíveis após o compose:**

| Serviço | URL |
|---|---|
| Aplicação Web | https://localhost:5001 |
| MinIO Console | http://localhost:9001 (minioadmin / minioadmin123) |
| Qdrant Dashboard | http://localhost:6333/dashboard |
| Postgres | localhost:5432 (cirth / cirth_dev_pass) |
| Redis | localhost:6379 |

---

## 2. Dia a dia

```bash
docker compose -f docker-compose.infra.yml up -d   # se os containers não estiverem rodando
make watch                                          # https://localhost:5001 com hot reload
make worker                                         # outro terminal: processa fila de jobs
make logs-web                                       # outro terminal: tail do Serilog (Web)
make logs-worker                                    # outro terminal: tail do Serilog (Worker)
make test                                           # testes unitários (sem Docker, rápido)
```

Logs do Serilog também estão no browser em `/Admin/Logs` (admin only), com filtros por origem (web/worker), nível mínimo, substring, e auto-refresh a cada 5s.

---

## 3. Checklist de testes manuais

Rodar com `make watch` + `make worker` + infra no Docker.

### Auth
- [ ] Acessar `https://localhost:5001` sem login → redireciona pro Entra ID
- [ ] Login com conta Microsoft → retorna pra `/`
- [ ] Usuário com role `Admin` enxerga link "Administração" na drawer; usuário comum não

### Conexões (sanity check inicial)
- [ ] `/Admin` → tab "Conexões" → clicar mostra 6 cards (PostgreSQL, Redis, Qdrant, MinIO, Azure AI — Chat, Azure AI — Embeddings)
- [ ] Todos os 6 verdes — Chat e Embeddings rodam probe real não-persistente (1 token / 1 vetor)
- [ ] Se algum estiver vermelho, a mensagem da Foundry/banco aparece no card

### Documentos
- [ ] `/Documents` → lista vazia no primeiro acesso (mostra alerta info)
- [ ] Clicar "Novo documento" → navega pra `/Documents/Upload` (rota dedicada, não dialog)
- [ ] Upload de arquivo `.pdf` ou `.txt` → toast "Documento enviado e em processamento", volta pra lista, card aparece com status "Pending"
- [ ] Upload de URL (ex.: `https://example.com`) com modo URL
- [ ] Worker processa → status muda pra "Indexed" (~5-30s dependendo do tamanho)
- [ ] Clicar em row do documento → `/Documents/{id}` mostra metadados + tags + versões

### Busca
- [ ] `/Search` → campo de busca
- [ ] Pesquisar termo presente em documento indexado → resultados com score + highlight
- [ ] Pesquisar termo inexistente → "Nenhum resultado encontrado"

### Chat
- [ ] `/Chat` → botão "+" pra nova conversa
- [ ] Criar conversa → campo de input aparece, foca automático
- [ ] Enviar mensagem → bubble do user aparece + assistant bubble vazia com placeholder
- [ ] Sequência visível: `⏳ Aguardando` → `🔍 Analisando` → `📚 Buscando contexto` → `🤖 Gerando resposta` → tokens streamando
- [ ] Bubble do assistant termina com botão `☆ Salvar resposta`
- [ ] Clicar "Salvar resposta" → toast verde "Resposta salva — acesse em /Saved"
- [ ] Recarregar `/Chat/{id}` → mensagens persistem em ordem cronológica

### Tags e coleções
- [ ] `/Tags` → criar tag com nome + cor opcional (`#RRGGBB`); aparece no chip
- [ ] `/Collections` → criar coleção com nome + descrição
- [ ] Na tela de upload, selecionar tags togláveis pra associar ao documento

### Saved Answers
- [ ] `/Saved` → lista respostas salvas (expand pra ver markdown da resposta)
- [ ] Fazer pergunta SIMILAR à salva no chat → se cosine ≥ 0.85, devolve sem chamar LLM (SavedAnswer hit)

### Recovery de jobs travados
- [ ] Parar o worker no meio de processar um upload (`Ctrl+C` no `make worker`)
- [ ] Documento fica em Processing
- [ ] Após 10min (ou ajustar `Worker:StuckJobThresholdMinutes` pra 1), restart worker → log do `StuckJobRecoveryService` mostra recuperação, job volta pra Retrying
- [ ] Em `/Admin` → tab Conexões, botão "Reprocessar falhas" re-enfileira tudo que está em `Failed` permanente

---

## 4. Testes automatizados

```bash
make test                # unitários (Domain + Application) — sem Docker
make test-integration    # Testcontainers (requer Docker rodando)
make test-all            # tudo
```

---

## 5. Nova migration

```bash
make migration NAME=DescricaoDaMudanca
make db-update
```

Pra migrations que precisam de SQL raw (ex.: coluna GENERATED, índice GIN), gere a migration vazia e edite o método `Up` adicionando `migrationBuilder.Sql("...")` — exemplo em `Migrations/20260512235221_AddContentTsvGenerated.cs`.

---

## 6. Reiniciar do zero (dados)

```bash
docker compose -f docker-compose.infra.yml down -v   # apaga volumes
docker compose -f docker-compose.infra.yml up -d
make db-update
```

---

## 7. Acessando logs

Três formas (todas mostram a mesma informação do Serilog):

| Cenário | Comando |
|---|---|
| Console em tempo real | `make watch` (Web) ou `make worker` (Worker) |
| Tail do arquivo de hoje | `make logs-web` ou `make logs-worker` |
| Browser, com filtros | `/Admin/Logs` — admin only, auto-refresh 5s, filtro por nível e substring |
| Direto no terminal | `tail -F src/Cirth.Web/logs/cirth-web-*.log` |

Arquivos rolando diários em `src/Cirth.{Web,Worker}/logs/cirth-{web,worker}-<data>.log`.
