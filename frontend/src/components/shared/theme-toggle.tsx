"use client";

import { Laptop, Moon, Sun } from "lucide-react";
import { useTranslations } from "next-intl";
import { useTheme } from "next-themes";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export function ThemeToggle() {
  const t = useTranslations("theme");
  const { theme, setTheme } = useTheme();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="rounded-full" aria-label={t("label")}>
          {/* Both icons render; CSS picks one so server and client markup match. */}
          <Sun className="size-[1.15rem] dark:hidden" aria-hidden="true" />
          <Moon className="hidden size-[1.15rem] dark:block" aria-hidden="true" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="min-w-36">
        <DropdownMenuRadioGroup value={theme ?? "system"} onValueChange={setTheme}>
          <DropdownMenuRadioItem value="light">
            <Sun /> {t("light")}
          </DropdownMenuRadioItem>
          <DropdownMenuRadioItem value="dark">
            <Moon /> {t("dark")}
          </DropdownMenuRadioItem>
          <DropdownMenuRadioItem value="system">
            <Laptop /> {t("system")}
          </DropdownMenuRadioItem>
        </DropdownMenuRadioGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
