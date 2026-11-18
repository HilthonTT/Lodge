import Link from "next/link";

import { Container } from "@/components/shared/container";
import { Logo } from "@/components/shared/logo";

const LINKS = [
  {
    title: "Explore",
    items: [
      { label: "All stays", href: "/" },
      { label: "Search", href: "/search" },
    ],
  },
  {
    title: "Your trips",
    items: [
      { label: "My bookings", href: "/bookings" },
      { label: "Account settings", href: "/settings" },
    ],
  },
];

export function Footer() {
  return (
    <footer className="mt-24 border-t bg-muted/40">
      <Container className="grid gap-10 py-12 sm:grid-cols-2 md:grid-cols-[2fr_1fr_1fr]">
        <div className="grid content-start gap-3">
          <Logo />
          <p className="max-w-xs text-sm text-muted-foreground">
            Unique homes, cabins and apartments — booked in a few clicks.
          </p>
        </div>
        {LINKS.map((group) => (
          <nav key={group.title} aria-label={group.title} className="grid content-start gap-3">
            <h2 className="text-sm font-semibold">{group.title}</h2>
            <ul className="grid gap-2">
              {group.items.map((item) => (
                <li key={item.href}>
                  <Link
                    href={item.href}
                    className="text-sm text-muted-foreground transition-colors hover:text-foreground"
                  >
                    {item.label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
        ))}
      </Container>
      <Container className="flex flex-col gap-2 border-t py-6 text-xs text-muted-foreground sm:flex-row sm:items-center sm:justify-between">
        <p>© {new Date().getFullYear()} Lodge. All rights reserved.</p>
        <p>Prices shown in the listing currency. Taxes may apply.</p>
      </Container>
    </footer>
  );
}
