"use client";

import { useEffect } from "react";
import { Button, Card, PageHeader } from "@/components/ui/primitives";

/**
 * Error boundary for every admin screen. A thrown render error shows this
 * instead of a blank page, and offers the way back.
 */
export default function AdminError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("[admin]", error);
  }, [error]);

  return (
    <div>
      <PageHeader
        eyebrow="Something broke"
        title="This screen could not be displayed"
        description="The rest of the admin is still working. Try loading this screen again, and if it keeps failing, note what you were doing before it happened."
      />
      <Card className="p-5">
        <p className="text-sm text-slate">
          {error.message || "No further detail was reported."}
        </p>
        {error.digest && (
          <p className="mt-2 text-xs text-ash tabular">Reference: {error.digest}</p>
        )}
        <div className="mt-4">
          <Button onClick={reset}>Try again</Button>
        </div>
      </Card>
    </div>
  );
}
