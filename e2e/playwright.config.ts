import { defineConfig, devices } from '@playwright/test';

const e2ePort = process.env.E2E_PORT ?? '5173';
const baseURL = process.env.BASE_URL ?? `http://localhost:${e2ePort}`;
const isCi = process.env.CI === 'true' || process.env.CI === '1';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!isCi,
  retries: isCi ? 1 : 0,
  workers: 1,
  timeout: 120_000,
  reporter: [
    ['html', { outputFolder: 'playwright-report/html', open: 'never' }],
    ['json', { outputFile: 'playwright-report/results.json' }],
    ['list'],
  ],
  use: {
    baseURL,
    trace: 'on',
    screenshot: 'on',
    video: 'on',
    headless: isCi ? true : process.env.PLAYWRIGHT_HEADLESS === '1',
    actionTimeout: 25_000,
    navigationTimeout: 60_000,
  },
  outputDir: 'playwright-report/artifacts',
  webServer: {
    command: `dotnet publish SchoolManager.csproj -c Release -o .e2e_publish -v minimal /p:UseAppHost=false && dotnet .e2e_publish\\SchoolManager.dll --urls http://127.0.0.1:${e2ePort}`,
    cwd: '..',
    url: `${baseURL}/`,
    reuseExistingServer: process.env.E2E_REUSE_SERVER === '1',
    timeout: 180_000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
