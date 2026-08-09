import type { Page } from '@playwright/test';

/**
 * GET autenticado (cookies de sesión). Devuelve status y `Location` de la **respuesta de documento**.
 * Con cookies, `Forbid()` suele ser **302** → `/Auth/AccessDenied` (ver `Program.cs` AccessDeniedPath).
 */
export async function sameOriginGetNavigation(
  page: Page,
  path: string
): Promise<{ status: number; location: string | undefined }> {
  const needle = path.split('?')[0];
  const responsePromise = page.waitForResponse(
    (res) =>
      res.request().resourceType() === 'document' &&
      (res.url().includes(needle) || res.url().endsWith(needle)),
    { timeout: 45_000 }
  );
  const gotoPromise = page.goto(path, { waitUntil: 'commit' });
  const res = await responsePromise;
  await gotoPromise;
  const loc = res.headers()['location'] ?? res.headers()['Location'];
  return { status: res.status(), location: loc };
}

export function assertOk200(
  r: { status: number; location?: string | undefined },
  message: string
): void {
  if (r.status !== 200) {
    throw new Error(`${message} (esperado HTTP 200, recibido ${r.status}, location=${r.location ?? '(none)'})`);
  }
}

/** Denegación explícita (403) o redirección a página de acceso denegado (cookie auth). */
export function assertForbiddenOrAccessDenied(
  r: { status: number; location: string | undefined },
  message: string
): void {
  const loc = (r.location ?? '').toLowerCase();
  const denied =
    r.status === 403 ||
    (r.status === 302 && (loc.includes('accessdenied') || loc.includes('/auth/accessdenied')));
  if (!denied) {
    throw new Error(`${message} (status=${r.status}, location=${r.location ?? '(none)'})`);
  }
}

/**
 * Login MVC con antiforgery. Opcional `schoolId` para seleccionar institución.
 */
export async function login(
  page: Page,
  email: string,
  password: string,
  schoolId?: string
): Promise<void> {
  await page.goto('/');
  // El toggle de contraseña expone aria-label "contraseña" en el botón; usar name= del input.
  await page.locator('input[name="Email"]').fill(email);
  await page.locator('input[name="Password"]').fill(password);
  if (schoolId) {
    const school = page.locator('select[name="SchoolId"]');
    await school.waitFor({ state: 'visible', timeout: 15_000 });
    await school.selectOption(schoolId);
  }
  await page.locator('#btnLogin').click();
  await page.waitForURL((url) => !url.pathname.toLowerCase().includes('/auth/login'), {
    timeout: 30_000,
  });
}

export async function logout(page: Page): Promise<void> {
  const signOut = page.getByRole('link', { name: /cerrar sesión|salir|logout/i });
  const signOutBtn = page.getByRole('button', { name: /cerrar sesión|salir|logout/i });
  if (await signOut.count()) await signOut.first().click();
  else if (await signOutBtn.count()) await signOutBtn.first().click();
  await page.waitForURL(/Login/i, { timeout: 15_000 }).catch(() => {});
}
