#!/usr/bin/env bash
set -euo pipefail

DB_PATH="${DB_PATH:-/app/data/hiddenlistr.db}"
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Data Source=${DB_PATH}}"

mkdir -p "$(dirname "$DB_PATH")"

echo "Applying EF Core migrations..."
dotnet ef database update --project /src/HiddenListR.Domain --startup-project /src/HiddenListR

echo "Running seed SQL scripts from /src/HiddenListR.Domain/Scripts..."
shopt -s nullglob
for script in /src/HiddenListR.Domain/Scripts/*.sql; do
  echo "Executing $(basename "$script")"
  sqlite3 "$DB_PATH" < "$script"
done

# Ensure app container user can read/write SQLite files on the shared volume.
chmod 777 "$(dirname "$DB_PATH")" || true
chmod 666 "$DB_PATH" "$DB_PATH"-shm "$DB_PATH"-wal 2>/dev/null || true

echo "DB init complete."
