import { Skeleton } from "@/components/ui/primitives";

export default function AdminLoading() {
  return (
    <div className="flex flex-col gap-6" aria-busy="true" aria-label="Loading">
      <div className="flex flex-col gap-2">
        <Skeleton className="h-3 w-24" />
        <Skeleton className="h-8 w-64" />
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 4 }).map((_, index) => (
          <Skeleton key={index} className="h-28 w-full" />
        ))}
      </div>
      <Skeleton className="h-72 w-full" />
    </div>
  );
}
