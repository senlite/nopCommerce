const { chromium } = require('playwright');
(async() => {
  const browser = await chromium.launch({ headless: false, slowMo: 150 });
  const page = await browser.newPage();
  await page.goto('http://host.containers.internal:5000', { waitUntil: 'networkidle' });
  await page.screenshot({ path: '/tmp/install-page.png', fullPage: true });
  await page.waitForTimeout(3000);
  await browser.close();
})();
