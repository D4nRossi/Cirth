# Cirth — Guia de execução local

## 1. Primeira vez (one-time)

```bash
# 1. Copiar e ajustar variáveis de ambiente (para produção futura; dev usa appsettings.json direto)
cp .env.example .env && chmod 600 .env

# 2. Segredos do dev (ficam em ~/.microsoft/usersecrets/, nunca no repo)
dotnet user-secrets set AzureAi:ApiKey      "<sua-chave-azure>"    --project src/Cirth.Web
dotnet user-secrets set EntraId:ClientId    "<client-id-entra>"    --project src/Cirth.Web
dotnet user-secrets set EntraId:ClientSecret "<client-secret>"     --project src/Cirth.Web

# 3. Subir infra local
docker compose -f docker-compose.infra.yml up -d

# 4. Aplicar migrations
make db-update

# 5. Servidor
make watch    # http://localhost:5000 (ou porta configurada)
```

**Serviços disponíveis após o compose:**

| Serviço | URL |
|---|---|
| Aplicação Web | http://localhost:5000 |
| MinIO Console | http://localhost:9001 (minioadmin / minioadmin123) |
| Qdrant Dashboard | http://localhost:6333/dashboard |
| Postgres | localhost:5432 (cirth / cirth_dev_pass) |
| Redis | localhost:6379 |

---

## 2. Dia a dia

```bash
docker compose -f docker-compose.infra.yml up -d   # só se os containers não estiverem rodando
make watch                                          # inicia servidor com hot reload
make test                                           # testes unitários (sem Docker, rápido)
```

---

## 3. Checklist de testes manuais

Rodar com a aplicação em `make watch` e infra no Docker.

### Auth
- [ ] Acessar `http://localhost:5000` sem login → redireciona para Entra ID
- [ ] Login com conta Microsoft → retorna para `/`

### Documentos
- [ ] `/documents` → lista vazia no primeiro acesso
- [ ] Upload de arquivo `.pdf` ou `.txt` → card aparece com status "Pendente"
- [ ] Upload de URL (ex: `https://example.com`) → card aparece com status "Pendente"
- [ ] Worker processa o job → status muda para "Indexado" (requer `make worker` ou Worker rodando)
- [ ] Clicar em documento → página de detalhe

### Busca
- [ ] `/search` → campo de busca
- [ ] Pesquisar termo presente em documento indexado → resultados com score + highlight
- [ ] Pesquisar termo inexistente → "Nenhum resultado encontrado"

### Chat
- [ ] `/chat` → botão "Nova conversa"
- [ ] Criar conversa → campo de input aparece
- [ ] Enviar mensagem → resposta em streaming (cursor piscando enquanto gera)
- [ ] Histórico → mensagens persistem ao recarregar

### Tags e coleções
- [ ] Adicionar tag a documento → tag aparece no card
- [ ] Criar coleção → aparece na lista
- [ ] Adicionar documento à coleção

### Saved Answers
- [ ] `/saved` → lista de respostas salvas

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

---

## 6. Reiniciar do zero (dados)

```bash
docker compose -f docker-compose.infra.yml down -v   # apaga volumes
docker compose -f docker-compose.infra.yml up -d
make db-update
```
