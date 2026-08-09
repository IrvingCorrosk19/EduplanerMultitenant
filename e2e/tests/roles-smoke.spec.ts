import { test, expect } from '@playwright/test';
import { login } from './helpers/auth';
import { PASSWORD, SCHOOL_CANTON } from './fixtures';

test.describe('Smoke por rol (Canton)', () => {
  test('profesor: TeacherGradebook Index', async ({ page }) => {
    await login(page, 'profesor.canton@test.local', PASSWORD, SCHOOL_CANTON);
    const res = await page.goto('/TeacherGradebook/Index');
    expect(res?.status()).toBeLessThan(400);
    await expect(page.locator('body')).toBeVisible();
  });

  test('estudiante: StudentReport (notas)', async ({ page }) => {
    await login(page, 'estudiante.canton@test.local', PASSWORD, SCHOOL_CANTON);
    const res = await page.goto('/StudentReport/Index');
    expect(res?.status()).toBeLessThan(400);
    await expect(page.locator('body')).toBeVisible();
  });

  test('secretaria: StudentAssignment', async ({ page }) => {
    await login(page, 'secretaria.canton@test.local', PASSWORD, SCHOOL_CANTON);
    const res = await page.goto('/StudentAssignment/Index');
    expect(res?.status()).toBeLessThan(400);
    await expect(page.locator('body')).toBeVisible();
  });
});

test.describe('Aislamiento escuela B (San Miguelito)', () => {
  test('admin San Miguelito accede a /User/Index', async ({ page }) => {
    await login(page, 'admin.sanmiguelito@test.local', PASSWORD, '6e42399f-6f17-4585-b92e-fa4fff02cb65');
    const res = await page.goto('/User/Index');
    expect(res?.status()).toBeLessThan(400);
  });
});
