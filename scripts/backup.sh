#!/usr/bin/env bash
# Ежедневная резервная копия БД Бюджета (pg_dump через docker compose).
# Хранит 7 последних дампов в каталоге backups/.
set -euo pipefail

cd "$(dirname "$0")/.."

STAMP="$(date +%Y%m%d-%H%M%S)"
DIR="backups"
mkdir -p "$DIR"

docker compose exec -T postgres pg_dump -U budget budget | gzip > "$DIR/budget-$STAMP.sql.gz"
echo "Создан дамп: $DIR/budget-$STAMP.sql.gz"

ls -1t "$DIR"/budget-*.sql.gz 2>/dev/null | tail -n +8 | while read -r old; do
  rm -- "$old"
  echo "Удалён старый дамп: $old"
done

# Опционально: копирование в Nextcloud (раскомментируйте и укажите путь/URL).
# rsync -a "$DIR"/budget-*.sql.gz user@nextcloud.example.com:/path/to/nextcloud/uploads/
