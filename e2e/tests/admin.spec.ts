import { test, expect } from '@playwright/test';
import { login } from './helpers/auth';
import { PASSWORD, SCHOOL_CANTON } from './fixtures';

test.describe('Admin escuela Canton', () => {
  test('login y redirección post-login', async ({ page }) => {
    await login(page, 'admin.canton@test.local', PASSWORD, SCHOOL_CANTON);
    await expect(page).not.toHaveURL(/\/Auth\/Login/i);
    await expect(page).toHaveURL(/\/Home\//i);
  });

  test('/User/Index accesible', async ({ page }) => {
    await login(page, 'admin.canton@test.local', PASSWORD, SCHOOL_CANTON);
    const res = await page.goto('/User/Index');
    expect(res?.status()).toBeLessThan(400);
    await expect(page.locator('body')).toBeVisible();
  });
});
