"use client";

import { Loader2, MessageCircle, SendHorizontal } from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { UserAvatar } from "@/components/shared/user-avatar";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Textarea } from "@/components/ui/textarea";
import { useSession } from "@/features/auth/session-provider";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { loginUrl } from "@/lib/auth/routes";
import { formatNumber } from "@/lib/format";
import { cn } from "@/lib/utils";

import { MESSAGE_MAX_LENGTH } from "../api/messaging";
import { useStartConversation } from "../hooks/use-messaging";

type Host = { firstName: string; avatarUrl: string | null };

/** Dialog that sends the first message about a listing and opens the thread. */
export function ContactHostDialog({
  apartmentId,
  listingName,
  host,
  open,
  onOpenChange,
}: {
  apartmentId: string;
  listingName: string;
  host: Host;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const t = useTranslations("messaging.contact");
  const locale = useLocale();
  const router = useRouter();
  const errorMessage = useApiErrorMessage();
  const start = useStartConversation();
  const [message, setMessage] = useState("");
  const trimmed = message.trim();
  const tooLong = message.length > MESSAGE_MAX_LENGTH;

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    if (!trimmed || tooLong) return;
    start.mutate(
      { apartmentId, message: trimmed },
      {
        onSuccess: ({ conversationId }) => {
          toast.success(t("sent", { name: host.firstName }));
          // Close first so the dialog's focus trap and aria-hidden are gone before the route changes.
          onOpenChange(false);
          setMessage("");
          router.push(`/messages/${conversationId}`);
        },
        onError: (error) => toast.error(t("error"), { description: errorMessage(error) }),
      },
    );
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <form onSubmit={submit} className="grid gap-5">
          <DialogHeader>
            <div className="flex items-center gap-3">
              <UserAvatar name={host.firstName} src={host.avatarUrl} className="size-12" />
              <div className="grid gap-0.5 text-left">
                <DialogTitle>{t("title", { name: host.firstName })}</DialogTitle>
                <DialogDescription className="line-clamp-1">{listingName}</DialogDescription>
              </div>
            </div>
          </DialogHeader>
          <div className="grid gap-2">
            <label htmlFor="contact-message" className="text-sm font-medium">
              {t("label")}
            </label>
            <Textarea
              id="contact-message"
              rows={5}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              placeholder={t("placeholder")}
              aria-invalid={tooLong || undefined}
              autoFocus
              data-testid="contact-message"
            />
            <div className="flex justify-between gap-3 text-xs text-muted-foreground">
              <span>{t("hint")}</span>
              <span className={cn("shrink-0 tabular-nums", tooLong && "font-medium text-destructive")}>
                {formatNumber(message.length, locale)}/{formatNumber(MESSAGE_MAX_LENGTH, locale)}
              </span>
            </div>
          </div>
          <DialogFooter>
            <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
              {t("cancel")}
            </Button>
            <Button type="submit" disabled={!trimmed || tooLong || start.isPending} data-testid="contact-send">
              {start.isPending ? <Loader2 className="animate-spin" aria-hidden="true" /> : <SendHorizontal aria-hidden="true" />}
              {t("send")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

/** "Contact host" on the listing page (sign-in link for visitors, hidden for the host). */
export function ContactHostButton({
  apartmentId,
  listingName,
  host,
  className,
}: {
  apartmentId: string;
  listingName: string;
  host: Host & { id: string };
  className?: string;
}) {
  const t = useTranslations("messaging.contact");
  const { session } = useSession();
  const pathname = usePathname();
  const [open, setOpen] = useState(false);

  if (session?.userId === host.id) return null;
  if (!session) {
    return (
      <Button asChild variant="outline" className={className}>
        <Link href={loginUrl(pathname)}>
          <MessageCircle aria-hidden="true" /> {t("button")}
        </Link>
      </Button>
    );
  }
  return (
    <>
      <Button variant="outline" className={className} onClick={() => setOpen(true)} data-testid="contact-host">
        <MessageCircle aria-hidden="true" /> {t("button")}
      </Button>
      <ContactHostDialog apartmentId={apartmentId} listingName={listingName} host={host} open={open} onOpenChange={setOpen} />
    </>
  );
}
