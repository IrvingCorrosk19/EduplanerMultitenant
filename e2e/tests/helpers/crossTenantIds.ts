import type { Browser } from '@playwright/test';
import { login } from './auth';
import { PASSWORD, SCHOOL_SAN_MIGUELITO } from '../fixtures';

function resolveBaseURL(): string {
  const port = process.env.E2E_PORT ?? '5173';
  return process.env.BASE_URL ?? `http://localhost:${port}`;
}

/**
 * Obtiene un `User.Id` listado para admin San Miguelito (otra escuela respecto a Cantón).
 * Usa un **nuevo BrowserContext** para no depender de `logout` entre sesiones.
 * Override: `E2E_SM_CROSS_TENANT_USER_ID` o `E2E_SM_STUDENT_USER_ID`.
 */
export async function fetchCrossTenantUserIdFromSanMiguelitoIndex(browser: Browser): Promise<string> {
  const fromEnv = process.env.E2E_SM_CROSS_TENANT_USER_ID?.trim() ?? process.env.E2E_SM_STUDENT_USER_ID?.trim();
  if (fromEnv) return fromEnv;

  const baseURL = resolveBaseURL();
  const context = await browser.newContext({ baseURL });
  const page = await context.newPage();
  try {
    await login(page, 'admin.sanmiguelito@test.local', PASSWORD, SCHOOL_SAN_MIGUELITO);
    await page.goto('/User/Index');
    const btn = page.locator('#users-table tbody button.btn-edit-user[data-id]').first();
    await btn.waitFor({ state: 'visible', timeout: 45_000 });
    const id = await btn.getAttribute('data-id');
    if (!id) {
      throw new Error(
        'No hay usuarios editables en /User/Index (admin San Miguelito). ¿BD vacía o sin permisos?'
      );
    }
    return id;
  } finally {
    await context.close();
  }
}
