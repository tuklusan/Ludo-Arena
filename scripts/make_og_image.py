#!/usr/bin/env python3
# ============================================================================
# Copyright (c) 2026 Supratim Sanyal of SANYALnet Labs.
# Proprietary rights reserved except as expressly licensed herein.
#
# LUDO ARENA
# This file is governed by the SANYALnet Labs Non-Commercial License in the
# root LICENSE file. Non-Commercial use is permitted; Commercial Use and use
# for AI/ML model training are prohibited unless separately authorized.
#
# Attribution is required: "Based on original work by Supratim Sanyal of
# SANYALnet Labs." See LICENSE for full terms, warranty disclaimer, termination,
# patent, trademark, and governing-law provisions.
# ============================================================================
#
#
# Renders the 1200x630 social-share card (Open Graph / X summary_large_image) used by
# the GitHub Pages build, from one of the app-rendered runner screenshots.
#   python scripts/make_og_image.py
# Requires Pillow. Output: src/LudoNimArena.Browser/wwwroot/og-image.png
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "screenshots" / "github-hosted-linux-ubuntu-24.04-x64.png"
OUT = ROOT / "src" / "LudoNimArena.Browser" / "wwwroot" / "og-image.png"
W, H = 1200, 630


def font(size, bold=False):
    names = (["arialbd.ttf", "DejaVuSans-Bold.ttf", "Helvetica-Bold.ttc"] if bold
             else ["arial.ttf", "DejaVuSans.ttf", "Helvetica.ttc"])
    for n in names:
        try:
            return ImageFont.truetype(n, size)
        except OSError:
            pass
    return ImageFont.load_default()


bg = Image.new("RGB", (W, H))
px = bg.load()
for y in range(H):
    for x in range(W):
        t = (x / W + y / H) / 2
        px[x, y] = (int(20 + 22 * t), int(26 + 30 * t), int(48 + 40 * t))

# The board only (square crop of the final-game frame), with a soft frame.
shot = Image.open(SRC).convert("RGB").crop((215, 118, 875, 778)).resize((500, 500), Image.LANCZOS)
frame = Image.new("RGB", (516, 516), (255, 255, 255))
frame.paste(shot, (8, 8))
bg.paste(frame, (W - 516 - 56, (H - 516) // 2))

d = ImageDraw.Draw(bg)
d.text((64, 96), "SANYALnet Labs", font=font(30), fill=(150, 175, 230))
d.text((64, 156), "Ludo AI Arena", font=font(68, True), fill=(255, 255, 255))
for i, line in enumerate(["Four autonomous AI players", "play a full game of Ludo.", "Watch live in your browser."]):
    d.text((64, 290 + i * 46), line, font=font(34), fill=(220, 228, 245))
for i, c in enumerate([(220, 40, 40), (40, 150, 40), (235, 200, 30), (40, 60, 220)]):
    d.ellipse((64 + i * 52, 480, 64 + i * 52 + 38, 518), fill=c, outline=(255, 255, 255), width=3)
d.text((64, 548), "tuklusan.github.io/Ludo-Arena", font=font(26), fill=(150, 175, 230))

OUT.parent.mkdir(parents=True, exist_ok=True)
bg.save(OUT, optimize=True)
print("wrote", OUT, OUT.stat().st_size, "bytes")
