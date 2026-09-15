#!/usr/bin/env bash
# Mirror the runtime-neutral delivery references into the Claude plugin.
#
# The canonical copy of each file lives outside the plugin -- mostly in the
# Codex skill tree, which is the source of truth for delivery domain content.
# A plugin must be self-contained once installed, so those files are copied in
# rather than referenced. This script performs that copy, and `--check` asserts
# the copies are current (used by CI).
#
# Usage: sync-plugin-references.sh [--check]

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEST_DIR="$ROOT/plugins/delivery/references"

# source-relative-path -> destination basename
MANIFEST=(
    "skills/delivery-core/references/contract.md|contract.md"
    "skills/delivery-core/references/slicing.md|slicing.md"
    "skills/delivery/delivery-runtime-protocol/references/review-artifacts.md|review-artifacts.md"
    "skills/delivery/deliver-issue-slice/references/executable-contract.md|executable-contract.md"
    "skills/delivery/deliver-issue-slice/references/pull-request-review.md|pull-request-review.md"
    "skills/delivery/deliver-issue-slice/references/rebase.md|rebase.md"
    "skills/delivery/review-delivery-slice/references/complete-change.md|complete-change.md"
    "skills/delivery/review-delivery-slice/references/test-contract.md|test-contract.md"
)

CHECK=0
[[ "${1:-}" == "--check" ]] && CHECK=1

HEADER="<!-- Synced from %s by scripts/sync-plugin-references.sh. Edit the source, not this copy. -->"

failures=0
mkdir -p "$DEST_DIR"

for entry in "${MANIFEST[@]}"; do
    src="${entry%%|*}"
    dest_name="${entry##*|}"
    src_path="$ROOT/$src"
    dest_path="$DEST_DIR/$dest_name"

    if [[ ! -f "$src_path" ]]; then
        printf 'missing source: %s\n' "$src" >&2
        failures=$((failures + 1))
        continue
    fi

    rendered="$(printf "$HEADER" "$src")"$'\n\n'"$(cat "$src_path")"

    if [[ $CHECK -eq 1 ]]; then
        if [[ ! -f "$dest_path" ]]; then
            printf 'out of date (missing): plugins/delivery/references/%s\n' "$dest_name" >&2
            failures=$((failures + 1))
        elif ! printf '%s\n' "$rendered" | diff -q - "$dest_path" >/dev/null 2>&1; then
            printf 'out of date: plugins/delivery/references/%s differs from %s\n' "$dest_name" "$src" >&2
            failures=$((failures + 1))
        fi
    else
        printf '%s\n' "$rendered" > "$dest_path"
        printf 'synced %s -> plugins/delivery/references/%s\n' "$src" "$dest_name"
    fi
done

# Nothing may linger in the destination that the manifest does not own.
shopt -s nullglob
for existing in "$DEST_DIR"/*.md; do
    name="$(basename "$existing")"
    owned=0
    for entry in "${MANIFEST[@]}"; do
        [[ "${entry##*|}" == "$name" ]] && owned=1 && break
    done
    if [[ $owned -eq 0 ]]; then
        if [[ $CHECK -eq 1 ]]; then
            printf 'unmanaged file in plugin references: %s\n' "$name" >&2
            failures=$((failures + 1))
        else
            rm "$existing"
            printf 'removed unmanaged %s\n' "$name"
        fi
    fi
done

if [[ $failures -gt 0 ]]; then
    if [[ $CHECK -eq 1 ]]; then
        printf '\n%d reference(s) out of date. Run: ./scripts/sync-plugin-references.sh\n' "$failures" >&2
    fi
    exit 1
fi

[[ $CHECK -eq 1 ]] && printf 'Plugin references are in sync (%d files).\n' "${#MANIFEST[@]}"
exit 0
