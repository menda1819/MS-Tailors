"use client";

import { useCallback, useEffect, useId, useRef, type ReactNode } from "react";
import { Button, cx } from "@/components/ui/primitives";

/**
 * A modal built on the native <dialog>, which gives focus trapping, Escape to
 * close and inertness of the page behind it without reimplementing any of it.
 */
export function Modal({
  open,
  onClose,
  title,
  description,
  children,
  footer,
  size = "md",
}: {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children?: ReactNode;
  footer?: ReactNode;
  size?: "sm" | "md" | "lg";
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;

    if (open && !dialog.open) {
      dialog.showModal();
    } else if (!open && dialog.open) {
      dialog.close();
    }
  }, [open]);

  // The dialog can close itself — Escape, the backdrop — so state must follow.
  const handleClose = useCallback(() => onClose(), [onClose]);

  const widths = { sm: "max-w-sm", md: "max-w-lg", lg: "max-w-3xl" };

  return (
    <dialog
      ref={ref}
      onClose={handleClose}
      onClick={(event) => {
        // Clicking the backdrop closes; clicking the panel must not.
        if (event.target === ref.current) onClose();
      }}
      aria-labelledby={titleId}
      className={cx(
        "m-auto w-[calc(100vw-2rem)] rounded-tailored border border-sand-soft bg-chalk p-0",
        "shadow-overlay backdrop:bg-ink/40",
        widths[size],
      )}
    >
      <div className="flex flex-col">
        <div className="border-b border-sand-soft px-5 py-4">
          <h2 id={titleId} className="font-display text-lg leading-tight text-ink">
            {title}
          </h2>
          {description && <p className="mt-1 text-sm text-slate">{description}</p>}
        </div>

        {children && <div className="max-h-[70vh] overflow-y-auto px-5 py-4">{children}</div>}

        {footer && (
          <div className="flex flex-wrap justify-end gap-2 border-t border-sand-soft px-5 py-3.5">
            {footer}
          </div>
        )}
      </div>
    </dialog>
  );
}

/**
 * Confirmation for anything destructive.
 *
 * The confirm button names the actual action ("Delete brand"), never "OK" —
 * a person about to lose data should be able to read the button and know
 * exactly what happens.
 */
export function ConfirmDialog({
  open,
  onCancel,
  onConfirm,
  title,
  message,
  confirmLabel,
  busy = false,
  tone = "danger",
}: {
  open: boolean;
  onCancel: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  confirmLabel: string;
  busy?: boolean;
  tone?: "danger" | "primary";
}) {
  return (
    <Modal
      open={open}
      onClose={onCancel}
      title={title}
      size="sm"
      footer={
        <>
          <Button variant="secondary" onClick={onCancel} disabled={busy}>
            Cancel
          </Button>
          <Button variant={tone} onClick={onConfirm} disabled={busy}>
            {busy ? "Working…" : confirmLabel}
          </Button>
        </>
      }
    >
      <p className="text-sm leading-relaxed text-slate">{message}</p>
    </Modal>
  );
}
