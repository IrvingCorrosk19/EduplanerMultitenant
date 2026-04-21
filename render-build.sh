#!/usr/bin/env bash
set -e

apt-get update
apt-get install -y \
    chromium \
    fonts-liberation \
    libatk-bridge2.0-0 \
    libnss3 \
    libxss1 \
    libasound2 \
    libgbm1 \
    libgtk-3-0

dotnet publish -c Release -o out
