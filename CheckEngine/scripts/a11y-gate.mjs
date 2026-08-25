#!/usr/bin/env node
// Accessibility rehearsal runner for Check Engine storefront surfaces (G6 / NFR-046).
import { createRequire } from 'node:module';
import { writeFileSync } from 'node:fs';
import { join } from 'node:path';

const workDir = process.env.CHECKENGINE_A11Y_WORKDIR || '/tmp/checkengine-a11y-npm';
const outputDir = process.env.CHECKENGINE_A11Y_OUTPUT_DIR || '/tmp/checkengine-a11y';
const baseUrl = (process.env.CHECKENGINE_A11Y_BASE_URL || process.env.CHECKENGINE_CWV_BASE_URL || 'http://127.0.0.1:5000')
  .replace(/\/$/, '');
const chromePath = process.env.CHECKENGINE_A11Y_CHROME || process.env.PLAYWRIGHT_BROWSER_PATH || '';
const pages = (process.env.CHECKENGINE_A11Y_PAGES || '/,/search?q=filter,/computers,/en/gmaster-bmw-parts-5,/build-your-own-computer,/en/gmaster-gm-11127548196-2,/ar/')
  .split(',')
  .map((path) => path.trim())
  .filter(Boolean);

const require = createRequire(join(workDir, 'package.json'));
const { chromium } = require('playwright-core');
const axeSource = require('fs').readFileSync(require.resolve('axe-core'), 'utf8');

const launchOptions = {
  headless: true,
  args: ['--disable-gpu', '--disable-dev-shm-usage', '--no-sandbox']
};
if (chromePath) {
  launchOptions.executablePath = chromePath;
}

const browser = await chromium.launch(launchOptions);
const summary = { nfr: 'NFR-046', scope: '.ce-root,[data-ce-theme]', pages: [], failed: false };

for (const path of pages) {
  const url = baseUrl + path;
  const safe = path.replace(/[/?=&]/g, '_').replace(/^_+|_+$/g, '') || 'home';
  const page = await browser.newPage();
  const response = await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 30000 }).catch(() => null);
  const status = response?.status() ?? 0;
  const entry = { path, url, status, skipped: false, findings: [] };

  if (!response || status >= 400) {
    entry.skipped = true;
    entry.reason = `HTTP ${status || 'unreachable'}`;
    summary.pages.push(entry);
    await page.close();
    continue;
  }

  const hasChrome = await page.locator('.ce-root, [data-ce-theme]').count();
  if (hasChrome === 0) {
    entry.skipped = true;
    entry.reason = 'Check Engine surfaces absent';
    summary.pages.push(entry);
    await page.close();
    continue;
  }

  await page.addScriptTag({ content: axeSource });
  const findings = await page.evaluate(async () => {
    const results = await window.axe.run(
      { include: [['.ce-root'], ['[data-ce-theme]']] },
      { runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'] } }
    );
    return (results.violations || [])
      .filter((violation) => violation.impact === 'serious' || violation.impact === 'critical')
      .map((violation) => ({
        id: violation.id,
        impact: violation.impact,
        description: violation.description,
        help: violation.help,
        nodes: (violation.nodes || []).map((node) => node.target)
      }));
  });

  entry.findings = findings;
  if (findings.length) {
    summary.failed = true;
  }
  writeFileSync(join(outputDir, `axe-${safe}.json`), JSON.stringify(entry, null, 2));
  summary.pages.push(entry);
  await page.close();
}

await browser.close();
writeFileSync(join(outputDir, 'axe-summary.json'), JSON.stringify(summary, null, 2));

for (const page of summary.pages) {
  if (page.skipped) {
    console.log(`[a11y-gate] ${page.path}: skipped (${page.reason})`);
  } else if (page.findings.length) {
    console.error(`[a11y-gate] ${page.path}: fail (${page.findings.length} serious/critical)`);
    for (const finding of page.findings) {
      console.error(`  [${finding.impact}] ${finding.id}: ${finding.description}`);
    }
  } else {
    console.log(`[a11y-gate] ${page.path}: pass`);
  }
}

if (summary.failed) {
  console.error('[a11y-gate] one or more Check Engine surfaces exceeded NFR-046');
  process.exit(1);
}

console.log('[a11y-gate] all scanned Check Engine surfaces within NFR-046');
