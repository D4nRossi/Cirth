# ADR-0004 — MinIO como Object Storage S3-compatible

**Status**: Aceito
**Data**: 2026-05-11
**Contexto**: Cirth V1

## Contexto

Cirth precisa armazenar arquivos brutos (PDF, DOCX, futuramente vídeo/áudio). Opções: filesystem da VM, MinIO local, Azure Blob, Backblaze B2 ou outra cloud direto desde o início.

A V1 roda local (numa VM Linux), mas há perspectiva de migração para cloud. O objetivo é zero refactor na migração.

## Decisão

Adotamos **MinIO** local desde V1 como object storage, acessado via SDK S3-compatible (`AWSSDK.S3` ou `Minio` SDK).

Quando migrar para cloud, basta trocar o endpoint para:
- **Backblaze B2** (mais barato em produção)
- **Hetzner Object Storage**
- **Azure Blob** via interface S3-compatible (com pequeno adapter)
- **AWS S3** se preferível

## Consequências

**Positivas**:
- Mesma interface S3 em dev local e em qualquer cloud.
- Zero refactor de código de aplicação na migração.
- MinIO é leve (~200MB de imagem), roda confortável na VM.
- Tem console web para administração.

**Negativas**:
- Um container a mais para operar (cumpre seu papel sem dor).
- Configuração inicial de buckets, ACLs e lifecycle policies requer atenção.

## Implementação

Buckets criados pelo `minio-init` container no `docker-compose`:
- `cirth-uploads`: arquivos brutos de documentos
- `cirth-media` (V1.5): vídeo e áudio
- `cirth-exports`: exports gerados pelo sistema (relatórios futuros)

Versionamento de objetos: habilitado em `cirth-uploads` para suportar `DocumentVersion` sem custo extra no app.

## Alternativas consideradas

- **Filesystem da VM**: rejeitado por dificultar migração e backup incremental.
- **Azure Blob direto**: rejeitado por dependência cloud na V1 e custo desnecessário.
- **Backblaze B2 direto**: viável, mas MinIO local é mais barato em dev e mais flexível.
