"use client";

import { ResourceManager, type FieldDef } from "@/components/admin/resource-manager";
import { Thumb } from "@/components/admin/image-field";
import { countOf } from "@/lib/format";
import type { BrandDetail } from "@/types/admin";

const FIELDS: FieldDef[] = [
  { name: "name", label: "Brand name", kind: "text", required: true, span: "half" },
  {
    name: "slug",
    label: "URL slug",
    kind: "slug",
    required: true,
    span: "half",
    derivedFrom: "name",
    hint: "Appears in the address as /brands/your-slug.",
  },
  { name: "tagline", label: "Tagline", kind: "text", required: true, hint: "One short line shown under the brand name." },
  { name: "story", label: "Story", kind: "textarea", required: true, rows: 6, hint: "The brand's own account of itself, shown on its page." },
  { name: "philosophy", label: "Philosophy", kind: "textarea", rows: 3 },
  { name: "targetAudience", label: "Who it is for", kind: "textarea", rows: 2 },
  { name: "logoUrl", label: "Logo", kind: "image", folder: "brands", required: true },
  { name: "heroImageUrl", label: "Hero image", kind: "image", folder: "brands", required: true },
  { name: "displayOrder", label: "Display order", kind: "number", span: "half", hint: "Lower numbers appear first." },
  { name: "isActive", label: "Visible in the shop", kind: "checkbox", span: "half" },
  { name: "seoTitle", label: "SEO title", kind: "text", span: "half", hint: "Up to 70 characters." },
  { name: "seoDescription", label: "SEO description", kind: "text", span: "half", hint: "Up to 160 characters." },
];

export default function BrandsPage() {
  return (
    <ResourceManager<BrandDetail>
      eyebrow="Catalogue"
      title="Brands"
      description="Each brand gets its own page in the shop. Brands with products cannot be deleted — make them invisible instead."
      endpoint="/api/admin/brands"
      rowKey={(brand) => brand.id}
      itemNoun="brand"
      itemNounPlural="brands"
      describeRow={(brand) => brand.name}
      emptyTitle="No brands yet"
      emptyDescription="Add the first brand to start building the shop. Every product belongs to one."
      columns={[
        {
          key: "logo",
          header: "",
          render: (brand) => <Thumb url={brand.logoUrl} alt={`${brand.name} logo`} />,
        },
        {
          key: "name",
          header: "Brand",
          render: (brand) => (
            <div className="min-w-0">
              <p className="truncate font-medium text-ink">{brand.name}</p>
              <p className="truncate text-xs text-slate">{brand.tagline}</p>
            </div>
          ),
        },
        {
          key: "slug",
          header: "Slug",
          secondary: true,
          render: (brand) => <code className="text-xs text-slate">/{brand.slug}</code>,
        },
        {
          key: "products",
          header: "Products",
          align: "right",
          render: (brand) => (
            <span className="text-sm text-slate">{countOf(brand.productCount, "product")}</span>
          ),
        },
      ]}
      fields={FIELDS}
      toForm={(brand) => ({
        name: brand.name,
        slug: brand.slug,
        tagline: brand.tagline,
        story: brand.story,
        philosophy: brand.philosophy,
        targetAudience: brand.targetAudience,
        logoUrl: brand.logoUrl,
        heroImageUrl: brand.heroImageUrl,
        displayOrder: 0,
        isActive: true,
        seoTitle: brand.seoTitle ?? "",
        seoDescription: brand.seoDescription ?? "",
      })}
      emptyForm={{
        name: "",
        slug: "",
        tagline: "",
        story: "",
        philosophy: "",
        targetAudience: "",
        logoUrl: "",
        heroImageUrl: "",
        displayOrder: 0,
        isActive: true,
        seoTitle: "",
        seoDescription: "",
      }}
    />
  );
}
