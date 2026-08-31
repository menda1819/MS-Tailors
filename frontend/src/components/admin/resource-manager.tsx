"use client";

import { useCallback, useMemo, useState, type ReactNode } from "react";
import { adminApi, describeError } from "@/lib/admin-api";
import { usePaged } from "@/components/admin/use-paged";
import { Column, DataTable, Pagination } from "@/components/admin/data-table";
import { ImageField } from "@/components/admin/image-field";
import { ConfirmDialog, Modal } from "@/components/ui/dialog";
import { useToast } from "@/components/ui/toast";
import {
  Button,
  Card,
  Checkbox,
  Field,
  Input,
  PageHeader,
  Select,
  Textarea,
} from "@/components/ui/primitives";
import { slugify } from "@/lib/format";

export type FormValues = Record<string, unknown>;

export type FieldKind =
  | "text"
  | "slug"
  | "textarea"
  | "number"
  | "money"
  | "select"
  | "checkbox"
  | "tags"
  | "lines"
  | "image"
  | "datetime";

export interface FieldDef {
  name: string;
  label: string;
  kind: FieldKind;
  required?: boolean;
  hint?: string;
  options?: { value: string; label: string }[];
  /** image only — the storage folder the upload goes to. */
  folder?: string;
  /** slug only — the text field it is derived from until edited by hand. */
  derivedFrom?: string;
  /** Layout: half-width fields pair up on wide screens. */
  span?: "full" | "half";
  rows?: number;
}

interface ResourceManagerProps<T> {
  eyebrow: string;
  title: string;
  description?: string;
  endpoint: string;
  columns: Column<T>[];
  fields: FieldDef[];
  rowKey: (row: T) => string;
  /** Entity → form values when opening the editor. */
  toForm: (row: T) => FormValues;
  /**
   * Values for a brand-new record. A factory rather than an object so a default
   * that depends on the clock stays out of render.
   */
  emptyForm: FormValues | (() => FormValues);
  /** Form values → request body. Defaults to the values unchanged. */
  toBody?: (values: FormValues) => unknown;
  itemNoun: string;
  itemNounPlural: string;
  /** Names the record in the delete confirmation, so it is unambiguous. */
  describeRow: (row: T) => string;
  emptyTitle: string;
  emptyDescription: string;
  pageSize?: number;
  filters?: ReactNode;
  extraQuery?: Record<string, string | number | undefined>;
}

/**
 * List, create, edit and delete for a simple resource.
 *
 * Seven admin sections differ only in their fields and columns, so they share
 * this rather than seven near-identical pages — which is also what keeps
 * pagination, confirmation dialogs and error handling consistent across them.
 */
export function ResourceManager<T>({
  eyebrow,
  title,
  description,
  endpoint,
  columns,
  fields,
  rowKey,
  toForm,
  emptyForm,
  toBody,
  itemNoun,
  itemNounPlural,
  describeRow,
  emptyTitle,
  emptyDescription,
  pageSize = 20,
  filters,
  extraQuery,
}: ResourceManagerProps<T>) {
  const [page, setPage] = useState(1);
  const { items, meta, loading, error, reload } = usePaged<T>(endpoint, {
    page,
    pageSize,
    ...extraQuery,
  });

  const toast = useToast();

  const [editing, setEditing] = useState<T | null>(null);
  const [creating, setCreating] = useState(false);
  const resolveEmpty = useCallback(
    () => (typeof emptyForm === "function" ? emptyForm() : emptyForm),
    [emptyForm],
  );

  const [values, setValues] = useState<FormValues>(resolveEmpty);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const [pendingDelete, setPendingDelete] = useState<T | null>(null);
  const [deleting, setDeleting] = useState(false);

  const openCreate = useCallback(() => {
    setValues(resolveEmpty());
    setFormError(null);
    setCreating(true);
  }, [resolveEmpty]);

  const openEdit = useCallback(
    (row: T) => {
      setValues(toForm(row));
      setFormError(null);
      setEditing(row);
    },
    [toForm],
  );

  const closeForm = useCallback(() => {
    setCreating(false);
    setEditing(null);
    setFormError(null);
  }, []);

  const setValue = useCallback((name: string, value: unknown) => {
    setValues((current) => ({ ...current, [name]: value }));
  }, []);

  async function save() {
    setSaving(true);
    setFormError(null);

    const body = toBody ? toBody(values) : values;

    try {
      if (editing) {
        await adminApi.put(`${endpoint}/${rowKey(editing)}`, body);
        toast.notify(`${capitalise(itemNoun)} saved.`);
      } else {
        await adminApi.post(endpoint, body);
        toast.notify(`${capitalise(itemNoun)} created.`);
      }

      closeForm();
      await reload();
    } catch (caught) {
      setFormError(describeError(caught));
    } finally {
      setSaving(false);
    }
  }

  async function confirmDelete() {
    if (!pendingDelete) return;

    setDeleting(true);

    try {
      await adminApi.delete(`${endpoint}/${rowKey(pendingDelete)}`);
      toast.notify(`${capitalise(itemNoun)} deleted.`);
      setPendingDelete(null);
      await reload();
    } catch (caught) {
      toast.fail(describeError(caught));
    } finally {
      setDeleting(false);
    }
  }

  const actionColumn = useMemo<Column<T>>(
    () => ({
      key: "__actions",
      header: "",
      align: "right",
      render: (row) => (
        <div className="flex justify-end gap-1">
          <Button variant="ghost" size="sm" onClick={() => openEdit(row)}>
            Edit
          </Button>
          <Button
            variant="ghost"
            size="sm"
            className="text-danger hover:bg-danger-soft hover:text-danger"
            onClick={() => setPendingDelete(row)}
          >
            Delete
          </Button>
        </div>
      ),
    }),
    [openEdit],
  );

  const open = creating || editing !== null;

  return (
    <>
      <PageHeader
        eyebrow={eyebrow}
        title={title}
        description={description}
        actions={<Button onClick={openCreate}>New {itemNoun}</Button>}
      />

      <Card>
        {filters && (
          <div className="flex flex-wrap items-end gap-3 border-b border-sand-soft px-4 py-3">
            {filters}
          </div>
        )}

        <DataTable
          columns={[...columns, actionColumn]}
          rows={items}
          rowKey={rowKey}
          loading={loading}
          error={error}
          onRetry={reload}
          emptyTitle={emptyTitle}
          emptyDescription={emptyDescription}
          emptyAction={<Button onClick={openCreate}>New {itemNoun}</Button>}
        />

        <Pagination
          page={page}
          meta={meta}
          onPageChange={setPage}
          itemNoun={itemNounPlural}
        />
      </Card>

      <Modal
        open={open}
        onClose={closeForm}
        title={editing ? `Edit ${itemNoun}` : `New ${itemNoun}`}
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={saving}>
              Cancel
            </Button>
            <Button onClick={save} disabled={saving}>
              {saving ? "Saving…" : editing ? "Save changes" : `Create ${itemNoun}`}
            </Button>
          </>
        }
      >
        <div className="grid gap-4 sm:grid-cols-2">
          {fields.map((field) => (
            <FieldControl
              key={field.name}
              field={field}
              values={values}
              setValue={setValue}
            />
          ))}
        </div>

        {formError && (
          <p
            role="alert"
            className="mt-4 rounded-tailored bg-danger-soft px-3 py-2 text-sm text-danger"
          >
            {formError}
          </p>
        )}
      </Modal>

      <ConfirmDialog
        open={pendingDelete !== null}
        onCancel={() => setPendingDelete(null)}
        onConfirm={confirmDelete}
        busy={deleting}
        title={`Delete this ${itemNoun}?`}
        message={
          pendingDelete
            ? `"${describeRow(pendingDelete)}" will be removed from the site. This cannot be undone from here.`
            : ""
        }
        confirmLabel={`Delete ${itemNoun}`}
      />
    </>
  );
}

function FieldControl({
  field,
  values,
  setValue,
}: {
  field: FieldDef;
  values: FormValues;
  setValue: (name: string, value: unknown) => void;
}) {
  const raw = values[field.name];
  const id = `field-${field.name}`;
  const wide = field.span !== "half";

  const wrapper = (control: ReactNode) => (
    <Field
      key={field.name}
      label={field.label}
      htmlFor={id}
      hint={field.hint}
      required={field.required}
      className={wide ? "sm:col-span-2" : undefined}
    >
      {control}
    </Field>
  );

  switch (field.kind) {
    case "textarea":
      return wrapper(
        <Textarea
          id={id}
          rows={field.rows ?? 4}
          value={String(raw ?? "")}
          onChange={(event) => setValue(field.name, event.target.value)}
        />,
      );

    case "number":
    case "money":
      return wrapper(
        <Input
          id={id}
          type="number"
          inputMode="decimal"
          step={field.kind === "money" ? "0.01" : "1"}
          min="0"
          className="tabular"
          value={raw === null || raw === undefined ? "" : String(raw)}
          onChange={(event) => {
            const text = event.target.value;
            setValue(field.name, text === "" ? null : Number(text));
          }}
        />,
      );

    case "select":
      return wrapper(
        <Select
          id={id}
          value={String(raw ?? "")}
          onChange={(event) => setValue(field.name, event.target.value)}
        >
          <option value="">Choose…</option>
          {field.options?.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>,
      );

    case "checkbox":
      return (
        <div
          key={field.name}
          className={wide ? "sm:col-span-2" : undefined}
        >
          <Checkbox
            label={field.label}
            checked={Boolean(raw)}
            onChange={(event) => setValue(field.name, event.target.checked)}
          />
          {field.hint && <p className="mt-1 text-xs text-slate">{field.hint}</p>}
        </div>
      );

    case "tags":
    case "lines": {
      // Tags are comma-separated; bullet points are one per line. Both edit as
      // plain text because a chip editor is more to learn than it is worth here.
      const list = Array.isArray(raw) ? (raw as string[]) : [];
      const separator = field.kind === "tags" ? ", " : "\n";

      return wrapper(
        field.kind === "tags" ? (
          <Input
            id={id}
            value={list.join(separator)}
            onChange={(event) =>
              setValue(
                field.name,
                event.target.value
                  .split(",")
                  .map((entry) => entry.trim())
                  .filter(Boolean),
              )
            }
          />
        ) : (
          <Textarea
            id={id}
            rows={field.rows ?? 5}
            value={list.join(separator)}
            onChange={(event) =>
              setValue(
                field.name,
                event.target.value
                  .split("\n")
                  .map((entry) => entry.trim())
                  .filter(Boolean),
              )
            }
          />
        ),
      );
    }

    case "image":
      return wrapper(
        <ImageField
          value={String(raw ?? "")}
          onChange={(url) => setValue(field.name, url)}
          folder={field.folder ?? "gallery"}
          label={field.label.toLowerCase()}
        />,
      );

    case "datetime":
      return wrapper(
        <Input
          id={id}
          type="datetime-local"
          value={toLocalInput(raw)}
          onChange={(event) =>
            setValue(
              field.name,
              event.target.value ? new Date(event.target.value).toISOString() : null,
            )
          }
        />,
      );

    case "slug":
      return wrapper(
        <Input
          id={id}
          value={String(raw ?? "")}
          placeholder={
            field.derivedFrom ? slugify(String(values[field.derivedFrom] ?? "")) : undefined
          }
          onChange={(event) => setValue(field.name, slugify(event.target.value))}
          onBlur={(event) => {
            // Fill from the name when left blank, so nobody has to think about
            // slugs — but never overwrite one that was typed deliberately.
            if (!event.target.value && field.derivedFrom) {
              setValue(field.name, slugify(String(values[field.derivedFrom] ?? "")));
            }
          }}
        />,
      );

    default:
      return wrapper(
        <Input
          id={id}
          value={String(raw ?? "")}
          onChange={(event) => setValue(field.name, event.target.value)}
        />,
      );
  }
}

function toLocalInput(value: unknown): string {
  if (!value || typeof value !== "string") return "";

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";

  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

function capitalise(value: string): string {
  return value.charAt(0).toUpperCase() + value.slice(1);
}
