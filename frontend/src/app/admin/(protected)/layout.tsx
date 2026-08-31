import { AdminShell } from "@/components/admin/shell";

/** Everything under this group requires an authenticated Admin. */
export default function ProtectedAdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <AdminShell>{children}</AdminShell>;
}
