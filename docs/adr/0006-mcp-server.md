# ADR-0006 — MCP Server reutilizando Application handlers

**Status**: Aceito
**Data**: 2026-05-11
**Contexto**: Cirth V1

## Contexto

Cirth expõe um servidor MCP (Model Context Protocol) para que Claude Desktop e Claude Code possam consultar a base programaticamente. A questão é: o MCP deve duplicar lógica ou consumir a mesma camada que a UI?

## Decisão

Implementar **Cirth.Mcp** como um host ASP.NET Core minimalista que:
1. Implementa o protocolo MCP via SDK oficial `ModelContextProtocol` para .NET.
2. Suporta transporte **stdio** (uso local pelo Claude Desktop) e **HTTP/SSE** (uso remoto pelo Claude Code).
3. Autentica via header `X-Cirth-Api-Key` (HTTP) ou env var (stdio), resolvido por uma tabela `api_keys` em Postgres.
4. Cada tool MCP é um wrapper fino que despacha para um handler MediatR da `Cirth.Application` — **zero lógica duplicada**.

Tools expostas na V1:
- `search_documents(query, tags?, limit?)`
- `ask_question(question, conversation_id?)` — retorna resposta final (sem streaming no MCP)
- `get_document(document_id)`
- `list_tags()`
- `list_collections()`
- `list_saved_answers(query?)`

## Consequências

**Positivas**:
- Lógica de negócio mora em um único lugar (`Cirth.Application`). Bug fix ali corrige UI e MCP simultaneamente.
- Adicionar novo handler na Application automaticamente habilita tanto via UI quanto via MCP (com wrapper trivial).
- Auth desacoplada: UI usa OIDC + cookie, MCP usa API key. Ambos resolvem para `(TenantId, UserId)`.

**Negativas**:
- Streaming de chat não é trivial via MCP (protocolo MCP atual prioriza request/response). Para V1, `ask_question` retorna resposta completa. Streaming via MCP fica para V2 quando o protocolo amadurecer.
- Necessidade de gerenciar API keys (UI para criar/revogar, scoping por user).

## Alternativas consideradas

- **MCP standalone com lógica própria**: rejeitado por duplicação.
- **Não ter MCP na V1**: rejeitado porque Danirock usa Claude Code/Desktop intensamente, e o ganho de produtividade é alto.
