// Rendered UI contract test with synthetic intercepted responses; not database acceptance.
async (page) => {
  if (!/^http:\/\/(localhost|127\.0\.0\.1):5173\//.test(page.url())) throw new Error('Local frontend required');
  if (!(await page.locator('.account-summary').innerText()).includes('AUTOTEST-P8A-')) throw new Error('Synthetic account required');
  const requests = [];
  let statementReads = 0;
  const pattern = '**/api/finance/mill-records/**';
  const handler = async (route) => {
    const request = route.request();
    if (request.method() !== 'GET') throw new Error('This UI contract test must not mutate records');
    const url = request.url();
    if (/\/tickets\?/.test(url)) {
      const query = Object.fromEntries(url.split('?')[1].split('&').map((pair) => pair.split('=').map(decodeURIComponent)));
      requests.push(query);
      const pageNumber = Number(query.page);
      const count = pageNumber === 1 ? 50 : 5;
      const items = Array.from({ length: count }, (_, index) => ({
        id: `00000000-0000-4000-8000-${String((pageNumber - 1) * 50 + index + 1).padStart(12, '0')}`,
        millId: '00000000-0000-4000-8000-000000000100', millCode: 'P8A', millName: 'AUTOTEST-P8A Synthetic Mill',
        ticketReference: `P8A-GRID-${(pageNumber - 1) * 50 + index + 1}`, ticketDate: '2041-01-01',
        grossTonnes: 20, tareTonnes: 5, netTonnes: 15, status: 'Recorded', version: 1,
        createdAt: '2041-01-01T00:00:00Z', recordedAt: '2041-01-01T00:00:00Z',
        isCurrent: true, evidence: [], matchedStatementIds: [], matchedToAnotherStatement: false,
      }));
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
        items, page: pageNumber, pageSize: 50, totalCount: 55, unmatchedCount: 55, recordedNetTonnes: 825,
      }) });
    } else if (/\/statements\?/.test(url)) {
      statementReads++;
      const query = Object.fromEntries(url.split('?')[1].split('&').map((pair) => pair.split('=').map(decodeURIComponent)));
      const pageNumber = Number(query.page);
      const count = pageNumber === 1 ? 50 : 5;
      const items = Array.from({ length: count }, (_, index) => {
        const number = (pageNumber - 1) * 50 + index + 1;
        const id = `10000000-0000-4000-8000-${String(number).padStart(12, '0')}`;
        return { id, millId: '00000000-0000-4000-8000-000000000100', millCode: 'P8A',
          millName: 'AUTOTEST-P8A Synthetic Mill', statementReference: `P8A-STATEMENT-${number}`,
          periodStart: '2041-01-01', periodEnd: '2041-01-31', totalTonnes: 300, totalAmountUsd: 12000,
          status: 'Draft', version: 1, createdAt: '2041-01-01T00:00:00Z', isCurrent: true, evidence: [],
          reconciliation: { growerStatementId: id, statementTonnes: 300, matchedTicketTonnes: 0,
            tonnesVariance: 300, statementAmountUsd: 12000, amountStatus: 'NotAvailable', status: 'Unmatched',
            hasCrossStatementTicketReuse: false, matches: [] } };
      });
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
        items, page: pageNumber, pageSize: 50, totalCount: 55,
      }) });
    } else if (/\/statements\/[^/?]+\/candidates/.test(url)) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    } else await route.continue();
  };
  await page.route(pattern, handler);
  try {
    await page.getByRole('button', { name: 'Mill records', exact: true }).click();
    await page.getByText('P8A-GRID-1', { exact: true }).waitFor();
    await page.waitForFunction(() => document.querySelector('.mill-workspace')?.getAttribute('aria-busy') === 'false');
    if (await page.locator('.mill-ticket-row').count() !== 50) throw new Error('Expected bounded first page');
    if (!(await page.locator('.mill-summary').innerText()).includes('825')) throw new Error('Server total not rendered');
    if (statementReads !== 0) throw new Error('Ticket view fetched unrelated statement history');
    for (const width of [1440, 1024, 768, 390, 360]) {
      await page.setViewportSize({ width, height: 900 });
      if (await page.locator('.mill-table-head').isVisible() !== (width >= 1280)) throw new Error(`Ticket table/card boundary incorrect at ${width}`);
      if (width <= 768) {
        const undersized = await page.locator('.mill-view-switch button, .mill-toolbar-actions button, .mill-toolbar-actions a').evaluateAll((nodes) =>
          nodes.filter((node) => { const box = node.getBoundingClientRect(); return box.width < 44 || box.height < 44; })
            .map((node) => ({ label: node.textContent?.trim(), width: node.getBoundingClientRect().width, height: node.getBoundingClientRect().height })));
        if (undersized.length) throw new Error(`Undersized touch actions at ${width}: ${JSON.stringify(undersized)}`);
      }
      if (await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth)) {
        await page.screenshot({ path: `artifacts/p8a-populated-mill-overflow-${width}.png` });
        const overflow = await page.locator('main *').evaluateAll((nodes) => nodes.filter((node) => node.getBoundingClientRect().right > document.documentElement.clientWidth).slice(0, 8).map((node) => ({ tag: node.tagName, className: node.className, right: node.getBoundingClientRect().right })));
        throw new Error(`Populated ticket overflow at ${width}: ${JSON.stringify(overflow)}`);
      }
      if (width === 1024 || width === 360) await page.screenshot({ path: `artifacts/p8a-populated-mill-verified-${width}.png` });
    }
    await page.getByRole('button', { name: 'Next tickets', exact: true }).click();
    await page.getByText('P8A-GRID-51', { exact: true }).waitFor();
    if (await page.locator('.mill-ticket-row').count() !== 5) throw new Error('Second page not rendered');
    await page.getByLabel('Reference', { exact: true }).fill('P8A-GRID');
    await page.getByText('P8A-GRID-1', { exact: true }).waitFor();
    await page.waitForFunction(() => document.querySelector('.mill-workspace')?.getAttribute('aria-busy') === 'false');
    await page.getByRole('button', { name: 'Next tickets', exact: true }).click();
    await page.getByText('P8A-GRID-51', { exact: true }).waitFor();
    if (!requests.some((query) => query.page === '2' && query.search === 'P8A-GRID' && query.pageSize === '50')) throw new Error('Pagination lost the filter');
    await page.getByRole('button', { name: 'Statements', exact: true }).click();
    await page.locator('.statement-card').filter({ hasText: 'P8A-STATEMENT-1' }).first().waitFor();
    if (await page.locator('.statement-card').count() !== 50) throw new Error('Expected bounded statement page');
    await page.getByRole('button', { name: 'Next statements', exact: true }).click();
    await page.locator('.statement-card').filter({ hasText: 'P8A-STATEMENT-51' }).first().waitFor();
    if (await page.locator('.statement-card').count() !== 5) throw new Error('Second statement page not rendered');
    await page.getByRole('combobox', { name: /^Match state/ }).selectOption('Variance');
    await page.waitForFunction(() => document.querySelector('.mill-workspace')?.getAttribute('aria-busy') === 'false');
    await page.getByRole('button', { name: 'Tickets', exact: true }).click();
    await page.getByText('P8A-GRID-1', { exact: true }).waitFor();
    if (await page.getByRole('combobox', { name: /^Match state/ }).inputValue() !== '') throw new Error('Statement-only status leaked into ticket filters');
    return { syntheticInterceptedUi: true, ticketPageSizes: [50, 5], statementPageSizes: [50, 5],
      serverTotal: 825, filterPreserved: true, populatedViewportWidths: [1440, 1024, 768, 390, 360],
      unrelatedStatementHistorySkipped: true };
  } finally {
    await page.unroute(pattern, handler);
    await page.reload();
  }
}
