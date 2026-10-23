export interface WebsiteLink {
  key: string;
  name: string;
  path: string;
  queryParams?: { categoryId: string };
}
export function orderedWebsiteLinks(links: readonly WebsiteLink[], order: readonly string[] = []): WebsiteLink[] {
  const positions = new Map(order.map((key, index) => [key, index]));
  return [...links].sort((a, b) => (positions.get(a.key) ?? order.length) - (positions.get(b.key) ?? order.length));
}
