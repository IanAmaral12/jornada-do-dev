#!/usr/bin/env bash
# Roda os testes EditMode do Unity em batchmode e resume o resultado.
set -u
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.3.28f1/Editor/Unity.exe}"
WIN="$(cygpath -w "$ROOT")"
rm -f "$ROOT/TestResults/editmode.xml"
"$UNITY" -batchmode -projectPath "$WIN" -runTests -testPlatform EditMode \
  -testResults "$WIN\TestResults\editmode.xml" -logFile "$WIN\Logs\tests.log" >/dev/null 2>&1
grep -m10 "error CS" "$ROOT/Logs/tests.log" | sort -u
if [ ! -f "$ROOT/TestResults/editmode.xml" ]; then echo "FAIL: sem resultado (compilação?)"; exit 1; fi
grep -o '<test-run[^>]*' "$ROOT/TestResults/editmode.xml" | grep -o 'result="[^"]*"\|total="[0-9]*"\|passed="[0-9]*"\|failed="[0-9]*"' | tr '\n' ' '; echo
grep -o 'name="[^"]*"[^>]*result="Failed"' "$ROOT/TestResults/editmode.xml" | grep -o '^name="[^"]*"' | head -20
grep -q 'result="Passed"' <(grep -o '<test-run[^>]*' "$ROOT/TestResults/editmode.xml")
