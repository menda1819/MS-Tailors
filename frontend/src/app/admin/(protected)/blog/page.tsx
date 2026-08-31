"use client";

import { ResourceManager, type FieldDef } from "@/components/admin/resource-manager";
import { Thumb } from "@/components/admin/image-field";
import { Badge } from "@/components/ui/primitives";
import { formatDate } from "@/lib/format";
import type { BlogPostDetail } from "@/types/admin";

const FIELDS: FieldDef[] = [
  { name: "title", label: "Title", kind: "text", required: true },
  { name: "slug", label: "URL slug", kind: "slug", required: true, span: "half", derivedFrom: "title" },
  { name: "author", label: "Author", kind: "text", required: true, span: "half" },
  { name: "excerpt", label: "Excerpt", kind: "textarea", required: true, rows: 3, hint: "Shown on the blog index and in search results." },
  { name: "content", label: "Article", kind: "textarea", required: true, rows: 14 },
  { name: "coverImageUrl", label: "Cover image", kind: "image", folder: "blog" },
  { name: "tags", label: "Tags", kind: "tags", span: "half", hint: "Separate with commas." },
  { name: "publishedAt", label: "Publish date", kind: "datetime", span: "half", hint: "Leave blank to publish immediately when you tick Published." },
  {
    name: "isPublished",
    label: "Published",
    kind: "checkbox",
    hint: "A post dated in the future stays hidden until that date passes.",
  },
];

export default function BlogPage() {
  return (
    <ResourceManager<BlogPostDetail>
      eyebrow="Content"
      title="Blog"
      description="Posts appear publicly only once published and once their publish date has passed."
      endpoint="/api/admin/blog"
      rowKey={(post) => post.id}
      itemNoun="post"
      itemNounPlural="posts"
      describeRow={(post) => post.title}
      emptyTitle="No posts yet"
      emptyDescription="Write about fabrics, fittings and the work you do. It gives customers a reason to come back."
      columns={[
        { key: "cover", header: "", render: (post) => <Thumb url={post.coverImageUrl} alt={post.title} /> },
        {
          key: "title",
          header: "Post",
          render: (post) => (
            <div className="min-w-0">
              <p className="truncate font-medium text-ink">{post.title}</p>
              <p className="truncate text-xs text-slate">{post.excerpt}</p>
            </div>
          ),
        },
        { key: "author", header: "Author", secondary: true, render: (post) => post.author },
        {
          key: "published",
          header: "Published",
          align: "right",
          render: (post) =>
            post.publishedAt ? (
              <span className="text-sm text-slate">{formatDate(post.publishedAt)}</span>
            ) : (
              <Badge tone="warning">Draft</Badge>
            ),
        },
      ]}
      fields={FIELDS}
      toForm={(post) => ({
        title: post.title,
        slug: post.slug,
        excerpt: post.excerpt,
        content: post.content,
        coverImageUrl: post.coverImageUrl,
        author: post.author,
        tags: post.tags,
        publishedAt: post.publishedAt,
        isPublished: post.publishedAt !== null,
      })}
      emptyForm={{
        title: "",
        slug: "",
        excerpt: "",
        content: "",
        coverImageUrl: "",
        author: "MS Tailors",
        tags: [],
        publishedAt: null,
        isPublished: false,
      }}
    />
  );
}
