import { test, expect } from '@playwright/test';
import { login } from './helpers/auth';
import { PASSWORD, SCHOOL_CANTON } from './fixtures';

test.describe('Secretaria — asignación estudiante', () => {
  test('abrir asignación y modal de grado/grupo (gestión estudiantes)', async ({ page }) => {
    await login(page, 'secretaria.canton@test.local', PASSWORD, SCHOOL_CANTON);
    const res = await page.goto('/StudentAssignment/Index');
    expect(res?.status()).toBeLessThan(400);

    const editBtn = page.locator('#asignacionesTable tbody button').filter({ hasText: /editar/i }).first();
    if ((await editBtn.count()) === 0) {
      test.skip(true, 'Sin filas de estudiantes en StudentAssignment para la escuela E2E.');
    }
    await editBtn.click();
    await expect(page.locator('#gradeGroupModal')).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('#gradeGroupSelect')).toBeVisible();
  });
});
