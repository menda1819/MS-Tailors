/** Mirrors the API DTOs. Kept hand-written so a field never leaks in silently. */

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export type UserRole = "Customer" | "Admin";

export type OrderStatus =
  | "New"
  | "Confirmed"
  | "Processing"
  | "Packed"
  | "Shipped"
  | "Delivered"
  | "Cancelled"
  | "Returned";

export type PaymentStatus = "Pending" | "Paid" | "Failed" | "Refunded";
export type PaymentMethod = "CashOnDelivery" | "Card" | "OnlineGateway";
export type CouponType = "Percentage" | "FixedAmount";
export type InquiryStatus = "New" | "Contacted" | "Closed";

export type ServiceNeeded =
  | "CustomSuit"
  | "CustomShirt"
  | "CustomTrousers"
  | "WeddingSuit"
  | "AlterationService"
  | "MeasurementAppointment"
  | "GeneralInquiry";

export type GalleryCategory =
  | "Suits"
  | "Shirts"
  | "Trousers"
  | "WeddingWear"
  | "Alterations"
  | "Uniforms"
  | "TailoringWork"
  | "Store";

// --- Auth -------------------------------------------------------------------

export interface AuthenticatedUser {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  role: UserRole;
  emailVerified: boolean;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}

// --- Catalogue --------------------------------------------------------------

export interface BrandDetail {
  id: string;
  name: string;
  slug: string;
  tagline: string;
  story: string;
  philosophy: string;
  targetAudience: string;
  logoUrl: string;
  heroImageUrl: string;
  seoTitle: string | null;
  seoDescription: string | null;
  productCount: number;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  parentCategoryId: string | null;
  imageUrl: string;
  displayOrder: number;
  children: Category[];
}

export interface ProductImage {
  url: string;
  alt: string;
  order: number;
}

export interface Specification {
  label: string;
  value: string;
}

export interface ProductVariant {
  sku: string;
  size: string;
  color: string;
  colorHex: string;
  stockQuantity: number;
  inStock: boolean;
  price: number;
  imageUrl: string | null;
}

export interface ProductDetail {
  id: string;
  name: string;
  slug: string;
  brandId: string;
  categoryIds: string[];
  description: string;
  shortDescription: string;
  fabric: string;
  careInstructions: string;
  specifications: Specification[];
  images: ProductImage[];
  basePrice: number;
  salePrice: number | null;
  effectivePrice: number;
  currency: string;
  isOnSale: boolean;
  variants: ProductVariant[];
  sizeGuideId: string | null;
  tags: string[];
  isFeatured: boolean;
  isNewArrival: boolean;
  inStock: boolean;
  totalStock: number;
  seoTitle: string | null;
  seoDescription: string | null;
}

// --- Content ----------------------------------------------------------------

export interface ServiceDetail {
  id: string;
  name: string;
  slug: string;
  category: string;
  shortDescription: string;
  longDescription: string;
  imageUrl: string;
  bulletPoints: string[];
  displayOrder: number;
}

export interface GalleryItem {
  id: string;
  imageUrl: string;
  thumbnailUrl: string;
  caption: string;
  altText: string;
  category: GalleryCategory;
  categoryLabel: string;
  displayOrder: number;
}

export interface Testimonial {
  id: string;
  customerName: string;
  customerTitle: string | null;
  quote: string;
  rating: number;
  imageUrl: string | null;
  displayOrder: number;
}

export interface BlogPostDetail {
  id: string;
  title: string;
  slug: string;
  excerpt: string;
  content: string;
  coverImageUrl: string;
  author: string;
  tags: string[];
  publishedAt: string | null;
}

// --- Orders -----------------------------------------------------------------

export interface OrderItem {
  productId: string;
  brandId: string;
  productName: string;
  variantSku: string;
  size: string;
  color: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderStatusChange {
  status: OrderStatus;
  changedAt: string;
  changedBy: string;
  note: string | null;
}

export interface Order {
  id: string;
  orderNumber: string;
  customerId: string | null;
  contact: { fullName: string; email: string; phone: string };
  shippingAddress: {
    line1: string;
    line2: string | null;
    city: string;
    postalCode: string | null;
  };
  specialInstructions: string | null;
  items: OrderItem[];
  subtotal: number;
  discountAmount: number;
  couponCode: string | null;
  deliveryFee: number;
  total: number;
  currency: string;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  orderStatus: OrderStatus;
  statusHistory: OrderStatusChange[];
  trackingNumber: string | null;
  placedAt: string;
}

export interface OrderSummary {
  id: string;
  orderNumber: string;
  total: number;
  currency: string;
  paymentStatus: PaymentStatus;
  orderStatus: OrderStatus;
  itemCount: number;
  placedAt: string;
}

// --- Customers --------------------------------------------------------------

export interface AdminCustomer {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  role: UserRole;
  emailVerified: boolean;
  isActive: boolean;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface Address {
  label: string;
  line1: string;
  line2: string | null;
  city: string;
  postalCode: string | null;
  isDefault: boolean;
}

export interface AdminCustomerDetail {
  customer: AdminCustomer;
  addresses: Address[];
  recentOrders: OrderSummary[];
  orderCount: number;
  lifetimeValue: number;
  currency: string;
}

// --- Inquiries --------------------------------------------------------------

export interface Inquiry {
  id: string;
  name: string;
  email: string;
  phone: string;
  serviceNeeded: ServiceNeeded;
  serviceNeededLabel: string;
  message: string;
  preferredDate: string | null;
  status: InquiryStatus;
  receivedAt: string;
}

// --- Coupons ----------------------------------------------------------------

export interface Coupon {
  id: string;
  code: string;
  type: CouponType;
  value: number;
  minOrderTotal: number;
  maxDiscount: number | null;
  usageLimit: number | null;
  usedCount: number;
  startsAt: string;
  expiresAt: string;
  isActive: boolean;
}

// --- Dashboard --------------------------------------------------------------

export interface DashboardData {
  sales: { total: number; today: number; month: number; currency: string };
  totalOrders: number;
  todayOrders: number;
  ordersByStatus: { status: OrderStatus; count: number }[];
  customerCount: number;
  newInquiryCount: number;
  lowStockVariants: LowStockVariant[];
  bestSellers: {
    productId: string;
    productName: string;
    productSlug: string;
    unitsSold: number;
    price: number;
  }[];
  bestPerformingBrands: {
    brandId: string;
    brandName: string;
    brandSlug: string;
    unitsSold: number;
    revenue: number;
    productCount: number;
  }[];
  generatedAtUtc: string;
}

export interface LowStockVariant {
  productId: string;
  productName: string;
  productSlug: string;
  sku: string;
  size: string;
  color: string;
  stockQuantity: number;
}

// --- Media ------------------------------------------------------------------

export interface UploadedMedia {
  key: string;
  url: string;
  width: number;
  height: number;
  renditions: { url: string; width: number; height: number; bytes: number }[];
}

export interface BulkStockResult {
  updatedCount: number;
  problems: string[];
}
