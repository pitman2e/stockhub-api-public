#!/bin/bash
SRC="."
DEST="../stockhub-api-public/"

rsync -av \
  --delete \
  --exclude='cronjob/' \
  --exclude='document/' \
  --exclude='.git/' \
  --exclude='.gitignore' \
  "$SRC" "$DEST"