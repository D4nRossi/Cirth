#!/usr/bin/env bash
# Cirth weekly backup script.
# Roda via cron: 0 3 * * 0 /opt/cirth/scripts/backup.sh

set -euo pipefail

BACKUP_DIR="/opt/cirth/backups"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
RETENTION_DAYS=30

source /opt/cirth/.env

mkdir -p "$BACKUP_DIR"

echo "[$(date)] Starting Cirth backup..."

# 1. Dump PostgreSQL
echo "Dumping Postgres..."
docker exec cirth-postgres pg_dump -U "$POSTGRES_USER" -d cirth -F c -Z 9 \
  > "$BACKUP_DIR/postgres-$TIMESTAMP.dump"

# 2. Sincronizar MinIO para Backblaze B2 via rclone (incremental)
echo "Syncing MinIO to Backblaze B2..."
rclone sync \
  --s3-endpoint http://localhost:9000 \
  --s3-access-key-id "$MINIO_ROOT_USER" \
  --s3-secret-access-key "$MINIO_ROOT_PASSWORD" \
  :s3:cirth-uploads \
  b2:$B2_BUCKET/minio/cirth-uploads/

# 3. Upload do dump Postgres pro B2
echo "Uploading Postgres dump to B2..."
rclone copy "$BACKUP_DIR/postgres-$TIMESTAMP.dump" b2:$B2_BUCKET/postgres/

# 4. Limpar dumps locais antigos
echo "Cleaning local backups older than $RETENTION_DAYS days..."
find "$BACKUP_DIR" -name "postgres-*.dump" -mtime +$RETENTION_DAYS -delete

echo "[$(date)] Backup complete."
