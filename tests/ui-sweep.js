#!/usr/bin/env node

/**
 * Panelarr UI Sweep — visits every frontend route, captures console errors,
 * page errors, and failed API requests. Also runs navigation sequences that
 * exercise the shared state.series lifecycle (Series Detail unmount clears it).
 *
 * Usage: NODE_PATH=$(npm root -g) node tests/ui-sweep.js
 */

const path = require('path');
const fs = require('fs');

const BASE_URL = process.env.PANELARR_URL || 'http://localhost:8787';

let API_KEY = process.env.PANELARR_APIKEY || '';
if (!API_KEY) {
  const home = process.env.HOME;
  const configPaths = [
    path.join(home, 'Library/Application Support/Panelarr/config.xml'),
    '/config/config.xml',
    path.join(home, '.config/Panelarr/config.xml')
  ];
  for (const p of configPaths) {
    if (fs.existsSync(p)) {
      const match = fs.readFileSync(p, 'utf8').match(/<ApiKey>([^<]+)/);
      if (match) { API_KEY = match[1]; break; }
    }
  }
}

const ROUTES = [
  '/',
  '/library',
  '/add/search',
  '/issues',
  '/shelf',
  '/unmapped',
  '/series/cv-160860',
  '/issue/cv-1076354',
  '/calendar',
  '/activity/history',
  '/activity/queue',
  '/activity/blocklist',
  '/wanted/missing',
  '/wanted/cutoffunmet',
  '/settings',
  '/settings/mediamanagement',
  '/settings/profiles',
  '/settings/quality',
  '/settings/customformats',
  '/settings/indexers',
  '/settings/downloadclients',
  '/settings/importlists',
  '/settings/connect',
  '/settings/metadata',
  '/settings/tags',
  '/settings/general',
  '/settings/ui',
  '/settings/development',
  '/system/status',
  '/system/tasks',
  '/system/backup',
  '/system/updates',
  '/system/events',
  '/system/logs/files'
];

(async () => {
  const puppeteer = require('puppeteer');
  const browser = await puppeteer.launch({ headless: true });
  const page = await browser.newPage();
  await page.setViewport({ width: 1600, height: 1000 });

  const issues = [];
  let current = '';

  page.on('console', (msg) => {
    if (msg.type() === 'error' || msg.type() === 'warning') {
      const text = msg.text();
      // Ignore noise: favicon, SignalR transient disconnects
      if (text.includes('favicon') || text.includes('apple-touch')) return;
      issues.push({ route: current, kind: `console-${msg.type()}`, detail: text.substring(0, 500) });
    }
  });
  page.on('pageerror', (err) => {
    issues.push({ route: current, kind: 'pageerror', detail: String(err).substring(0, 500) });
  });
  page.on('requestfailed', (req) => {
    if (req.url().includes('favicon')) return;
    issues.push({ route: current, kind: 'requestfailed', detail: `${req.url()} :: ${req.failure() && req.failure().errorText}` });
  });
  page.on('response', (res) => {
    if (res.status() >= 400 && res.url().includes('/api/')) {
      issues.push({ route: current, kind: `http-${res.status()}`, detail: res.url() });
    }
  });

  const goto = async (route, label) => {
    current = label || route;
    const sep = route.includes('?') ? '&' : '?';
    await page.goto(`${BASE_URL}${route}${sep}apikey=${API_KEY}`, { waitUntil: 'networkidle2', timeout: 20000 });
    await new Promise(r => setTimeout(r, 600));
  };

  console.log('--- Pass 1: visit every route fresh ---');
  for (const route of ROUTES) {
    try {
      await goto(route);
      process.stdout.write(`  visited ${route}\n`);
    } catch (err) {
      issues.push({ route, kind: 'navigation-error', detail: err.message });
    }
  }

  // --- Pass 2: SPA navigation sequences that exercise state.series lifecycle.
  // Clicking through the SPA (no full page loads) is what triggers stale-state
  // bugs; direct page.goto() reloads the whole app and hides them.
  console.log('--- Pass 2: SPA navigation sequences ---');
  const clickLink = async (selector, label) => {
    current = label;
    await Promise.all([
      page.waitForNetworkIdle({ timeout: 15000 }).catch(() => {}),
      page.click(selector)
    ]);
    await new Promise(r => setTimeout(r, 800));
  };

  try {
    // Library -> Series Detail -> back to Library (unmount clears state.series)
    await goto('/library', 'seq:library');
    current = 'seq:library->seriesdetail';
    await page.evaluate(() => {
      const link = document.querySelector('a[href*="/series/"]');
      if (link) link.click();
    });
    await new Promise(r => setTimeout(r, 1200));

    current = 'seq:seriesdetail->library(back)';
    await page.goBack({ waitUntil: 'networkidle2' }).catch(() => {});
    await new Promise(r => setTimeout(r, 1000));

    // After back-navigation, library should still render series rows/posters
    const seriesVisible = await page.evaluate(() => {
      return document.body.innerText.includes('Absolute Superman') ||
             document.body.innerText.includes('Power Rangers');
    });
    if (!seriesVisible) {
      issues.push({ route: 'seq:seriesdetail->library(back)', kind: 'stale-state', detail: 'Library page empty after returning from Series Detail' });
    }

    // Series Detail (SPA nav) -> Issues index -> open an issue detail
    current = 'seq:issues-after-seriesdetail';
    await page.evaluate(() => {
      const link = document.querySelector('a[href*="/series/"]');
      if (link) link.click();
    });
    await new Promise(r => setTimeout(r, 1200));
    await page.evaluate(() => {
      const link = document.querySelector('a[href="/issues"]');
      if (link) link.click();
    });
    await new Promise(r => setTimeout(r, 1500));

    current = 'seq:issuedetail-after-seriesdetail';
    await page.evaluate(() => {
      const link = document.querySelector('a[href*="/issue/"]');
      if (link) link.click();
    });
    await new Promise(r => setTimeout(r, 1500));

    // Issue detail after visiting series detail: series name should render
    const issueDetailHasSeries = await page.evaluate(() => document.body.innerText.length > 500);
    if (!issueDetailHasSeries) {
      issues.push({ route: 'seq:issuedetail-after-seriesdetail', kind: 'stale-state', detail: 'Issue detail page nearly empty after Series Detail visit' });
    }
  } catch (err) {
    issues.push({ route: current, kind: 'sequence-error', detail: err.message });
  }

  await browser.close();

  console.log('\n=== UI SWEEP RESULTS ===');
  if (issues.length === 0) {
    console.log('No console errors, page errors, failed requests, or stale-state issues found.');
  } else {
    const dedup = {};
    for (const i of issues) {
      const key = `${i.kind} :: ${i.detail}`;
      if (!dedup[key]) dedup[key] = { ...i, routes: new Set() };
      dedup[key].routes.add(i.route);
    }
    for (const key of Object.keys(dedup)) {
      const i = dedup[key];
      console.log(`\n[${i.kind}] on ${[...i.routes].join(', ')}\n  ${i.detail}`);
    }
    console.log(`\n${issues.length} total issue events (${Object.keys(dedup).length} unique)`);
  }
  process.exit(0);
})();
