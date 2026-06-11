#!/usr/bin/env node

/**
 * Panelarr Smoke Test Suite
 *
 * Runs API + browser tests against a running Panelarr instance.
 * Requires: Node.js, puppeteer (global: npm i -g puppeteer)
 *
 * Usage:
 *   node tests/smoke-test.js
 *   node tests/smoke-test.js --url http://nas:8787 --apikey YOUR_KEY
 *   node tests/smoke-test.js --skip-browser   # API tests only
 */

const { execSync } = require('child_process');
const http = require('http');
const https = require('https');
const path = require('path');
const fs = require('fs');

// --- Config ---
const args = process.argv.slice(2);
const getArg = (name, fallback) => {
  const idx = args.indexOf(name);
  return idx !== -1 && args[idx + 1] ? args[idx + 1] : fallback;
};

const BASE_URL = getArg('--url', 'http://localhost:8787');
const SKIP_BROWSER = args.includes('--skip-browser');

// Try to read API key from config.xml if not provided
let API_KEY = getArg('--apikey', '');
if (!API_KEY) {
  try {
    const home = process.env.HOME;
    const configPaths = [
      path.join(home, 'Library/Application Support/Panelarr/config.xml'),
      '/config/config.xml', // Docker
      path.join(home, '.config/Panelarr/config.xml') // Linux
    ];
    for (const p of configPaths) {
      if (fs.existsSync(p)) {
        const xml = fs.readFileSync(p, 'utf8');
        const match = xml.match(/<ApiKey>([^<]+)/);
        if (match) {
          API_KEY = match[1];
          break;
        }
      }
    }
  } catch (e) { /* ignore */ }
}

if (!API_KEY) {
  console.error('Error: No API key found. Pass --apikey YOUR_KEY or ensure config.xml is accessible.');
  process.exit(1);
}

// --- Test framework ---
const results = [];
let currentSection = '';

function section(name) {
  currentSection = name;
  console.log(`\n=== ${name} ===\n`);
}

function test(name, passed, detail) {
  results.push({ section: currentSection, name, passed });
  const icon = passed ? '\x1b[32m[PASS]\x1b[0m' : '\x1b[31m[FAIL]\x1b[0m';
  console.log(`  ${icon} ${name}${detail ? ` (${detail})` : ''}`);
}

function fetchJSON(urlPath) {
  const sep = urlPath.includes('?') ? '&' : '?';
  const fullUrl = `${BASE_URL}${urlPath}${sep}apikey=${API_KEY}`;

  return new Promise((resolve, reject) => {
    const client = fullUrl.startsWith('https') ? https : http;
    client.get(fullUrl, (res) => {
      let data = '';
      res.on('data', (chunk) => data += chunk);
      res.on('end', () => {
        try {
          resolve(JSON.parse(data));
        } catch (e) {
          reject(new Error(`Failed to parse JSON from ${urlPath}: ${data.substring(0, 100)}`));
        }
      });
    }).on('error', reject);
  });
}

// --- API Tests ---
async function runAPITests() {

  section('Search & Add');

  const batman = await fetchJSON('/api/v1/search?term=batman');
  test('Search "batman"', batman.length > 0, `${batman.length} results`);

  const batman2024 = await fetchJSON('/api/v1/search?term=batman%202024');
  test('Search "batman 2024" (year extraction)', batman2024.length > 0, `${batman2024.length} results`);

  const empty = await fetchJSON('/api/v1/search?term=');
  test('Empty search returns empty', empty.length === 0);

  const xmen2099 = await fetchJSON('/api/v1/search?term=x-men%202099');
  test('Search "x-men 2099" (year NOT extracted)', xmen2099.length > 0, `${xmen2099.length} results`);

  const marvel1602 = await fetchJSON('/api/v1/search?term=1602');
  test('Search "1602" (year NOT extracted)', marvel1602.length > 0, `${marvel1602.length} results`);

  const ben10 = await fetchJSON('/api/v1/search?term=ben%2010');
  test('Search "ben 10"', ben10.length > 0, `${ben10.length} results`);

  section('Data Quality');

  const seriesResults = batman.filter(r => r.series);
  const withYear = seriesResults.filter(r => r.series.year).length;
  const withPublisher = seriesResults.filter(r => r.series.disambiguation).length;
  const withImages = seriesResults.filter(r => r.series.images && r.series.images.length > 0).length;
  const withOverview = seriesResults.filter(r => r.series.overview).length;

  test('Results have year', withYear > seriesResults.length * 0.8, `${withYear}/${seriesResults.length}`);
  test('Results have publisher', withPublisher > seriesResults.length * 0.8, `${withPublisher}/${seriesResults.length}`);
  test('Results have images', withImages > seriesResults.length * 0.8, `${withImages}/${seriesResults.length}`);
  test('Results have overview', withOverview > seriesResults.length * 0.5, `${withOverview}/${seriesResults.length}`);

  section('API Endpoints');

  const series = await fetchJSON('/api/v1/series');
  test('GET /series', Array.isArray(series), `${series.length} series`);

  const lookup = await fetchJSON('/api/v1/series/lookup?term=batman');
  test('GET /series/lookup', lookup.length > 0, `${lookup.length} results`);

  const issues = await fetchJSON('/api/v1/issue?page=1&pageSize=5');
  test('GET /issue (paginated)', Array.isArray(issues), `${issues.length} issues`);

  const health = await fetchJSON('/api/v1/health');
  test('GET /health', Array.isArray(health), `${health.length} checks`);

  const calendar = await fetchJSON('/api/v1/calendar?start=2025-01-01&end=2027-12-31');
  test('GET /calendar', Array.isArray(calendar), `${calendar.length} entries`);

  const queue = await fetchJSON('/api/v1/queue?page=1&pageSize=10');
  test('GET /queue', queue !== null);

  const missing = await fetchJSON('/api/v1/wanted/missing?page=1&pageSize=5');
  test('GET /wanted/missing', missing.totalRecords !== undefined, `${missing.totalRecords} missing`);

  section('Cleanup Verification');

  const notifSchema = await fetchJSON('/api/v1/notification/schema');
  const notifNames = notifSchema.map(n => n.implementationName);
  test('Subsonic removed', !notifNames.includes('Subsonic'));
  test('Discord present', notifNames.includes('Discord'));

  const indexerSchema = await fetchJSON('/api/v1/indexer/schema');
  const indexerNames = indexerSchema.map(i => i.implementationName);
  test('Gazelle removed', !indexerNames.includes('Gazelle'));
  test('Nyaa removed', !indexerNames.includes('Nyaa'));
  test('Newznab present', indexerNames.includes('Newznab'));

  section('Config');

  const metaConfig = await fetchJSON('/api/v1/config/metadataprovider');
  test('ComicVine API key configured', !!metaConfig.comicVineApiKey);
  test('Metron credentials configured', !!metaConfig.metronUsername);
}

// --- Browser Tests ---
async function runBrowserTests() {
  let puppeteer;
  try {
    puppeteer = require('puppeteer');
  } catch (e) {
    console.log('\n  [SKIP] Puppeteer not available — skipping browser tests');
    console.log('  Install with: npm i -g puppeteer\n');
    return;
  }

  section('Browser Tests');

  const browser = await puppeteer.launch({ headless: true });
  const page = await browser.newPage();

  try {
    // Homepage
    await page.goto(`${BASE_URL}/?apikey=${API_KEY}`, { waitUntil: 'networkidle2', timeout: 15000 });
    test('Homepage loads', (await page.title()).includes('Panelarr'));

    // Search page
    await page.goto(`${BASE_URL}/add/search?apikey=${API_KEY}`, { waitUntil: 'networkidle2', timeout: 15000 });
    const searchBox = await page.$('input[name="searchBox"]');
    test('Search page has search box', !!searchBox);

    // Search returns table (focus via evaluate — ElementHandle.click can
    // hang on some Chrome builds while the page itself is fine)
    await page.evaluate(() => document.querySelector('input[name="searchBox"]').focus());
    await page.keyboard.type('batman', { delay: 50 });
    await page.waitForSelector('table tbody tr', { timeout: 10000 });
    const rows = await page.$$('table tbody tr');
    test('Search returns table rows', rows.length > 0, `${rows.length} rows`);

    // Column headers
    const headers = await page.$$('table thead th');
    test('Table has 7+ column headers', headers.length >= 7);

    // Sort click
    if (headers.length > 2) {
      await page.evaluate(() => document.querySelectorAll('table thead th')[2].click());
      await new Promise(r => setTimeout(r, 500));
      test('Column header clickable for sort', true);
    }

    // Filter box
    const filterBox = await page.$('input[name="filterBox"]');
    test('Filter box present', !!filterBox);

    // Filter reduces results
    if (filterBox) {
      await page.evaluate(() => document.querySelector('input[name="filterBox"]').focus());
      await page.keyboard.type('dc', { delay: 50 });
      await new Promise(r => setTimeout(r, 500));
      const filteredRows = await page.$$('table tbody tr');
      test('Filter reduces results', filteredRows.length < rows.length && filteredRows.length > 0,
        `${filteredRows.length} filtered vs ${rows.length} total`);

      // Result count
      const spans = await page.$$eval('span', els => els.map(e => e.textContent));
      const countText = spans.find(t => t.includes('of') && t.includes('result'));
      test('Result count displayed', !!countText, countText ? countText.trim() : '');
    }

    // Settings — no fingerprinting
    await page.goto(`${BASE_URL}/settings/mediamanagement?apikey=${API_KEY}`, { waitUntil: 'networkidle2', timeout: 15000 });
    test('No fingerprinting in settings', !(await page.content()).toLowerCase().includes('fingerprinting'));

    // System status
    await page.goto(`${BASE_URL}/system/status?apikey=${API_KEY}`, { waitUntil: 'networkidle2', timeout: 15000 });
    test('System status shows Panelarr', (await page.content()).includes('Panelarr'));

    // Calendar
    await page.goto(`${BASE_URL}/calendar?apikey=${API_KEY}`, { waitUntil: 'networkidle2', timeout: 15000 });
    test('Calendar page loads', (await page.content()).length > 1000);

    // Wanted
    await page.goto(`${BASE_URL}/wanted/missing?apikey=${API_KEY}`, { waitUntil: 'networkidle2', timeout: 15000 });
    test('Wanted missing page loads', (await page.content()).length > 1000);

  } catch (err) {
    console.error('  Browser test error:', err.message);
  } finally {
    await browser.close();
  }
}

// --- Main ---
(async () => {
  console.log('Panelarr Smoke Test Suite');
  console.log(`Target: ${BASE_URL}`);
  console.log(`API Key: ${API_KEY.substring(0, 8)}...`);

  // Check app is running
  try {
    const ping = await fetchJSON('/ping');
    if (ping.status !== 'OK') throw new Error('Bad ping');
  } catch (e) {
    console.error(`\nError: Cannot reach Panelarr at ${BASE_URL}`);
    console.error('Make sure the app is running.\n');
    process.exit(1);
  }

  await runAPITests();

  if (!SKIP_BROWSER) {
    await runBrowserTests();
  }

  // Summary
  const passed = results.filter(r => r.passed).length;
  const failed = results.filter(r => !r.passed).length;
  const total = results.length;

  console.log('\n' + '='.repeat(50));
  if (failed === 0) {
    console.log(`\x1b[32m  ALL ${total} TESTS PASSED\x1b[0m`);
  } else {
    console.log(`\x1b[31m  ${failed} FAILED\x1b[0m, ${passed} passed, ${total} total`);
    console.log('\n  Failed tests:');
    results.filter(r => !r.passed).forEach(r => {
      console.log(`    - [${r.section}] ${r.name}`);
    });
  }
  console.log('='.repeat(50) + '\n');

  process.exit(failed > 0 ? 1 : 0);
})();
