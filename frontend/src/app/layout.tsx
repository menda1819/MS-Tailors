import type { Metadata } from "next";
import { Fraunces, Instrument_Sans } from "next/font/google";
import "./globals.css";

/**
 * Fraunces carries every editorial line. Its optical-size axis means a heading
 * at 84px is drawn differently from one at 24px rather than merely scaled —
 * the reason it was chosen over a fixed high-contrast serif.
 */
const fraunces = Fraunces({
  variable: "--font-fraunces",
  subsets: ["latin"],
  display: "swap",
  axes: ["SOFT", "WONK", "opsz"],
});

/** Body, labels, buttons, prices. Tabular figures matter here. */
const instrumentSans = Instrument_Sans({
  variable: "--font-instrument-sans",
  subsets: ["latin"],
  display: "swap",
});

export const metadata: Metadata = {
  title: {
    default: "MS Tailors",
    template: "%s · MS Tailors",
  },
  description:
    "Perfect Fit. Timeless Style. Bespoke tailoring and ready-made clothing in Panadura, Sri Lanka.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html
      lang="en"
      className={`${fraunces.variable} ${instrumentSans.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">{children}</body>
    </html>
  );
}
