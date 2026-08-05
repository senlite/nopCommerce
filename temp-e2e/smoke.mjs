const { chromium } = require('playwright');
(async() => {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();
  await page.goto('http://127.0.0.1:5000', { waitUntil: 'networkidle' });
  console.log('TITLE:', await page.title());
  console.log('URL:', page.url());
  await page.screenshot({ path: '/tmp/install-page.png', fullPage: true });
  await browser.close();
})();
