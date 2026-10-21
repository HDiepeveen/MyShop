export interface ShopSeoSettings { assortmentImageUrl?: string | null; assortmentImageMode?: string | null; showHeaderPhone?: boolean; showHeaderWhatsapp?: boolean; seoDescription?: string | null; homeImageUrl?: string | null; headerText?: string | null; heading: string; seoTitle: string; revision: string; shopName: string; welcomeText: string; introduction: string; footerText: string; company: CompanyPage; home?: HomePageSettings; }
export interface CompanyPage { imageUrl?: string | null; imageMode?: string | null; seoTitle?: string | null; seoDescription?: string | null; heading: string; name: string | null; description: string | null; address: string | null; email: string | null; phone: string | null; openingHours: string | null; }
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

export interface HomePageSettings { imageMode?: string | null; seoTitle?: string | null; seoDescription?: string | null; welcomeText?: string | null; imageUrl?: string | null; place?: string | null; description?: string | null; enabled: boolean; heading: string; items: { pageId?: string; imageUrl?: string | null; imageMode?: string | null; seoTitle?: string | null; seoDescription?: string | null; title: string | null; place?: string | null; webAddress?: string | null; description: string }[]; }
