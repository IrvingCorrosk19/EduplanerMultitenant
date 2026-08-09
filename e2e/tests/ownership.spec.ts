import { test, expect } from '@playwright/test';
import {
  login,
  sameOriginGetNavigation,
  assertForbiddenOrAccessDenied,
  assertOk200,
} from './helpers/auth';
import { fetchCrossTenantUserIdFromSanMiguelitoIndex } from './helpers/crossTenantIds';
import { fetchCantonEditableUserId } from './helpers/cantonUserIds';
import { PASSWORD, SCHOOL_CANTON } from './fixtures';

test.describe('Ownership — ID válido (200)', () => {
  test.describe.configure({ timeout: 120_000 });

  test('admin Canton: /User/Edit/{usuario misma escuela} → 200', async ({ page, browser }) => {
    const userId = await fetchCantonEditableUserId(browser);
    await login(page, 'admin.canton@test.local', PASSWORD, SCHOOL_CANTON);
    const nav = await sameOriginGetNavigation(page, `/User/Edit/${userId}`);
    assertOk200(nav, 'Admin debe poder abrir edición de usuario de su escuela');
  });

  test('estudiante Canton: /Student/Details/{propio id} → 200', async ({ page }) => {
    await login(page, 'estudiante.canton@test.local', PASSWORD, SCHOOL_CANTON);
    await page.goto('/StudentReport/Index');
    const studentId = await page.locator('#student-id').getAttribute('value');
    expect(studentId && studentId.length > 10, '#student-id en StudentReport').toBeTruthy();
    const nav = await sameOriginGetNavigation(page, `/Student/Details/${studentId}`);
    assertOk200(nav, 'Estudiante debe ver su propio detalle');
  });
});

test.describe('Ownership multi-escuela (403)', () => {
  test.describe.configure({ timeout: 120_000 });

  test('admin Canton: /User/Edit/{usuario otra escuela} → 403', async ({ page, browser }) => {
    const otherSchoolUserId = await fetchCrossTenantUserIdFromSanMiguelitoIndex(browser);
    await login(page, 'admin.canton@test.local', PASSWORD, SCHOOL_CANTON);
    const nav = await sameOriginGetNavigation(page, `/User/Edit/${otherSchoolUserId}`);
    assertForbiddenOrAccessDenied(nav, 'Debe denegar edición de usuario de otra escuela');
  });

  test('estudiante Canton: /Student/Details/{id estudiante otra escuela} → 403', async ({ page, browser }) => {
    const otherSchoolUserId = await fetchCrossTenantUserIdFromSanMiguelitoIndex(browser);
    await login(page, 'estudiante.canton@test.local', PASSWORD, SCHOOL_CANTON);
    const nav = await sameOriginGetNavigation(page, `/Student/Details/${otherSchoolUserId}`);
    assertForbiddenOrAccessDenied(nav, 'Debe denegar detalle de otro estudiante (403 o AccessDenied)');
  });
});
