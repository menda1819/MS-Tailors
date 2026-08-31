import type { Metadata } from "next";
import { AuthProvider } from "@/lib/auth-context";
import { ToastProvider } from "@/components/ui/toast";

export const metadata: Metadata = {
  title: {
    default: "Shop admin",
    template: "%s · MS Tailors admin",
  },
  // The admin must never appear in search results.
  robots: { index: false, follow: false },
};

/**
 * Providers only. The auth guard lives in (protected)/layout.tsx so that
 * /admin/login can use the same auth context without the guard redirecting
 * the login page to itself.
 */
export default function AdminLayout({ children }: LayoutProps<"/admin">) {
  return (
    <AuthProvider>
      <ToastProvider>{children}</ToastProvider>
    </AuthProvider>
  );
}
