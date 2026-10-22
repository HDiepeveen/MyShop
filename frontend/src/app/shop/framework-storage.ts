// Read the old key once so an open payment can still be checked after the rename.
export function migrateFrameworkStorage(suffix: string): string {
  const key = 'HDWebFramework.' + suffix;
  const previous = 'myshop.' + suffix;
  try {
    const existing = localStorage.getItem(key);
    const legacy = localStorage.getItem(previous);
    if (existing === null && legacy !== null) localStorage.setItem(key, legacy);
    localStorage.removeItem(previous);
  } catch { /* The cart and payment screens also support unavailable browser storage. */ }
  return key;
}
