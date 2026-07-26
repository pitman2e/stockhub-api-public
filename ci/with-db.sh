#!/bin/sh
# Resolve the database connection string from dotnet user-secrets and either
# export it as ConnectionStrings__StockHubDatabase for a wrapped command,
# or write it to an env file for VS Code launch configs.
#
# Program.cs reads only the single key "ConnectionStrings:StockHubDatabase".
# The TEST/UAT/PROD user-secret keys below are a local vault; this script
# copies the selected value into the ConnectionStrings__StockHubDatabase
# environment override (Linux env names cannot contain ':').
#
# Usage:
#   with-db.sh [TEST|UAT|PROD] <command> [args...]
#   with-db.sh --write-env [TEST|UAT|PROD] <outfile>
set -e

usage() {
    echo "Usage: $0 [TEST|UAT|PROD] <command> [args...]" >&2
    echo "       $0 --write-env [TEST|UAT|PROD] <outfile>" >&2
    exit 2
}

WRITE_ENV=0
if [ "${1:-}" = "--write-env" ]; then
    WRITE_ENV=1
    shift
fi

ENV_NAME="${1:-}"
[ -z "$ENV_NAME" ] && usage
shift

case "$ENV_NAME" in
    TEST) SECRET_KEY="ConnectionStrings:StockHubDatabaseTest" ;;
    UAT) SECRET_KEY="ConnectionStrings:StockHubDatabase" ;;
    PROD) SECRET_KEY="ConnectionStrings:StockHubDatabaseProd" ;;
    *) usage ;;
esac

if [ "$WRITE_ENV" -eq 1 ]; then
    [ $# -eq 1 ] || usage
    OUTFILE="$1"
else
    [ $# -ge 1 ] || usage
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
API_PROJECT="$SCRIPT_DIR/../api"

CONN="$(dotnet user-secrets list --project "$API_PROJECT" 2>&1 | sed -n "s/^${SECRET_KEY} = //p")"
if [ -z "$CONN" ]; then
    echo "error: user-secret '$SECRET_KEY' not found. Set it via:" >&2
    echo "  dotnet user-secrets set \"$SECRET_KEY\" \"<ConStrHere>\" --project \"$API_PROJECT\"" >&2
    exit 1
fi

if [ "$WRITE_ENV" -eq 1 ]; then
    printf 'ConnectionStrings__StockHubDatabase=%s\n' "$CONN" > "$OUTFILE"
    chmod 600 "$OUTFILE"
else
    ConnectionStrings__StockHubDatabase="$CONN"
    export ConnectionStrings__StockHubDatabase
    exec "$@"
fi
