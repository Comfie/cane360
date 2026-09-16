// Rendered UI contract test with synthetic intercepted responses; never mutates the database.
async (page) => {
  if (!/^http:\/\/(localhost|127\.0\.0\.1):5173\//.test(page.url())) throw new Error('Local frontend required');
  await page.unrouteAll({ behavior: 'ignoreErrors' });
  await page.goto('http://localhost:5173/finance');
  if (!(await page.locator('.account-summary').innerText()).includes('AUTOTEST-P8A-')) throw new Error('Synthetic account required');
  const requests = [];
  const pattern = '**/api/finance/transactions?**';
  const handler = async (route) => {
    const request = route.request();
    if (request.method() !== 'GET') throw new Error('This UI contract test must not mutate records');
    const query = Object.fromEntries(request.url().split('?')[1].split('&')
      .map((pair) => pair.split('=').map(decodeURIComponent)));
    requests.push(query);
    const pageNumber = Number(query.page);
    const count = pageNumber === 1 ? 50 : 5;
    const items = Array.from({ length: count }, (_, index) => {
      const number = (pageNumber - 1) * 50 + index + 1;
      return {
        id: `20000000-0000-4000-8000-${String(number).padStart(12, '0')}`,
        type: 'Expense', category: 'OtherExpense', eventDate: '2041-01-01',
        payeeOrPayer: `AUTOTEST-P8A payee ${number}`,
        amountUsd: 10, sourceReference: `AUTOTEST-P8A-FIN-${String(number).padStart(5, '0')}`,
        notes: 'Synthetic intercepted UI record', status: 'Draft', version: 0,
        createdAt: '2041-01-01T00:00:00Z', allocations: [],
      };
    });
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
      items, page: pageNumber, pageSize: 50, totalCount: 55,
      postedExpenseUsd: 5000, postedIncomeUsd: 1250, draftCount: 55,
    }) });
  };
  await page.route(pattern, handler);
  try {
    await page.reload();
    await page.getByText('AUTOTEST-P8A-FIN-00001', { exact: true }).waitFor();
    if (await page.locator('.finance-row').count() !== 50) throw new Error('Expected bounded first transaction page');
    const summary = await page.locator('.finance-register-summary').innerText();
    if (!summary.includes('5,000') || !summary.includes('1,250') || !summary.includes('55'))
      throw new Error(`Authoritative server totals were not rendered: ${summary}`);
    for (const width of [1440, 1024, 768, 390, 360]) {
      await page.setViewportSize({ width, height: 900 });
      if (await page.locator('.finance-ledger-head').isVisible() !== (width >= 640))
        throw new Error(`Transaction table/card boundary incorrect at ${width}`);
      if (width <= 768) {
        const undersized = await page.locator('.finance-commandbar button, .finance-filters button, .payroll-pagination button, .finance-row button').evaluateAll((nodes) =>
          nodes.filter((node) => { const box = node.getBoundingClientRect(); return box.width < 44 || box.height < 44; })
            .map((node) => ({ label: node.textContent?.trim(), width: node.getBoundingClientRect().width, height: node.getBoundingClientRect().height })));
        if (undersized.length) throw new Error(`Undersized Finance actions at ${width}: ${JSON.stringify(undersized)}`);
      }
      if (await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth)) {
        const overflow = await page.locator('main *').evaluateAll((nodes) => nodes
          .filter((node) => node.getBoundingClientRect().right > document.documentElement.clientWidth)
          .slice(0, 8).map((node) => ({ tag: node.tagName, className: node.className, right: node.getBoundingClientRect().right })));
        throw new Error(`Populated Finance overflow at ${width}: ${JSON.stringify(overflow)}`);
      }
    }
    await page.getByRole('button', { name: 'Next transactions', exact: true }).click();
    await page.getByText('AUTOTEST-P8A-FIN-00051', { exact: true }).waitFor();
    if (await page.locator('.finance-row').count() !== 5) throw new Error('Second transaction page was not rendered');
    await page.getByLabel('Payee or source', { exact: true }).fill('AUTOTEST-P8A');
    await page.getByRole('button', { name: 'Apply filters', exact: true }).click();
    await page.getByText('AUTOTEST-P8A-FIN-00001', { exact: true }).waitFor();
    await page.getByRole('button', { name: 'Next transactions', exact: true }).click();
    await page.getByText('AUTOTEST-P8A-FIN-00051', { exact: true }).waitFor();
    if (!requests.some((query) => query.page === '2' && query.search === 'AUTOTEST-P8A' && query.pageSize === '50'))
      throw new Error('Finance pagination lost the active search filter');
    return { syntheticInterceptedUi: true, pageSizes: [50, 5], authoritativeTotals: true,
      filterPreserved: true, populatedViewportWidths: [1440, 1024, 768, 390, 360] };
  } finally {
    await page.unroute(pattern, handler);
    await page.reload();
  }
}
