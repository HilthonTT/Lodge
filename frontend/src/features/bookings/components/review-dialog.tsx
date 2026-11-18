"use client";

import { Loader2, Star } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field } from "@/components/ui/field";
import { Textarea } from "@/components/ui/textarea";
import { FormError } from "@/features/auth/components/form-error";
import { cn } from "@/lib/utils";
import { uuid } from "@/lib/uuid";

import { useCreateReview } from "../hooks/use-bookings";

const MAX_COMMENT = 500;
const LABELS = ["Terrible", "Poor", "Okay", "Good", "Excellent"];

type ReviewDialogProps = {
  bookingId: string;
  apartmentName: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

export function ReviewDialog({ bookingId, apartmentName, open, onOpenChange }: ReviewDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        {open && (
          <ReviewForm bookingId={bookingId} apartmentName={apartmentName} onDone={() => onOpenChange(false)} />
        )}
      </DialogContent>
    </Dialog>
  );
}

function ReviewForm({
  bookingId,
  apartmentName,
  onDone,
}: {
  bookingId: string;
  apartmentName: string;
  onDone: () => void;
}) {
  const [rating, setRating] = useState(0);
  const [hover, setHover] = useState(0);
  const [comment, setComment] = useState("");
  const [touched, setTouched] = useState(false);
  // Stable per dialog session so a retried submit isn't recorded twice.
  const [idempotencyKey] = useState(uuid);
  const createReview = useCreateReview();

  const commentError = !comment.trim()
    ? "Tell other guests about your stay"
    : comment.length > MAX_COMMENT
      ? `Keep it under ${MAX_COMMENT} characters`
      : undefined;
  const ratingError = rating === 0 ? "Choose a rating" : undefined;

  const onSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setTouched(true);
    if (commentError || ratingError) return;
    try {
      await createReview.mutateAsync({ bookingId, rating, comment: comment.trim(), idempotencyKey });
      toast.success("Thanks for your review!");
      onDone();
    } catch {
      // shown inline
    }
  };

  const shown = hover || rating;

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-5">
      <DialogHeader>
        <DialogTitle>Review your stay</DialogTitle>
        <DialogDescription>How was {apartmentName}?</DialogDescription>
      </DialogHeader>

      <FormError message={createReview.error?.message} />

      <fieldset className="grid gap-2">
        <legend className="mb-2 text-sm font-medium">Rating</legend>
        <div className="flex items-center gap-3">
          <div className="flex" role="radiogroup" aria-label="Rating" onMouseLeave={() => setHover(0)}>
            {[1, 2, 3, 4, 5].map((value) => (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={rating === value}
                aria-label={`${value} star${value > 1 ? "s" : ""} – ${LABELS[value - 1]}`}
                onClick={() => setRating(value)}
                onMouseEnter={() => setHover(value)}
                className="rounded-md p-1 transition-transform hover:scale-110 focus-visible:ring-[3px] focus-visible:ring-ring/40 focus-visible:outline-none"
              >
                <Star
                  className={cn("size-7", value <= shown ? "fill-star text-star" : "text-muted-foreground/40")}
                  aria-hidden="true"
                />
              </button>
            ))}
          </div>
          <span className="text-sm text-muted-foreground">{shown ? LABELS[shown - 1] : ""}</span>
        </div>
        {touched && ratingError && <p className="text-xs font-medium text-destructive">{ratingError}</p>}
      </fieldset>

      <Field
        id="review-comment"
        label="Your review"
        error={touched ? commentError : undefined}
        aside={
          <span className={cn("text-xs text-muted-foreground tabular-nums", comment.length > MAX_COMMENT && "text-destructive")}>
            {comment.length}/{MAX_COMMENT}
          </span>
        }
      >
        {(control) => (
          <Textarea
            {...control}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            rows={5}
            placeholder="What did you love? Anything future guests should know?"
          />
        )}
      </Field>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone}>
          Cancel
        </Button>
        <Button type="submit" disabled={createReview.isPending}>
          {createReview.isPending && <Loader2 className="animate-spin" aria-hidden="true" />}
          Submit review
        </Button>
      </DialogFooter>
    </form>
  );
}
