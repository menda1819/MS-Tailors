"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { adminApi, describeError } from "@/lib/admin-api";
import { ImageField, Thumb } from "@/components/admin/image-field";
import { useToast } from "@/components/ui/toast";
import {
  Button,
  Card,
  CardHeader,
  Checkbox,
  Field,
  Input,
  Select,
  Textarea,
} from "@/components/ui/primitives";
import { slugify } from "@/lib/format";
import type {
  BrandDetail,
  Category,
  PagedResponse,
  ProductDetail,
  ProductImage,
  Specification,
} from "@/types/admin";

interface ProductForm {
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
  tags: string[];
  isFeatured: boolean;
  isNewArrival: boolean;
  isActive: boolean;
  seoTitle: string;
  seoDescription: string;
}

const EMPTY: ProductForm = {
  name: "",
  slug: "",
  brandId: "",
  categoryIds: [],
  description: "",
  shortDescription: "",
  fabric: "",
  careInstructions: "",
  specifications: [],
  images: [],
  basePrice: 0,
  salePrice: null,
  tags: [],
  isFeatured: false,
  isNewArrival: false,
  isActive: true,
  seoTitle: "",
  seoDescription: "",
};

export function ProductForm({ product }: { product?: ProductDetail }) {
  const router = useRouter();
  const toast = useToast();

  const [form, setForm] = useState<ProductForm>(
    product
      ? {
          name: product.name,
          slug: product.slug,
          brandId: product.brandId,
          categoryIds: product.categoryIds,
          description: product.description,
          shortDescription: product.shortDescription,
          fabric: product.fabric,
          careInstructions: product.careInstructions,
          specifications: product.specifications,
          images: product.images,
          basePrice: product.basePrice,
          salePrice: product.salePrice,
          tags: product.tags,
          isFeatured: product.isFeatured,
          isNewArrival: product.isNewArrival,
          isActive: true,
          seoTitle: product.seoTitle ?? "",
          seoDescription: product.seoDescription ?? "",
        }
      : EMPTY,
  );

  const [brands, setBrands] = useState<BrandDetail[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      const [brandResponse, categoryResponse] = await Promise.allSettled([
        adminApi.get<PagedResponse<BrandDetail>>("/api/admin/brands?pageSize=100"),
        adminApi.get<Category[]>("/api/admin/categories/all"),
      ]);

      if (brandResponse.status === "fulfilled") setBrands(brandResponse.value.items);
      if (categoryResponse.status === "fulfilled") setCategories(categoryResponse.value);
    })();
  }, []);

  function set<K extends keyof ProductForm>(key: K, value: ProductForm[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function toggleCategory(id: string) {
    set(
      "categoryIds",
      form.categoryIds.includes(id)
        ? form.categoryIds.filter((existing) => existing !== id)
        : [...form.categoryIds, id],
    );
  }

  async function save() {
    setSaving(true);
    setError(null);

    const body = {
      ...form,
      slug: form.slug || slugify(form.name),
      salePrice: form.salePrice === 0 ? null : form.salePrice,
      seoTitle: form.seoTitle || null,
      seoDescription: form.seoDescription || null,
      images: form.images.map((image, index) => ({ ...image, order: index })),
    };

    try {
      if (product) {
        await adminApi.put(`/api/admin/products/${product.id}`, body);
        toast.notify("Product saved.");
        router.refresh();
      } else {
        const created = await adminApi.post<ProductDetail>("/api/admin/products", body);
        toast.notify("Product created. Add sizes and colours next.");
        router.push(`/admin/products/${created.id}`);
      }
    } catch (caught) {
      setError(describeError(caught));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader title="Details" />
        <div className="grid gap-4 p-5 sm:grid-cols-2">
          <Field label="Product name" htmlFor="name" required className="sm:col-span-2">
            <Input
              id="name"
              value={form.name}
              onChange={(event) => {
                set("name", event.target.value);
                // Keep the slug in step while it has not been edited by hand.
                if (!product && (!form.slug || form.slug === slugify(form.name))) {
                  set("slug", slugify(event.target.value));
                }
              }}
            />
          </Field>

          <Field
            label="URL slug"
            htmlFor="slug"
            required
            hint="Appears in the address as /product/your-slug."
          >
            <Input
              id="slug"
              value={form.slug}
              onChange={(event) => set("slug", slugify(event.target.value))}
            />
          </Field>

          <Field label="Brand" htmlFor="brandId" required>
            <Select
              id="brandId"
              value={form.brandId}
              onChange={(event) => set("brandId", event.target.value)}
            >
              <option value="">Choose a brand…</option>
              {brands.map((brand) => (
                <option key={brand.id} value={brand.id}>
                  {brand.name}
                </option>
              ))}
            </Select>
          </Field>

          <Field
            label="Short description"
            htmlFor="shortDescription"
            required
            className="sm:col-span-2"
            hint="One or two lines, shown on the product card."
          >
            <Textarea
              id="shortDescription"
              rows={2}
              value={form.shortDescription}
              onChange={(event) => set("shortDescription", event.target.value)}
            />
          </Field>

          <Field label="Full description" htmlFor="description" required className="sm:col-span-2">
            <Textarea
              id="description"
              rows={7}
              value={form.description}
              onChange={(event) => set("description", event.target.value)}
            />
          </Field>

          <Field label="Fabric" htmlFor="fabric">
            <Input
              id="fabric"
              value={form.fabric}
              placeholder="100% two-ply Egyptian cotton"
              onChange={(event) => set("fabric", event.target.value)}
            />
          </Field>

          <Field label="Care instructions" htmlFor="care">
            <Input
              id="care"
              value={form.careInstructions}
              placeholder="Machine wash cold. Hang dry."
              onChange={(event) => set("careInstructions", event.target.value)}
            />
          </Field>

          <Field label="Tags" htmlFor="tags" hint="Separate with commas. Used by search." className="sm:col-span-2">
            <Input
              id="tags"
              value={form.tags.join(", ")}
              onChange={(event) =>
                set(
                  "tags",
                  event.target.value
                    .split(",")
                    .map((tag) => tag.trim())
                    .filter(Boolean),
                )
              }
            />
          </Field>
        </div>
      </Card>

      <Card>
        <CardHeader
          title="Price"
          description="Prices are in LKR. Individual sizes and colours can override this."
        />
        <div className="grid gap-4 p-5 sm:grid-cols-2">
          <Field label="Base price" htmlFor="basePrice" required>
            <Input
              id="basePrice"
              type="number"
              min="0"
              step="0.01"
              className="tabular"
              value={form.basePrice}
              onChange={(event) => set("basePrice", Number(event.target.value) || 0)}
            />
          </Field>

          <Field
            label="Sale price"
            htmlFor="salePrice"
            hint="Leave blank when the product is not on sale. Must be below the base price."
          >
            <Input
              id="salePrice"
              type="number"
              min="0"
              step="0.01"
              className="tabular"
              value={form.salePrice ?? ""}
              onChange={(event) =>
                set("salePrice", event.target.value === "" ? null : Number(event.target.value))
              }
            />
          </Field>
        </div>
      </Card>

      <Card>
        <CardHeader
          title="Photographs"
          description="The first image is used on the product card. Alt text is required on every one."
        />
        <div className="flex flex-col gap-4 p-5">
          {form.images.map((image, index) => (
            <div
              key={`${image.url}-${index}`}
              className="flex flex-wrap items-start gap-4 rounded-tailored border border-sand-soft p-3"
            >
              <Thumb url={image.url} alt={image.alt} />

              <div className="min-w-56 flex-1">
                <Field label={`Alt text for image ${index + 1}`} htmlFor={`alt-${index}`} required>
                  <Input
                    id={`alt-${index}`}
                    value={image.alt}
                    placeholder="Navy poplin shirt, front view"
                    onChange={(event) => {
                      const next = [...form.images];
                      next[index] = { ...image, alt: event.target.value };
                      set("images", next);
                    }}
                  />
                </Field>
              </div>

              <div className="flex gap-1 pt-6">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  disabled={index === 0}
                  onClick={() => {
                    const next = [...form.images];
                    [next[index - 1], next[index]] = [next[index], next[index - 1]];
                    set("images", next);
                  }}
                >
                  Up
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="text-danger hover:bg-danger-soft hover:text-danger"
                  onClick={() => set("images", form.images.filter((_, i) => i !== index))}
                >
                  Remove
                </Button>
              </div>
            </div>
          ))}

          <ImageField
            value=""
            folder="products"
            label="photograph"
            onChange={(url) => {
              if (url) {
                set("images", [...form.images, { url, alt: "", order: form.images.length }]);
              }
            }}
          />
        </div>
      </Card>

      <Card>
        <CardHeader title="Specifications" description="Shown as a table on the product page." />
        <div className="flex flex-col gap-3 p-5">
          {form.specifications.map((specification, index) => (
            <div key={index} className="flex flex-wrap gap-3">
              <Input
                aria-label={`Specification ${index + 1} label`}
                placeholder="Collar"
                className="flex-1"
                value={specification.label}
                onChange={(event) => {
                  const next = [...form.specifications];
                  next[index] = { ...specification, label: event.target.value };
                  set("specifications", next);
                }}
              />
              <Input
                aria-label={`Specification ${index + 1} value`}
                placeholder="Semi-cutaway"
                className="flex-1"
                value={specification.value}
                onChange={(event) => {
                  const next = [...form.specifications];
                  next[index] = { ...specification, value: event.target.value };
                  set("specifications", next);
                }}
              />
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="text-danger hover:bg-danger-soft hover:text-danger"
                onClick={() =>
                  set("specifications", form.specifications.filter((_, i) => i !== index))
                }
              >
                Remove
              </Button>
            </div>
          ))}

          <div>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => set("specifications", [...form.specifications, { label: "", value: "" }])}
            >
              Add specification
            </Button>
          </div>
        </div>
      </Card>

      <Card>
        <CardHeader title="Placement" />
        <div className="flex flex-col gap-4 p-5">
          <fieldset>
            <legend className="text-[0.8125rem] font-medium text-ink">Categories</legend>
            <div className="mt-2 flex flex-wrap gap-x-6 gap-y-2">
              {categories.length === 0 ? (
                <p className="text-sm text-slate">
                  No categories exist yet. Create one before publishing this product.
                </p>
              ) : (
                categories.map((category) => (
                  <Checkbox
                    key={category.id}
                    label={category.name}
                    checked={form.categoryIds.includes(category.id)}
                    onChange={() => toggleCategory(category.id)}
                  />
                ))
              )}
            </div>
          </fieldset>

          <div className="flex flex-wrap gap-6">
            <Checkbox
              label="Featured"
              checked={form.isFeatured}
              onChange={(event) => set("isFeatured", event.target.checked)}
            />
            <Checkbox
              label="New arrival"
              checked={form.isNewArrival}
              onChange={(event) => set("isNewArrival", event.target.checked)}
            />
            <Checkbox
              label="Visible in the shop"
              checked={form.isActive}
              onChange={(event) => set("isActive", event.target.checked)}
            />
          </div>
        </div>
      </Card>

      <Card>
        <CardHeader title="Search engines" />
        <div className="grid gap-4 p-5 sm:grid-cols-2">
          <Field label="SEO title" htmlFor="seoTitle" hint="Up to 70 characters.">
            <Input
              id="seoTitle"
              value={form.seoTitle}
              onChange={(event) => set("seoTitle", event.target.value)}
            />
          </Field>
          <Field label="SEO description" htmlFor="seoDescription" hint="Up to 160 characters.">
            <Input
              id="seoDescription"
              value={form.seoDescription}
              onChange={(event) => set("seoDescription", event.target.value)}
            />
          </Field>
        </div>
      </Card>

      {error && (
        <p role="alert" className="rounded-tailored bg-danger-soft px-4 py-3 text-sm text-danger">
          {error}
        </p>
      )}

      <div className="sticky bottom-0 flex flex-wrap justify-end gap-2 border-t border-sand-soft bg-paper/95 py-3 backdrop-blur">
        <Button variant="secondary" onClick={() => router.push("/admin/products")} disabled={saving}>
          Cancel
        </Button>
        <Button onClick={save} disabled={saving}>
          {saving ? "Saving…" : product ? "Save changes" : "Create product"}
        </Button>
      </div>
    </div>
  );
}
