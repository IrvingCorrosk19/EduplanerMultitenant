import { test, expect } from '@playwright/test';
import { login } from './helpers/auth';
import { PASSWORD, SCHOOL_CANTON } from './fixtures';

test.describe('Estudiante — notas y ficha', () => {
  test('ver notas (StudentReport) y detalle propio (Student/Details)', async ({ page }) => {
    await login(page, 'estudiante.canton@test.local', PASSWORD, SCHOOL_CANTON);

    const resRep = await page.goto('/StudentReport/Index');
    expect(resRep?.status()).toBeLessThan(400);
    await expect(page.locator('body')).toBeVisible();

    const studentId = await page.locator('#student-id').getAttribute('value');
    expect(studentId && studentId.length > 10, 'StudentReport debe exponer #student-id').toBeTruthy();

    const resDet = await page.goto(`/Student/Details/${studentId}`);
    expect(resDet?.status()).toBeLessThan(400);
    await expect(page).toHaveURL(/Student\/Details/i);
    await expect(page.locator('body')).toBeVisible();
  });
});
