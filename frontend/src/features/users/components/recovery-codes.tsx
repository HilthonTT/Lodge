"use client";

import { Download, KeyRound } from "lucide-react";
import { useTranslations } from "next-intl";
import { useId, useState } from "react";

import { CopyButton } from "@/components/shared/copy-button";
import { Button } from "@/components/ui/button";

/** Shows freshly issued recovery codes once, with copy, download and an acknowledgement. */
export function RecoveryCodes({ codes, email, onDone }: { codes: string[]; email: string; onDone: () => void }) {
  const t = useTranslations("settings.security.twoFactor");
  const [saved, setSaved] = useState(false);
  const checkboxId = useId();
  const text = codes.join("\n");

  const download = () => {
    const content = [t("fileHeader", { email }), "", t("fileIntro"), "", ...codes, ""].join("\r\n");
    const url = URL.createObjectURL(new Blob([content], { type: "text/plain;charset=utf-8" }));
    const link = document.createElement("a");
    link.href = url;
    link.download = "lodge-recovery-codes.txt";
    document.body.append(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="grid gap-5 rounded-2xl border bg-card p-5 sm:p-6">
      <div className="flex items-start gap-3">
        <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-accent text-accent-foreground">
          <KeyRound className="size-5" aria-hidden="true" />
        </span>
        <div className="grid gap-1">
          <h3 className="font-semibold">{t("recoveryTitle")}</h3>
          <p className="text-sm text-muted-foreground">{t("recoveryDescription")}</p>
        </div>
      </div>

      <ul
        aria-label={t("recoveryTitle")}
        data-testid="recovery-codes"
        className="grid grid-cols-2 gap-x-6 gap-y-2 rounded-xl bg-muted/60 p-4 font-mono text-sm sm:grid-cols-2"
      >
        {codes.map((code) => (
          <li key={code} className="select-all">
            {code}
          </li>
        ))}
      </ul>

      <div className="flex flex-wrap gap-2">
        <CopyButton value={text} label={t("copyCodes")} />
        <Button type="button" size="sm" variant="outline" onClick={download}>
          <Download aria-hidden="true" /> {t("download")}
        </Button>
      </div>

      <div className="flex flex-wrap items-center justify-between gap-4 border-t pt-4">
        <label htmlFor={checkboxId} className="flex cursor-pointer items-center gap-2 text-sm font-medium">
          <input
            id={checkboxId}
            type="checkbox"
            className="size-4 accent-primary"
            checked={saved}
            onChange={(event) => setSaved(event.target.checked)}
          />
          {t("savedAck")}
        </label>
        <Button type="button" onClick={onDone} disabled={!saved}>
          {t("done")}
        </Button>
      </div>
    </div>
  );
}
