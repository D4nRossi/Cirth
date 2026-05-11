#!/usr/bin/env bash
# Cirth deploy webhook receiver
# Roda como serviço systemd na VM, escutando port 9000 atrás do NGINX.
# GitHub Actions chama este endpoint após push de imagens para GHCR.

set -euo pipefail

cd /opt/cirth

echo "[$(date)] Deploy starting..."

# Login no GHCR (token salvo em /opt/cirth/.ghcr-token)
docker login ghcr.io -u USERNAME --password-stdin < /opt/cirth/.ghcr-token

# Pull das novas imagens
docker compose pull web mcp worker

# Recriar containers afetados
docker compose up -d web mcp worker

# Cleanup de imagens antigas
docker image prune -f --filter "until=168h"

echo "[$(date)] Deploy complete."
