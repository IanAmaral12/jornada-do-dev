#!/usr/bin/env bash
# Roda os testes do Unity em batchmode e resume o resultado. Uso: tools/run_tests.sh [EditMode|PlayMode]
set -u
PLAT="${1:-EditMode}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe}"
WIN="$(cygpath -w "$ROOT")"
RES="$ROOT/TestResults/$PLAT.xml"
LOG="$ROOT/Logs/tests-$PLAT.log"
rm -f "$RES"
"$UNITY" -batchmode -projectPath "$WIN" -runTests -testPlatform "$PLAT" \
  -testResults "$(cygpath -w "$RES")" -logFile "$(cygpath -w "$LOG")" >/dev/null 2>&1
grep -m10 "error CS" "$LOG" | sort -u
if [ ! -f "$RES" ]; then echo "FAIL: sem resultado (compilação?)"; exit 1; fi
echo "$PLAT: $(grep -o '<test-run[^>]*' "$RES" | grep -o 'result="[^"]*"\|total="[0-9]*"\|passed="[0-9]*"\|failed="[0-9]*"' | tr '\n' ' ')"
grep -o '<test-case [^>]*result="Failed"' "$RES" | grep -o 'name="[^"]*"' | head -20
grep -o '<test-run[^>]*' "$RES" | grep -q 'result="Passed"'
