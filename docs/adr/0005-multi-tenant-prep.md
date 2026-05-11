# ADR-0005 — Multi-tenant lógico desde V1

**Status**: Aceito
**Data**: 2026-05-11
**Contexto**: Cirth V1

## Contexto

Cirth nasce como ferramenta pessoal mas tem perspectiva clara de virar SaaS leve para amigos. Decidir multi-tenancy depois significa migração dolorosa (refatorar todos os queries, criar `TenantId` em todas as tabelas, gerar migrations massivas).

## Decisão

Implementar **multi-tenancy lógica desde V1**:
- Toda entidade tem `TenantId` obrigatório.
- EF Core global query filter aplicado automaticamente: `modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantProvider.CurrentTenantId)`.
- `ITenantProvider` injetado, resolve `TenantId` do `HttpContext` (via claim do OIDC ou API key).
- Pipeline behavior do MediatR (`TenantScopingBehavior`) garante que comandos também tenham `TenantId` setado.
- Qdrant: uma única collection `cirth-chunks` com payload `tenant_id` e filtros obrigatórios em toda search.
- MinIO: bucket único `cirth-uploads`, chaves prefixadas `{tenant_id}/{doc_id}/v{n}`.

Para uso pessoal, há apenas uma tenant. Para SaaS, cada conta de Entra ID nova pode virar tenant nova.

## Consequências

**Positivas**:
- SaaS fica viável com mudança mínima de código.
- Isolamento de dados garantido pelo framework, não pela disciplina do desenvolvedor.
- Backup, export, delete por tenant é trivial.

**Negativas**:
- Esforço inicial ligeiramente maior em modelagem (todo lugar tem `TenantId`).
- Qualquer query que precise escapar do filter (admin global) requer `IgnoreQueryFilters()` explícito.

## Alternativas consideradas

- **Single-tenant V1, refactor depois**: rejeitado pelo custo de migração futura.
- **Multi-tenancy física (schema por tenant, ou database por tenant)**: overkill para V1, postergado para V2 se houver compliance exigindo isolamento físico.
