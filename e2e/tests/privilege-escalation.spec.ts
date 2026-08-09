import { test, expect } from '@playwright/test';
import { login } from './helpers/auth';
import { PASSWORD, SCHOOL_CANTON } from './fixtures';

test.describe('Escalación de privilegios', () => {
  test('profesor no puede usar /User/Index (solo admin)', async ({ page }) => {
    await login(page, 'profesor.canton@test.local', PASSWORD, SCHOOL_CANTON);
    await page.goto('/User/Index', { waitUntil: 'networkidle' });
    await expect(page).toHaveURL(/Login|AccessDenied|Auth/i);
  });
});
