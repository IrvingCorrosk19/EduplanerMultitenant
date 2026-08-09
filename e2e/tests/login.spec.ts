import { test, expect } from '@playwright/test';
import { PASSWORD, SCHOOL_CANTON } from './fixtures';

test.describe('Login MVC – resolución por correo', () => {
  test('selector de institución oculto al cargar (sin listar todos los colegios)', async ({ page }) => {
    await page.goto('/Auth/Login');
    await expect(page.locator('input[name="Email"]')).toBeVisible();
    const schoolGroup = page.locator('#schoolSelectorGroup');
    await expect(schoolGroup).toBeHidden();
  });

  test('login exitoso con un solo colegio: sin selector obligatorio', async ({ page }) => {
    await page.goto('/Auth/Login');
    await page.locator('input[name="Email"]').fill('admin.canton@test.local');
    await page.locator('input[name="Email"]').blur();
    await page.waitForTimeout(600);
    // Correo de un solo tenant → selector permanece oculto o vacío
    const schoolGroup = page.locator('#schoolSelectorGroup');
    const visible = await schoolGroup.isVisible().catch(() => false);
    if (visible) {
      // Solo si el backend devolvió multi (no esperado para canton único)
      await page.locator('select[name="SchoolId"]').selectOption(SCHOOL_CANTON);
    }
    await page.locator('input[name="Password"]').fill(PASSWORD);
    await page.locator('#btnLogin').click();
    await page.waitForURL((url) => !url.pathname.toLowerCase().includes('/auth/login'), { timeout: 30_000 });
    await expect(page).not.toHaveURL(/\/Auth\/Login/i);
  });

  test('ResolveLoginSchools no enumera colegios ajenos para correo único', async ({ request }) => {
    const res = await request.get('/Auth/ResolveLoginSchools?email=admin.canton@test.local');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.requiresSelection).toBeFalsy();
    expect(Array.isArray(body.schools)).toBeTruthy();
    expect(body.schools.length).toBe(0);
  });
});
