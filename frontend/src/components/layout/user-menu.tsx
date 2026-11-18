"use client";

import {
  CalendarCheck,
  CircleUserRound,
  Bell,
  ClipboardList,
  Heart,
  House,
  LogOut,
  MessageCircle,
  Settings,
  ShieldCheck,
  UserRound,
} from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";

import { UserAvatar } from "@/components/shared/user-avatar";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useSession } from "@/features/auth/session-provider";
import { fileUrl } from "@/lib/auth/avatar";
import { loginUrl } from "@/lib/auth/routes";

export function UserMenu() {
  const t = useTranslations("nav");
  const { session, isAdmin, signOut } = useSession();
  const pathname = usePathname();

  if (!session) {
    const next = pathname === "/login" || pathname === "/register" ? undefined : pathname;
    return (
      <div className="flex items-center gap-1">
        <Button asChild variant="ghost" className="hidden rounded-full sm:inline-flex">
          <Link href={loginUrl(next)}>{t("signIn")}</Link>
        </Button>
        <Button asChild className="rounded-full">
          <Link href="/register">{t("signUp")}</Link>
        </Button>
      </div>
    );
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          className="h-11 gap-2 rounded-full pr-1.5 pl-3.5 shadow-xs"
          aria-label={t("accountMenu", { name: session.name })}
        >
          <span className="hidden max-w-28 truncate text-sm font-medium lg:inline">
            {session.name.split(" ")[0]}
          </span>
          <UserAvatar name={session.name} src={fileUrl(session.imageId)} className="size-8" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuLabel className="grid gap-0.5">
          <span className="truncate font-semibold">{session.name}</span>
          <span className="truncate text-xs font-normal text-muted-foreground">{session.email}</span>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        {isAdmin && (
          <>
            <DropdownMenuGroup>
              <DropdownMenuItem asChild>
                <Link href="/admin/bookings">
                  <ClipboardList /> {t("manageBookings")}
                </Link>
              </DropdownMenuItem>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
          </>
        )}
        <DropdownMenuGroup>
          <DropdownMenuItem asChild>
            <Link href={`/users/${session.userId}`}>
              <CircleUserRound /> {t("profile")}
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/bookings">
              <CalendarCheck /> {t("myBookings")}
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/messages">
              <MessageCircle /> {t("messages")}
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/notifications">
              <Bell /> {t("notifications")}
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/wishlists">
              <Heart /> {t("wishlists")}
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/hosting">
              <House /> {t("hosting")}
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/settings/personal-info">
              <UserRound /> {t("personalInfo")}
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/settings/login-and-security">
              <ShieldCheck /> {t("loginSecurity")}
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/settings">
              <Settings /> {t("accountSettings")}
            </Link>
          </DropdownMenuItem>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" onSelect={() => signOut({ redirectTo: "/" })}>
          <LogOut /> {t("signOut")}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
