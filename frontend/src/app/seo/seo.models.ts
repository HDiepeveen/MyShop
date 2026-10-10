export interface ShopSeoSettings { heading: string; seoTitle: string; revision: string; shopName: string; welcomeText: string; introduction: string; }
export interface ProductSeoInfo {
  productId: string; name: string; revision: string;
  values: { seoTitle: string | null; seoDescription: string | null; webAddress: string | null };
  resolvedTitle: string; resolvedDescription: string; resolvedAddress: string;
}

export interface ProductTypeHeadings {
  productTypeId: string;
  aboutHeading: string | null;
  attributesHeading: string | null;
  revision: string;
}
