import { defineConfig } from 'vitest/config';

export default defineConfig({
  // Bound process startup to keep the suite reliable on busy development machines.
  test: { maxWorkers: 2 },
});
