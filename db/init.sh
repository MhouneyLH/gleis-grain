#!/bin/bash
# Creates the databases and loads the Orleans schema.
#
# The official Postgres image runs everything in /docker-entrypoint-initdb.d once, when it starts on an
# empty data volume. The AppHost mounts this folder there, and the Postgres integration test copies it
# into its container, so both get exactly the same schema. To run it again:
#   docker volume rm gleis-grain-postgres
#
# Orleans does not create tables itself. The scripts in orleans/ are the unchanged Orleans 10.4.0 ones.
#   orleans   membership and reminders (cluster-wide)
#   <module>  grain state of one module
# Keep the module list in sync with src/AppHost/Program.cs.
set -eo pipefail

scripts=/docker-entrypoint-initdb.d/orleans

# create_database <name> <script>...: creates the database, then runs the scripts in it, in order.
create_database() {
  local name=$1
  shift
  psql -v ON_ERROR_STOP=1 -X -q --username "$POSTGRES_USER" --dbname postgres -c "CREATE DATABASE \"$name\""
  for script in "$@"; do
    psql -v ON_ERROR_STOP=1 -X -q --username "$POSTGRES_USER" --dbname "$name" -f "$scripts/PostgreSQL-$script.sql"
  done
}

create_database orleans Main Clustering Reminders

# Persistence uses the query table that Main creates.
for module in stops trips alerts ingestion game; do
  create_database "$module" Main Persistence
done
