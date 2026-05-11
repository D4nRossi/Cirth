# ADR-0003 — Busca híbrida BM25 + Vetorial via RRF

**Status**: Aceito
**Data**: 2026-05-11
**Contexto**: Cirth V1

## Contexto

KM com chat RAG depende criticamente da qualidade do retrieval. Busca puramente lexical (BM25) falha em sinônimos e paráfrases. Busca puramente vetorial falha em termos raros, nomes próprios e siglas. A literatura recente é consistente: híbrida supera ambas.

## Decisão

Implementar **busca híbrida** combinando:
- **BM25-like via Postgres FTS** (`tsvector` + `ts_rank_cd`), recuperando top 50.
- **Vetorial via Qdrant** (cosine similarity sobre `text-embedding-3-small`), recuperando top 50.
- **Reciprocal Rank Fusion** com `k=60` para combinar os rankings.
- Top 8 final entregue ao retrieval do RAG.

**Não** implementaremos reranker (cross-encoder ou LLM-based) na V1, por simplicidade.

## Consequências

**Positivas**:
- Qualidade de retrieval significativamente superior a abordagens puras.
- Tolerância a queries mal-formuladas (BM25 ajuda quando o user usa termo exato; vetorial ajuda quando descreve por significado).
- Fallback natural: se Qdrant falhar, BM25 ainda funciona; se embedding falhar, BM25 ainda funciona.

**Negativas**:
- Duas consultas por busca em vez de uma (latência total ~30% maior que abordagem pura, ainda dentro do orçamento de 500ms p95).
- Tuning de RRF e thresholds requer experimentação inicial.
- Sem reranker, qualidade pode ficar abaixo do que reranker entregaria (aceitável para V1).

## Alternativas consideradas

- **Só vetorial**: rejeitado por falhas conhecidas em termos exatos e siglas.
- **Só BM25**: rejeitado por não atender RAG semântico.
- **Híbrida com reranker**: postergado para V2, quando volume justificar o custo extra de latência e modelo.
