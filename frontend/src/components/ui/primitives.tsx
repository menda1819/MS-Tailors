import type { ReactNode } from "react";

/** Joins class names, dropping falsy values. */
export function cx(...values: (string | false | null | undefined)[]): string {
  return values.filter(Boolean).join(" ");
}

// --- Button -----------------------------------------------------------------

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";
type ButtonSize = "sm" | "md";

const buttonBase =
  "inline-flex items-center justify-center gap-2 rounded-tailored font-medium " +
  "transition-colors disabled:opacity-50 disabled:pointer-events-none whitespace-nowrap";

const buttonVariants: Record<ButtonVariant, string> = {
  primary: "bg-indigo text-chalk hover:bg-indigo-hover",
  secondary: "border border-sand text-ink bg-chalk hover:bg-paper-sunk",
  ghost: "text-slate hover:text-ink hover:bg-paper-sunk",
  // Destructive actions read as destructive before they are clicked.
  danger: "bg-danger text-chalk hover:opacity-90",
};

const buttonSizes: Record<ButtonSize, string> = {
  sm: "text-[0.8125rem] px-3 py-1.5",
  md: "text-sm px-4 py-2.5",
};

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
}

export function Button({
  variant = "primary",
  size = "md",
  className,
  children,
  ...rest
}: ButtonProps) {
  return (
    <button
      className={cx(buttonBase, buttonVariants[variant], buttonSizes[size], className)}
      {...rest}
    >
      {children}
    </button>
  );
}

// --- Form fields -------------------------------------------------------------

const fieldBase =
  "w-full rounded-tailored border bg-chalk px-3 py-2 text-sm text-ink " +
  "placeholder:text-ash focus:border-indigo";

export interface FieldProps {
  label: string;
  htmlFor?: string;
  hint?: string;
  error?: string;
  required?: boolean;
  children: ReactNode;
  className?: string;
}

/**
 * Label, control, hint and error in one place, so no field can ship without a
 * label and no error can appear detached from the input it belongs to.
 */
export function Field({
  label,
  htmlFor,
  hint,
  error,
  required,
  children,
  className,
}: FieldProps) {
  return (
    <div className={cx("flex flex-col gap-1.5", className)}>
      <label htmlFor={htmlFor} className="text-[0.8125rem] font-medium text-ink">
        {label}
        {required && (
          <span className="text-danger" aria-hidden="true">
            {" "}
            *
          </span>
        )}
      </label>
      {children}
      {hint && !error && <p className="text-xs text-slate">{hint}</p>}
      {error && (
        <p className="text-xs text-danger" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}

export function Input({
  className,
  invalid,
  ...rest
}: React.InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean }) {
  return (
    <input
      className={cx(fieldBase, invalid ? "border-danger" : "border-sand-soft", className)}
      aria-invalid={invalid || undefined}
      {...rest}
    />
  );
}

export function Textarea({
  className,
  invalid,
  ...rest
}: React.TextareaHTMLAttributes<HTMLTextAreaElement> & { invalid?: boolean }) {
  return (
    <textarea
      className={cx(
        fieldBase,
        "min-h-24 leading-relaxed",
        invalid ? "border-danger" : "border-sand-soft",
        className,
      )}
      aria-invalid={invalid || undefined}
      {...rest}
    />
  );
}

export function Select({
  className,
  invalid,
  children,
  ...rest
}: React.SelectHTMLAttributes<HTMLSelectElement> & { invalid?: boolean }) {
  return (
    <select
      className={cx(fieldBase, invalid ? "border-danger" : "border-sand-soft", className)}
      aria-invalid={invalid || undefined}
      {...rest}
    >
      {children}
    </select>
  );
}

export function Checkbox({
  label,
  className,
  ...rest
}: React.InputHTMLAttributes<HTMLInputElement> & { label: string }) {
  return (
    <label className={cx("flex items-center gap-2.5 text-sm text-ink cursor-pointer", className)}>
      <input
        type="checkbox"
        className="size-4 rounded-tailored border-sand accent-indigo"
        {...rest}
      />
      {label}
    </label>
  );
}

// --- Badge -------------------------------------------------------------------

type BadgeTone = "neutral" | "accent" | "success" | "warning" | "danger";

const badgeTones: Record<BadgeTone, string> = {
  neutral: "bg-paper-sunk text-slate",
  accent: "bg-indigo-soft text-indigo",
  success: "bg-success-soft text-success",
  warning: "bg-warning-soft text-warning",
  danger: "bg-danger-soft text-danger",
};

export function Badge({
  tone = "neutral",
  children,
  className,
}: {
  tone?: BadgeTone;
  children: ReactNode;
  className?: string;
}) {
  return (
    <span
      className={cx(
        "inline-flex items-center rounded-tailored px-2 py-0.5 text-xs font-medium",
        badgeTones[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}

// --- Surfaces ----------------------------------------------------------------

export function Card({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cx(
        "rounded-tailored border border-sand-soft bg-chalk",
        className,
      )}
    >
      {children}
    </div>
  );
}

export function CardHeader({
  title,
  description,
  actions,
}: {
  title: string;
  description?: string;
  actions?: ReactNode;
}) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-3 border-b border-sand-soft px-5 py-4">
      <div>
        <h2 className="font-display text-lg leading-tight text-ink">{title}</h2>
        {description && <p className="mt-1 text-sm text-slate">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>
  );
}

// --- Skeleton ----------------------------------------------------------------

export function Skeleton({ className }: { className?: string }) {
  return <div className={cx("skeleton", className)} aria-hidden="true" />;
}

export function SkeletonText({ lines = 3 }: { lines?: number }) {
  return (
    <div className="flex flex-col gap-2" aria-hidden="true">
      {Array.from({ length: lines }).map((_, index) => (
        <Skeleton
          key={index}
          className={cx("h-3.5", index === lines - 1 ? "w-2/3" : "w-full")}
        />
      ))}
    </div>
  );
}

// --- Empty and error states --------------------------------------------------

/** An empty screen is an invitation to act, so it always offers the next step. */
export function EmptyState({
  title,
  description,
  action,
}: {
  title: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center gap-3 px-6 py-16 text-center">
      <div className="measure w-40" aria-hidden="true">
        {Array.from({ length: 20 }).map((_, index) => (
          <i key={index} />
        ))}
      </div>
      <h3 className="mt-2 font-display text-lg text-ink">{title}</h3>
      <p className="max-w-sm text-sm text-slate">{description}</p>
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}

/** Errors explain what happened and how to fix it. No apologies, no vagueness. */
export function ErrorState({
  message,
  onRetry,
}: {
  message: string;
  onRetry?: () => void;
}) {
  return (
    <div
      role="alert"
      className="flex flex-col items-start gap-3 rounded-tailored border border-danger-soft bg-danger-soft px-5 py-4"
    >
      <p className="text-sm font-medium text-danger">{message}</p>
      {onRetry && (
        <Button variant="secondary" size="sm" onClick={onRetry}>
          Try again
        </Button>
      )}
    </div>
  );
}

// --- Page furniture ----------------------------------------------------------

export function PageHeader({
  eyebrow,
  title,
  description,
  actions,
}: {
  /** A node, not just a string, so a page can put a breadcrumb link here. */
  eyebrow?: ReactNode;
  title: string;
  description?: string;
  actions?: ReactNode;
}) {
  return (
    <header className="flex flex-wrap items-end justify-between gap-4 pb-6">
      <div className="min-w-0">
        {eyebrow && (
          <p className="text-[0.6875rem] font-semibold uppercase tracking-[0.16em] text-slate">
            {eyebrow}
          </p>
        )}
        <h1 className="mt-1.5 font-display text-2xl leading-tight text-ink sm:text-3xl">
          {title}
        </h1>
        {description && (
          <p className="mt-2 max-w-2xl text-sm text-slate">{description}</p>
        )}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </header>
  );
}
