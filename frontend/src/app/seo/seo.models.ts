export interface ShopSeoSettings { heading: string; seoTitle: string; revision: string; shopName: string; welcomeText: string; introduction: string; footerText: string; company: CompanyPage; }
export interface CompanyPage { heading: string; name: string | null; description: string | null; address: string | null; email: string | null; phone: string | null; openingHours: string | null; }
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
