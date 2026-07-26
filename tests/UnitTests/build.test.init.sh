#!/bin/sh
set -u

RESULTS_DIR=${TEST_RESULTS_DIR:-/tests/UnitTests/TestResults}

if ! mkdir -p "$RESULTS_DIR"; then
	printf 'Could not create test results directory: %s\n' "$RESULTS_DIR" >&2
	exit 1
fi

find "$RESULTS_DIR" -type f -name '*.trx' -delete

printf 'Test runner .NET version: %s\n' "$(dotnet --version)"
printf 'Test results directory: %s\n' "$RESULTS_DIR"

dotnet test --project UnitTests.csproj --no-restore --report-trx --report-trx-filename UnitTests.trx --results-directory "$RESULTS_DIR"
test_exit_code=$?

printf 'dotnet test exit code: %s\n' "$test_exit_code"
printf 'TRX reports from this run:\n'
find "$RESULTS_DIR" -type f -name '*.trx' -print

exit "$test_exit_code"
