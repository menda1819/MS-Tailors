"use client";

import { useRef, useState } from "react";
import Image from "next/image";
import { adminApi, describeError } from "@/lib/admin-api";
import { Button, cx } from "@/components/ui/primitives";
import type { UploadedMedia } from "@/types/admin";

/**
 * Uploads an image and hands back its URL.
 *
 * The API resizes to every rendition on upload, so the value stored on the
 * entity is the largest one and the smaller sizes already exist alongside it.
 */
export function ImageField({
  value,
  onChange,
  folder,
  label = "image",
}: {
  value: string;
  onChange: (url: string) => void;
  folder: string;
  label?: string;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [dragging, setDragging] = useState(false);

  async function upload(file: File) {
    setBusy(true);
    setError(null);

    try {
      const form = new FormData();
      form.append("file", file);

      const uploaded = await adminApi.upload<UploadedMedia>(
        `/api/admin/media?folder=${encodeURIComponent(folder)}`,
        form,
      );

      onChange(uploaded.url);
    } catch (caught) {
      setError(describeError(caught));
    } finally {
      setBusy(false);
      if (inputRef.current) inputRef.current.value = "";
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <div
        onDragOver={(event) => {
          event.preventDefault();
          setDragging(true);
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={(event) => {
          event.preventDefault();
          setDragging(false);
          const file = event.dataTransfer.files?.[0];
          if (file) void upload(file);
        }}
        className={cx(
          "flex items-center gap-4 rounded-tailored border border-dashed p-3",
          dragging ? "border-indigo bg-indigo-soft/40" : "border-sand",
        )}
      >
        {value ? (
          <div className="relative size-20 shrink-0 overflow-hidden rounded-tailored bg-paper-sunk">
            {/* Remote hosts must be allow-listed for next/image, so uploaded
                images render through a plain img rather than failing loudly. */}
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={value}
              alt={`Current ${label}`}
              className="size-full object-cover"
            />
          </div>
        ) : (
          <div className="flex size-20 shrink-0 items-center justify-center rounded-tailored bg-paper-sunk text-[0.625rem] uppercase tracking-[0.12em] text-ash">
            None
          </div>
        )}

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              disabled={busy}
              onClick={() => inputRef.current?.click()}
            >
              {busy ? "Uploading…" : value ? `Replace ${label}` : `Upload ${label}`}
            </Button>
            {value && (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={busy}
                onClick={() => onChange("")}
              >
                Remove
              </Button>
            )}
          </div>
          <p className="mt-1.5 text-xs text-slate">
            JPEG, PNG or WebP up to 10 MB. Drag a file here, or browse. Smaller
            sizes are generated automatically.
          </p>
        </div>

        <input
          ref={inputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp,image/avif"
          className="sr-only"
          onChange={(event) => {
            const file = event.target.files?.[0];
            if (file) void upload(file);
          }}
        />
      </div>

      {error && (
        <p role="alert" className="text-xs text-danger">
          {error}
        </p>
      )}
    </div>
  );
}

/** Renders a stored image URL in a table cell, with a placeholder when empty. */
export function Thumb({ url, alt }: { url: string | null; alt: string }) {
  if (!url) {
    return (
      <div className="size-10 rounded-tailored bg-paper-sunk" aria-hidden="true" />
    );
  }

  return (
    <div className="relative size-10 overflow-hidden rounded-tailored bg-paper-sunk">
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img src={url} alt={alt} className="size-full object-cover" />
    </div>
  );
}

export { Image as NextImage };
