"use client";

import { CalendarCheck, ClipboardList, LogOut, Settings, ShieldCheck, UserRound } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

import { Avatar, AvatarFallback } from "@/components/ui/avatar";
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
import { loginUrl } from "@/lib/auth/routes";
import { initials } from "@/lib/utils";

export function UserMenu() {
  const { session, isAdmin, signOut } = useSession();
  const pathname = usePathname();

  if (!session) {
    const next = pathname === "/login" || pathname === "/register" ? undefined : pathname;
    return (
      <div className="flex items-center gap-1">
        <Button asChild variant="ghost" className="hidden rounded-full sm:inline-flex">
          <Link href={loginUrl(next)}>Sign in</Link>
        </Button>
        <Button asChild className="rounded-full">
          <Link href="/register">Sign up</Link>
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
          aria-label={`Account menu for ${session.name}`}
        >
          <span className="hidden max-w-28 truncate text-sm font-medium lg:inline">
            {session.name.split(" ")[0]}
          </span>
          <Avatar className="size-8">
            <AvatarFallback>{initials(session.name)}</AvatarFallback>
          </Avatar>
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
                  <ClipboardList /> Manage bookings
                </Link>
              </DropdownMenuItem>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
          </>
        )}
        <DropdownMenuGroup>
          <DropdownMenuItem asChild>
            <Link href="/bookings">
              <CalendarCheck /> My bookings
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/settings/personal-info">
              <UserRound /> Personal info
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/settings/login-and-security">
              <ShieldCheck /> Login &amp; security
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem asChild>
            <Link href="/settings">
              <Settings /> Account settings
            </Link>
          </DropdownMenuItem>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem variant="destructive" onSelect={() => signOut({ redirectTo: "/" })}>
          <LogOut /> Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
