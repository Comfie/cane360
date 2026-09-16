// Execute with the existing Playwright browser runner against a logged-in AUTOTEST-P8A workspace.
// This checks rendered layout and keyboard behavior; it is not a populated golden-path or performance gate.
async (page) => {
  const origin = await page.evaluate(() => window.location.origin);
  if (!['http://127.0.0.1:5173', 'http://localhost:5173'].includes(origin)) {
    throw new Error('Use the local Cane360 frontend against Railway Development.');
  }
  await page.goto(`${origin}/farm`);
  await page.getByRole('heading', { name: 'Your farm record', exact: true }).waitFor();
  if (!(await page.locator('.account-summary').innerText()).includes('AUTOTEST-P8A-')) {
    throw new Error('An isolated AUTOTEST-P8A account is required.');
  }
  const results = [];
  const routes = ['/', '/farm', '/fields', '/activities', '/labour', '/payroll', '/inventory', '/finance', '/reports', '/administration'];
  for (const route of routes) {
    await page.goto(`${origin}${route}`);
    await page.locator('h1').waitFor();
    await page.waitForLoadState('networkidle');
    await page.locator('.loading-state').waitFor({ state: 'hidden' });
    for (const width of [1440, 1024, 768, 390, 360]) {
      await page.setViewportSize({ width, height: 900 });
      const dimensions = await page.evaluate(() => ({
        client: document.documentElement.clientWidth,
        scroll: document.documentElement.scrollWidth,
      }));
      if (dimensions.scroll > dimensions.client) {
        throw new Error(`${route} overflows at ${width}px: ${dimensions.scroll} > ${dimensions.client}`);
      }
      results.push({ route, width, overflow: false });
    }
  }
  await page.getByRole('button', { name: 'More', exact: true }).click();
  const menu = page.getByRole('dialog', { name: 'More', exact: true });
  await menu.waitFor();
  if (!await menu.evaluate((element) => element.contains(document.activeElement))) throw new Error('Menu did not receive focus');
  await page.keyboard.press('Shift+Tab');
  if (!await menu.getByRole('link', { name: 'Administration', exact: true }).evaluate((element) => element === document.activeElement)) {
    throw new Error('Menu reverse-tab did not wrap');
  }
  await page.keyboard.press('Escape');
  if (!await page.getByRole('button', { name: 'More', exact: true }).evaluate((element) => element === document.activeElement)) {
    throw new Error('Menu did not restore focus');
  }
  await page.goto(`${origin}/finance`);
  await page.getByRole('button', { name: 'New transaction', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await dialog.waitFor();
  if (!await dialog.evaluate((element) => element.contains(document.activeElement))) throw new Error('Finance dialog did not receive focus');
  for (let index = 0; index < 30; index++) {
    await page.keyboard.press('Tab');
    if (!await dialog.evaluate((element) => element.contains(document.activeElement))) throw new Error('Focus escaped finance dialog');
  }
  const clipped = await dialog.evaluate((element) => element.scrollWidth > element.clientWidth);
  if (clipped) throw new Error('Finance form scrolls horizontally at 360px');
  await page.keyboard.press('Escape');
  if (!await page.getByRole('button', { name: 'New transaction', exact: true }).evaluate((element) => element === document.activeElement)) {
    throw new Error('Finance dialog did not restore focus');
  }
  await page.goto(`${origin}/farm`);
  for (const [buttonName, dialogName] of [['Edit farm information', 'Edit farm information'], ['Add person', 'Add person']]) {
    const opener = page.getByRole('button', { name: buttonName, exact: true });
    await opener.click();
    const farmDialog = page.getByRole('dialog', { name: dialogName, exact: true });
    await farmDialog.waitFor();
    for (let index = 0; index < 24; index++) {
      await page.keyboard.press(index % 2 ? 'Shift+Tab' : 'Tab');
      if (!await farmDialog.evaluate((element) => element.contains(document.activeElement))) throw new Error(`${dialogName}: focus escaped`);
    }
    if (await farmDialog.evaluate((element) => element.scrollWidth > element.clientWidth)) throw new Error(`${dialogName}: horizontal form overflow`);
    await page.keyboard.press('Escape');
    if (!await opener.evaluate((element) => element === document.activeElement)) throw new Error(`${dialogName}: focus not restored`);
  }
  return { results, keyboardChecks: 12, browserVersion: page.context().browser()?.version(), populatedGoldenPaths: 'not exercised' };
}
