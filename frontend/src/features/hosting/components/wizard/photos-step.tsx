"use client";

import {
  closestCenter,
  DndContext,
  type DragEndEvent,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import {
  arrayMove,
  rectSortingStrategy,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
} from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { AlertCircle, GripVertical, ImagePlus, Loader2, Star, Trash2, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";

import { RemoteImage } from "@/components/shared/remote-image";
import { Button } from "@/components/ui/button";
import { getBrowserHttp } from "@/lib/api/browser";
import { useApiErrorMessage } from "@/i18n/api-errors";
import { cn } from "@/lib/utils";

import { hostingApi } from "../../api/hosting";
import { LIMITS } from "../../schemas/listing";

export type Photo = { id: string; url: string };

type Upload = { key: string; name: string; preview: string; progress: number; error?: string };

const TYPES = ["image/jpeg", "image/png", "image/webp"];
const MAX_BYTES = 10 * 1024 * 1024;

type PhotosStepProps = {
  photos: Photo[];
  onChange: (photos: Photo[]) => void;
  error?: string;
};

export function PhotosStep({ photos, onChange, error }: PhotosStepProps) {
  const t = useTranslations("hosting.wizard.photos");
  const errorMessage = useApiErrorMessage();
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploads, setUploads] = useState<Upload[]>([]);
  const [dragOver, setDragOver] = useState(false);
  // Uploads finish in any order; keep a live copy so each one appends to the latest list.
  const photosRef = useRef(photos);
  useEffect(() => {
    photosRef.current = photos;
  }, [photos]);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  const patchUpload = (key: string, patch: Partial<Upload>) =>
    setUploads((list) => list.map((u) => (u.key === key ? { ...u, ...patch } : u)));

  const addFiles = (files: FileList | File[]) => {
    const room = LIMITS.photos - photosRef.current.length - uploads.filter((u) => !u.error).length;
    [...files].slice(0, Math.max(0, room)).forEach((file) => {
      const key = `${file.name}-${file.size}-${Math.random().toString(36).slice(2)}`;
      const preview = URL.createObjectURL(file);
      const invalid = !TYPES.includes(file.type) ? t("invalidType") : file.size > MAX_BYTES ? t("tooLarge") : undefined;
      setUploads((list) => [...list, { key, name: file.name, preview, progress: 0, error: invalid }]);
      if (invalid) return;
      hostingApi
        .uploadPhoto(getBrowserHttp(), file, (fraction) => patchUpload(key, { progress: fraction }))
        .then((uploaded) => {
          photosRef.current = [...photosRef.current, { id: uploaded.fileId, url: uploaded.url }];
          onChange(photosRef.current);
          setUploads((list) => list.filter((u) => u.key !== key));
          URL.revokeObjectURL(preview);
        })
        .catch((err: Error) => patchUpload(key, { error: errorMessage(err) }));
    });
  };

  const dismiss = (upload: Upload) => {
    URL.revokeObjectURL(upload.preview);
    setUploads((list) => list.filter((u) => u.key !== upload.key));
  };

  const onDragEnd = ({ active, over }: DragEndEvent) => {
    if (!over || active.id === over.id) return;
    const from = photos.findIndex((p) => p.id === active.id);
    const to = photos.findIndex((p) => p.id === over.id);
    if (from >= 0 && to >= 0) onChange(arrayMove(photos, from, to));
  };

  const move = (index: number, delta: number) => {
    const target = index + delta;
    if (target < 0 || target >= photos.length) return;
    onChange(arrayMove(photos, index, target));
  };

  const full = photos.length >= LIMITS.photos;

  return (
    <div className="grid gap-5">
      <div
        onDragOver={(e) => {
          if (!e.dataTransfer.types.includes("Files")) return;
          e.preventDefault();
          setDragOver(true);
        }}
        onDragLeave={() => setDragOver(false)}
        onDrop={(e) => {
          if (!e.dataTransfer.files.length) return;
          e.preventDefault();
          setDragOver(false);
          addFiles(e.dataTransfer.files);
        }}
        className={cn(
          "grid justify-items-center gap-3 rounded-2xl border-2 border-dashed p-8 text-center transition-colors",
          dragOver ? "border-primary bg-accent/60" : "border-border",
          error && "border-destructive/60",
        )}
      >
        <ImagePlus className="size-8 text-muted-foreground" aria-hidden="true" />
        <div className="grid gap-1">
          <p className="font-medium">{t("dropTitle")}</p>
          <p className="text-sm text-muted-foreground">{t("dropHint", { max: LIMITS.photos })}</p>
        </div>
        <Button type="button" variant="outline" onClick={() => inputRef.current?.click()} disabled={full}>
          {t("choose")}
        </Button>
        <input
          ref={inputRef}
          type="file"
          accept={TYPES.join(",")}
          multiple
          className="sr-only"
          tabIndex={-1}
          aria-label={t("choose")}
          data-testid="photo-input"
          onChange={(e) => {
            if (e.target.files) addFiles(e.target.files);
            e.target.value = "";
          }}
        />
      </div>

      {error && (
        <p role="alert" className="text-sm font-medium text-destructive">
          {error}
        </p>
      )}

      {photos.length > 0 && <p className="text-sm text-muted-foreground">{t("reorderHint")}</p>}

      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={onDragEnd}>
        <SortableContext items={photos.map((p) => p.id)} strategy={rectSortingStrategy}>
          <ul className="grid grid-cols-2 gap-4 sm:grid-cols-3">
            {photos.map((photo, index) => (
              <SortablePhoto
                key={photo.id}
                photo={photo}
                index={index}
                count={photos.length}
                onRemove={() => onChange(photos.filter((p) => p.id !== photo.id))}
                onMove={(delta) => move(index, delta)}
              />
            ))}
            {uploads.map((upload) => (
              <li
                key={upload.key}
                className="relative aspect-[4/3] overflow-hidden rounded-2xl border bg-muted"
                aria-live="polite"
              >
                {/* eslint-disable-next-line @next/next/no-img-element -- local blob preview */}
                <img src={upload.preview} alt="" className="size-full object-cover opacity-60" />
                <div className="absolute inset-x-3 bottom-3 grid gap-1.5">
                  {upload.error ? (
                    <div className="flex items-start gap-2 rounded-lg bg-background/95 p-2 text-xs text-destructive">
                      <AlertCircle className="size-4 shrink-0" aria-hidden="true" />
                      <span className="min-w-0 flex-1">{upload.error}</span>
                      <button type="button" onClick={() => dismiss(upload)} aria-label={t("dismiss")}>
                        <X className="size-4" aria-hidden="true" />
                      </button>
                    </div>
                  ) : (
                    <>
                      <span className="flex items-center gap-1.5 text-xs font-medium text-foreground">
                        <Loader2 className="size-3.5 animate-spin" aria-hidden="true" />
                        {t("uploading", { percent: String(Math.round(upload.progress * 100)) })}
                      </span>
                      <div
                        role="progressbar"
                        aria-label={t("uploadProgress", { name: upload.name })}
                        aria-valuemin={0}
                        aria-valuemax={100}
                        aria-valuenow={Math.round(upload.progress * 100)}
                        className="h-1.5 overflow-hidden rounded-full bg-background/80"
                      >
                        <div className="h-full bg-primary transition-[width]" style={{ width: `${upload.progress * 100}%` }} />
                      </div>
                    </>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </SortableContext>
      </DndContext>
    </div>
  );
}

function SortablePhoto({
  photo,
  index,
  count,
  onRemove,
  onMove,
}: {
  photo: Photo;
  index: number;
  count: number;
  onRemove: () => void;
  onMove: (delta: number) => void;
}) {
  const t = useTranslations("hosting.wizard.photos");
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: photo.id });

  return (
    <li
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className={cn(
        "group relative aspect-[4/3] overflow-hidden rounded-2xl border bg-muted shadow-card",
        isDragging && "z-10 opacity-80 shadow-float",
        index === 0 && "sm:col-span-2 sm:row-span-2 sm:aspect-auto",
      )}
      data-photo-id={photo.id}
    >
      <RemoteImage src={photo.url} alt={t("photoAlt", { number: String(index + 1) })} fill sizes="(min-width: 640px) 33vw, 50vw" className="object-cover" />
      {index === 0 && (
        <span className="absolute top-3 left-3 inline-flex items-center gap-1 rounded-full bg-background/95 px-2.5 py-1 text-xs font-semibold shadow-card">
          <Star className="size-3.5 fill-star text-star" aria-hidden="true" /> {t("cover")}
        </span>
      )}
      <div className="absolute top-2 right-2 flex gap-1">
        <button
          type="button"
          {...attributes}
          {...listeners}
          aria-label={t("dragHandle", { number: String(index + 1) })}
          className="grid size-8 cursor-grab touch-none place-items-center rounded-full bg-background/95 shadow-card outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50 active:cursor-grabbing"
        >
          <GripVertical className="size-4" aria-hidden="true" />
        </button>
        <button
          type="button"
          onClick={onRemove}
          aria-label={t("remove", { number: String(index + 1) })}
          className="grid size-8 place-items-center rounded-full bg-background/95 text-destructive shadow-card outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50"
        >
          <Trash2 className="size-4" aria-hidden="true" />
        </button>
      </div>
      {index > 0 && (
        <button
          type="button"
          onClick={() => onMove(-index)}
          className="absolute bottom-2 left-2 rounded-full bg-background/95 px-2.5 py-1 text-xs font-medium opacity-0 shadow-card transition-opacity group-hover:opacity-100 focus-visible:opacity-100"
        >
          {t("makeCover")}
        </button>
      )}
      <span className="sr-only">{t("position", { number: String(index + 1), count: String(count) })}</span>
    </li>
  );
}
