"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { useAuth } from "@/lib/auth-context";
import { Button, Skeleton, cx } from "@/components/ui/primitives";

interface NavItem {
  href: string;
  label: string;
  /** Grouping label rendered above the first item in each group. */
  group: string;
}

/**
 * Grouped by the job being done, not by API shape: a shop owner thinks in
 * "today's work", "what I sell", "who I sell to".
 */
const NAV: NavItem[] = [
  { href: "/admin", label: "Dashboard", group: "Overview" },
  { href: "/admin/orders", label: "Orders", group: "Today" },
  { href: "/admin/inquiries", label: "Inquiries", group: "Today" },
  { href: "/admin/inventory", label: "Inventory", group: "Today" },
  { href: "/admin/products", label: "Products", group: "Catalogue" },
  { href: "/admin/brands", label: "Brands", group: "Catalogue" },
  { href: "/admin/categories", label: "Categories", group: "Catalogue" },
  { href: "/admin/coupons", label: "Coupons", group: "Catalogue" },
  { href: "/admin/customers", label: "Customers", group: "People" },
  { href: "/admin/services", label: "Services", group: "Content" },
  { href: "/admin/gallery", label: "Gallery", group: "Content" },
  { href: "/admin/blog", label: "Blog", group: "Content" },
  { href: "/admin/testimonials", label: "Testimonials", group: "Content" },
];

function isActive(pathname: string, href: string): boolean {
  // /admin must not light up for every child route.
  return href === "/admin" ? pathname === "/admin" : pathname.startsWith(href);
}

function SidebarNav({ onNavigate }: { onNavigate?: () => void }) {
  const pathname = usePathname();
  const groups = [...new Set(NAV.map((item) => item.group))];

  return (
    <nav aria-label="Admin sections" className="flex flex-col gap-6 px-3 py-5">
      {groups.map((group) => (
        <div key={group} className="flex flex-col gap-1">
          <p className="px-3 pb-1 text-[0.625rem] font-semibold uppercase tracking-[0.18em] text-ash">
            {group}
          </p>
          {NAV.filter((item) => item.group === group).map((item) => {
            const active = isActive(pathname, item.href);
            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={onNavigate}
                aria-current={active ? "page" : undefined}
                className={cx(
                  "rounded-tailored px-3 py-2 text-sm transition-colors",
                  active
                    ? "bg-indigo-soft font-medium text-indigo"
                    : "text-slate hover:bg-paper-sunk hover:text-ink",
                )}
              >
                {item.label}
              </Link>
            );
          })}
        </div>
      ))}
    </nav>
  );
}

/**
 * Guards every /admin route.
 *
 * Client-side redirection is a convenience, not the security boundary — the API
 * enforces the Admin role on every request regardless of what the browser does.
 */
export function AdminShell({ children }: { children: React.ReactNode }) {
  const { status, user, isAdmin, signOut } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [drawerOpen, setDrawerOpen] = useState(false);

  useEffect(() => {
    if (status === "anonymous") {
      router.replace(`/admin/login?next=${encodeURIComponent(pathname)}`);
    }
  }, [status, router, pathname]);

  if (status === "loading") {
    return (
      <div className="flex min-h-screen items-center justify-center bg-paper">
        <div className="flex w-64 flex-col gap-3" aria-busy="true" aria-label="Signing in">
          <Skeleton className="h-3 w-24" />
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-3 w-40" />
        </div>
      </div>
    );
  }

  if (status === "anonymous") return null;

  if (!isAdmin) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-paper px-6">
        <div className="max-w-md text-center">
          <h1 className="font-display text-2xl text-ink">This area is for staff</h1>
          <p className="mt-3 text-sm text-slate">
            You are signed in as {user?.email}, which is a customer account. Ask an
            administrator to grant you access, or sign in with a staff account.
          </p>
          <div className="mt-6 flex justify-center gap-2">
            <Button variant="secondary" onClick={() => router.push("/")}>
              Back to the shop
            </Button>
            <Button onClick={signOut}>Sign out</Button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen bg-paper">
      <aside className="hidden w-60 shrink-0 border-r border-sand-soft bg-chalk lg:block">
        <div className="border-b border-sand-soft px-6 py-5">
          <Link href="/admin" className="block">
            <span className="font-display text-lg leading-none text-ink">MS Tailors</span>
            <span className="mt-1 block text-[0.625rem] font-semibold uppercase tracking-[0.18em] text-slate">
              Shop admin
            </span>
          </Link>
        </div>
        <SidebarNav />
      </aside>

      {drawerOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <button
            type="button"
            aria-label="Close menu"
            className="absolute inset-0 bg-ink/40"
            onClick={() => setDrawerOpen(false)}
          />
          <div className="absolute inset-y-0 left-0 w-64 overflow-y-auto border-r border-sand-soft bg-chalk">
            <div className="border-b border-sand-soft px-6 py-5">
              <span className="font-display text-lg text-ink">MS Tailors</span>
            </div>
            {/* Closing here rather than in an effect on pathname: the click is
                the actual event, and an effect would fire on every navigation
                including ones that did not come from this menu. */}
            <SidebarNav onNavigate={() => setDrawerOpen(false)} />
          </div>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex items-center justify-between gap-3 border-b border-sand-soft bg-paper/95 px-4 py-3 backdrop-blur sm:px-6">
          <Button
            variant="ghost"
            size="sm"
            className="lg:hidden"
            onClick={() => setDrawerOpen(true)}
            aria-expanded={drawerOpen}
          >
            Menu
          </Button>

          <div className="ml-auto flex items-center gap-3">
            <span className="hidden text-[0.8125rem] text-slate sm:inline">{user?.email}</span>
            <Button variant="secondary" size="sm" onClick={signOut}>
              Sign out
            </Button>
          </div>
        </header>

        <main className="min-w-0 flex-1 px-4 py-6 sm:px-6 lg:px-8">{children}</main>
      </div>
    </div>
  );
}
