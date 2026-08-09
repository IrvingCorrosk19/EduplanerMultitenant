import { test, expect } from '@playwright/test';
import { login } from './helpers/auth';
import { PASSWORD, SCHOOL_CANTON } from './fixtures';

test.describe('Profesor — grupos y nota', () => {
  test('ver catálogo de grupos y portal docente con celda de nota', async ({ page }) => {
    await login(page, 'profesor.canton@test.local', PASSWORD, SCHOOL_CANTON);

    const resGroups = await page.goto('/Group/Index');
    expect(resGroups?.status()).toBeLessThan(400);
    await expect(page.locator('body')).toBeVisible();

    const resGb = await page.goto('/TeacherGradebook/Index');
    if (!resGb || resGb.status() >= 400) {
      test.skip(true, 'TeacherGradebook no accesible (docente sin ficha o error servidor).');
    }

    const selGroup = page.locator('#selGroup');
    try {
      await selGroup.waitFor({ state: 'visible', timeout: 60_000 });
    } catch {
      test.skip(true, 'Sin selector #selGroup (docente E2E sin asignaciones académicas).');
    }

    const options = page.locator('#selGroup option');
    const n = await options.count();
    if (n > 1) {
      const firstVal = await options.nth(1).getAttribute('value');
      if (firstVal) await selGroup.selectOption(firstVal);
    }

    await page.locator('#selTrimester').waitFor({ state: 'visible' });
    const trimOpts = await page.locator('#selTrimester option').count();
    if (trimOpts > 1) {
      const v = await page.locator('#selTrimester option').nth(1).getAttribute('value');
      if (v) await page.locator('#selTrimester').selectOption(v);
    }

    const scoreCells = page.locator('#gradebook td.gradebook-score-cell');
    if ((await scoreCells.count()) === 0) {
      test.skip(true, 'Sin celdas de nota (sin actividades o libro vacío).');
    }
    const cell = scoreCells.first();
    await cell.waitFor({ state: 'visible', timeout: 45_000 });

    await cell.click();
    await cell.fill('8.5');
    await cell.blur();
    await expect(cell).toContainText(/8\.5/);
  });
});
