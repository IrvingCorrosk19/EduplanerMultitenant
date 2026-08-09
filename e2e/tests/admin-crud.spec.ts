import { test, expect } from '@playwright/test';
import { login } from './helpers/auth';
import { PASSWORD, SCHOOL_CANTON } from './fixtures';

async function dismissSwal(page: import('@playwright/test').Page): Promise<void> {
  const confirm = page.locator('.swal2-confirm');
  await confirm.waitFor({ state: 'visible', timeout: 30_000 });
  await confirm.click();
}

test.describe('Admin — CRUD usuarios (UI real)', () => {
  test('User/Index: crear usuario y editar vía /User/Edit/{id}', async ({ page }) => {
    const stamp = Date.now();
    const email = `e2e.crud.${stamp}@test.local`;
    const docId = `E2E-${stamp}`;

    await login(page, 'admin.canton@test.local', PASSWORD, SCHOOL_CANTON);
    await page.goto('/User/Index');
    await expect(page.locator('#userForm')).toBeVisible();

    await page.locator('#userForm input[name="Name"]').fill('E2E');
    await page.locator('#userForm input[name="LastName"]').fill('Creado');
    await page.locator('#userForm input[name="Email"]').fill(email);
    await page.locator('#userForm input[name="DocumentId"]').fill(docId);
    await page.locator('#userForm input[name="PasswordHash"]').fill('Test#2026');
    await page.locator('#userForm select[name="Role"]').selectOption('Inspector');

    const [createRes] = await Promise.all([
      page.waitForResponse(
        (r) => r.url().includes('/User/CreateJson') && r.request().method() === 'POST',
        { timeout: 45_000 }
      ),
      page.locator('#btnCreateUser').click(),
    ]);
    expect(createRes.ok(), `CreateJson: ${createRes.status()}`).toBeTruthy();
    const created = (await createRes.json()) as { id?: string };
    expect(created.id, 'CreateJson debe devolver id').toBeTruthy();

    await dismissSwal(page);
    await page.waitForLoadState('networkidle');

    await page.goto(`/User/Edit/${created.id}`);
    await expect(page.locator('input[name="Email"]')).toHaveValue(email, { timeout: 20_000 });
    await page.locator('input[name="LastName"]').fill('EditadoE2E');

    // HTML5 + jQuery pueden bloquear el submit; forzar envío del formulario principal (no el de foto).
    const editForm = page.locator('form[method="post"]').filter({ has: page.locator('input[name="Id"]') });
    await editForm.evaluate((f) => f.setAttribute('novalidate', 'novalidate'));

    const postPromise = page.waitForResponse(
      (r) =>
        r.url().includes('/User/Edit') &&
        r.request().method() === 'POST' &&
        !r.url().includes('UpdatePhoto') &&
        !r.url().includes('RemovePhoto'),
      { timeout: 45_000 }
    );
    await page.getByRole('button', { name: /Guardar Cambios/i }).click();
    const post = await postPromise;
    expect([302, 303].includes(post.status()), `POST User/Edit esperaba 302/303, recibió ${post.status()}`).toBeTruthy();

    await page.waitForURL((u) => !u.pathname.includes('/User/Edit'), {
      timeout: 45_000,
      waitUntil: 'domcontentloaded',
    });
    await expect(page.locator('body')).toBeVisible();
  });
});
