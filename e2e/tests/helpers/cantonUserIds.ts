import type { Browser } from '@playwright/test';
import { login } from './auth';
import { PASSWORD, SCHOOL_CANTON } from '../fixtures';

function resolveBaseURL(): string {
  const port = process.env.E2E_PORT ?? '5173';
  return process.env.BASE_URL ?? `http://localhost:${port}`;
}

/** Primer usuario editable en Cantón (misma escuela que el admin Cantón). */
export async function fetchCantonEditableUserId(browser: Browser): Promise<string> {
  const baseURL = resolveBaseURL();
  const context = await browser.newContext({ baseURL });
  const page = await context.newPage();
  try {
    await login(page, 'admin.canton@test.local', PASSWORD, SCHOOL_CANTON);
    await page.goto('/User/Index');
    const btn = page.locator('#users-table tbody button.btn-edit-user[data-id]').first();
    await btn.waitFor({ state: 'visible', timeout: 45_000 });
    const id = await btn.getAttribute('data-id');
    if (!id) throw new Error('No hay usuario editable en User/Index (Cantón).');
    return id;
  } finally {
    await context.close();
  }
}
