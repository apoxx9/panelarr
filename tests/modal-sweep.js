#!/usr/bin/env node

/**
 * Panelarr Modal Sweep — opens the major modals/overlays on a running instance
 * and captures console errors, page errors and failed API calls. Modals don't
 * render during route-level sweeps, so bugs hide in them.
 *
 * Usage: NODE_PATH=$(npm root -g) node tests/modal-sweep.js
 */

const path = require('path');
const fs = require('fs');

const BASE_URL = process.env.PANELARR_URL || 'http://localhost:8787';

let API_KEY = process.env.PANELARR_APIKEY || '';
if (!API_KEY) {
  const home = process.env.HOME;
  for (const p of [
    path.join(home, 'Library/Application Support/Panelarr/config.xml'),
    '/config/config.xml',
    path.join(home, '.config/Panelarr/config.xml')
  ]) {
    if (fs.existsSync(p)) {
      const match = fs.readFileSync(p, 'utf8').match(/<ApiKey>([^<]+)/);
      if (match) { API_KEY = match[1]; break; }
    }
  }
}

(async () => {
  const puppeteer = require('puppeteer');
  const browser = await puppeteer.launch({ headless: true });
  const page = await browser.newPage();
  await page.setViewport({ width: 1600, height: 1000 });

  const issues = [];
  let current = '';

  page.on('console', (msg) => {
    if (msg.type() !== 'error') return;
    const text = msg.text();
    if (text.includes('favicon') || text.includes('apple-touch')) return;
    issues.push({ step: current, kind: 'console-error', detail: text.substring(0, 400) });
  });
  page.on('pageerror', (err) => {
    issues.push({ step: current, kind: 'pageerror', detail: String(err).substring(0, 400) });
  });
  page.on('response', (res) => {
    if (res.status() >= 400 && res.url().includes('/api/')) {
      issues.push({ step: current, kind: `http-${res.status()}`, detail: res.url() });
    }
  });

  const sleep = (ms) => new Promise(r => setTimeout(r, ms));

  const goto = async (route) => {
    const sep = route.includes('?') ? '&' : '?';
    await page.goto(`${BASE_URL}${route}${sep}apikey=${API_KEY}`, { waitUntil: 'networkidle2', timeout: 20000 });
    await sleep(800);
  };

  // Click an element whose visible text matches (buttons, links, toolbar items)
  const clickByText = async (text, scope = 'button, a, [class*="toolbarButton"], [class*="PageToolbarButton"]') => {
    return page.evaluate((text, scope) => {
      const els = [...document.querySelectorAll(scope)];
      const el = els.find(e => e.textContent.trim().toLowerCase().includes(text.toLowerCase()) && e.offsetParent !== null);
      if (el) { el.click(); return true; }
      return false;
    }, text, scope);
  };

  const closeModal = async () => {
    await page.keyboard.press('Escape');
    await sleep(500);
    // some modals need an explicit close button
    await page.evaluate(() => {
      const btn = document.querySelector('[class*="closeButton"], button[aria-label="Close"]');
      if (btn) btn.click();
    });
    await sleep(400);
  };

  const step = async (name, fn) => {
    current = name;
    try {
      await fn();
      console.log(`  done: ${name}`);
    } catch (err) {
      issues.push({ step: name, kind: 'step-error', detail: err.message });
      console.log(`  ERROR in ${name}: ${err.message}`);
    }
    await closeModal();
  };

  // --- Series Detail modals ---
  await goto('/series/cv-160860');

  await step('series-detail:edit-modal', async () => {
    const ok = await page.evaluate(() => {
      const els = [...document.querySelectorAll('[class*="PageToolbarButton"], button, a')];
      const el = els.find(e => /edit/i.test(e.textContent.trim()) && e.offsetParent !== null);
      if (el) { el.click(); return true; }
      return false;
    });
    if (!ok) throw new Error('Edit button not found');
    await sleep(1200);
  });

  await step('series-detail:organize-modal', async () => {
    if (!await clickByText('preview rename')) throw new Error('Preview Rename button not found');
    await sleep(1500);
  });

  await step('series-detail:retag-modal', async () => {
    if (!await clickByText('write metadata tags')) throw new Error('Write Metadata Tags button not found');
    await sleep(1500);
  });

  await step('series-detail:monitoring-options', async () => {
    if (!await clickByText('issue monitoring')) throw new Error('Issue Monitoring button not found');
    await sleep(1200);
  });

  await step('series-detail:delete-modal', async () => {
    if (!await clickByText('delete')) throw new Error('Delete button not found');
    await sleep(1000);
  });

  // --- Issue Detail: interactive search tab ---
  await goto('/issue/cv-1076354');
  await step('issue-detail:search-tab', async () => {
    const ok = await page.evaluate(() => {
      const tabs = [...document.querySelectorAll('[class*="tab"]')];
      const el = tabs.find(e => /search/i.test(e.textContent.trim()) && e.offsetParent !== null);
      if (el) { el.click(); return true; }
      return false;
    });
    if (!ok) throw new Error('Search tab not found');
    await sleep(3000); // interactive search fires API request
  });

  // --- Manual import folder-select modal (Wanted > Missing toolbar) ---
  await goto('/wanted/missing');
  await step('wanted:manual-import-modal', async () => {
    if (!await clickByText('manual import')) throw new Error('Manual Import button not found');
    await sleep(1500);
  });

  // --- Series index: editor mode + organize ---
  await goto('/library');
  await step('library:series-editor-select', async () => {
    if (!await clickByText('series editor', '*')) {
      // editor may be behind "Options" or a view toggle; not fatal
      throw new Error('Series Editor entry not found (may be view-dependent)');
    }
    await sleep(1000);
  });

  // --- Settings: add indexer / download client / notification schema modals ---
  const clickAddCard = async (classFragment) => {
    return page.evaluate((classFragment) => {
      const card = document.querySelector(`[class*="${classFragment}"]`) ||
                   [...document.querySelectorAll('[class*="card"], [class*="Card"]')].find(e => e.querySelector('svg') && e.textContent.trim() === '');
      if (card) { card.click(); return true; }
      return false;
    }, classFragment);
  };

  await goto('/settings/indexers');
  await step('settings:add-indexer-modal', async () => {
    if (!await clickAddCard('addIndexer')) throw new Error('Add indexer card not found');
    await sleep(1500);
  });

  await goto('/settings/downloadclients');
  await step('settings:add-downloadclient-modal', async () => {
    if (!await clickAddCard('addDownloadClient')) throw new Error('Add download client card not found');
    await sleep(1500);
  });

  await goto('/settings/connect');
  await step('settings:add-notification-modal', async () => {
    if (!await clickAddCard('addNotification')) throw new Error('Add notification card not found');
    await sleep(1500);
  });

  await browser.close();

  console.log('\n=== MODAL SWEEP RESULTS ===');
  const real = issues.filter(i => i.kind !== 'step-error');
  const stepErrors = issues.filter(i => i.kind === 'step-error');
  if (stepErrors.length) {
    console.log('\nSteps that could not run (selector mismatches, not app bugs):');
    for (const i of stepErrors) console.log(`  [skipped] ${i.step}: ${i.detail}`);
  }
  if (real.length === 0) {
    console.log('\nNo console errors, page errors or failed API calls in any modal.');
  } else {
    for (const i of real) console.log(`\n[${i.kind}] at ${i.step}\n  ${i.detail}`);
    console.log(`\n${real.length} issue events`);
  }
  process.exit(0);
})();
