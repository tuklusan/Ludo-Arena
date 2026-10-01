/* Copyright (c) 2026 Supratim Sanyal of SANYALnet Labs.
   Proprietary rights reserved except as expressly licensed herein.
   
   LUDO ARENA
   This file is governed by the SANYALnet Labs Non-Commercial License in the
   root LICENSE file. Non-Commercial use is permitted; Commercial Use and use
   for AI/ML model training are prohibited unless separately authorized.
   
   Attribution is required: "Based on original work by Supratim Sanyal of
   SANYALnet Labs." See LICENSE for full terms, warranty disclaimer, termination,
   patent, trademark, and governing-law provisions.
   ============================================================================ */

// Minimal Emscripten-built .NET runtime bootstrap. The runtime downloads and initialises
// first; the game itself starts only on the player's click. That click is the user gesture
// browsers require before audio may play, so the AudioContext is unlocked in the same handler.
import { dotnet } from './_framework/dotnet.js';

const loader = document.getElementById('loader');
const note = document.getElementById('loader-note');
const tap = document.getElementById('loader-tap');

function unlockAudio() {
    try {
        const Ctx = window.AudioContext || window.webkitAudioContext;
        if (!Ctx) return;
        const ctx = new Ctx();
        const src = ctx.createBufferSource();
        src.buffer = ctx.createBuffer(1, 1, 22050);
        src.connect(ctx.destination);
        src.start(0);
        if (ctx.resume) ctx.resume();
    } catch (e) { /* audio is optional */ }
}

function dismissLoader() {
    loader.classList.add('done');
    setTimeout(() => loader.remove(), 400);
}

try {
    const runtime = await dotnet
        .withDiagnosticTracing(false)
        .withApplicationArgumentsFromQuery()
        .create();
    const config = runtime.getConfig();

    note.hidden = true;
    tap.hidden = false;
    loader.classList.add('ready');

    await new Promise(resolve => loader.addEventListener('click', resolve, { once: true }));
    unlockAudio();
    tap.textContent = 'Starting…';

    new MutationObserver((_, obs) => {
        if (document.querySelector('#out canvas')) { dismissLoader(); obs.disconnect(); }
    }).observe(document.getElementById('out'), { childList: true, subtree: true });

    await runtime.runMainAndExit(config.mainAssemblyName, [window.location.search]);
} catch (err) {
    console.error(err);
    tap.hidden = true;
    note.hidden = false;
    note.textContent = 'Sorry - the game could not start in this browser.';
}
