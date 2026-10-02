import { expect, test } from '@playwright/test';

test('an operator declares an incident and acknowledges it', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Command overview' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Open incidents' }).getByRole('timer').first()).toBeVisible();

  await page.getByLabel('On shift as').fill('Ana Ribeiro');
  await page.getByRole('link', { name: 'Declare incident' }).first().click();
  await page.getByLabel('Title').fill('Search results empty for every query');
  await page.getByLabel('Affected service').selectOption('search');
  await page
    .locator('label')
    .filter({ has: page.getByRole('radio', { name: /Sev2/ }) })
    .click();
  await page.getByLabel('Impact').fill('Every query returns zero results since 12:40 UTC.');
  await page.getByRole('button', { name: 'Declare incident' }).click();

  await expect(page.getByRole('heading', { name: /Search results empty for every query/ })).toBeVisible();
  await page.getByRole('button', { name: 'Acknowledge', exact: true }).click();
  await page.getByRole('form', { name: 'Acknowledge incident' }).getByRole('button', { name: 'Acknowledge' }).click();

  await expect(page.getByText('Incident acknowledged. The acknowledge clock has stopped.')).toBeVisible();
});
