"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { cx } from "@/components/ui/primitives";

interface Toast {
  id: number;
  message: string;
  tone: "success" | "error";
}

interface ToastContextValue {
  /** Confirms an action in the same words the button used: "Publish" → "Published". */
  notify: (message: string) => void;
  fail: (message: string) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const push = useCallback((message: string, tone: Toast["tone"]) => {
    const id = Date.now() + Math.random();
    setToasts((current) => [...current, { id, message, tone }]);

    // Errors stay longer: they usually need reading, not just acknowledging.
    setTimeout(() => {
      setToasts((current) => current.filter((toast) => toast.id !== id));
    }, tone === "error" ? 7000 : 4000);
  }, []);

  const value = useMemo<ToastContextValue>(
    () => ({
      notify: (message) => push(message, "success"),
      fail: (message) => push(message, "error"),
    }),
    [push],
  );

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div
        className="pointer-events-none fixed bottom-4 right-4 z-50 flex w-[min(24rem,calc(100vw-2rem))] flex-col gap-2"
        role="status"
        aria-live="polite"
      >
        {toasts.map((toast) => (
          <div
            key={toast.id}
            className={cx(
              "animate-rise pointer-events-auto rounded-tailored border px-4 py-3 text-sm shadow-raised",
              toast.tone === "success"
                ? "border-success-soft bg-success-soft text-success"
                : "border-danger-soft bg-danger-soft text-danger",
            )}
          >
            {toast.message}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const context = useContext(ToastContext);

  if (!context) {
    throw new Error("useToast must be used inside a ToastProvider.");
  }

  return context;
}
