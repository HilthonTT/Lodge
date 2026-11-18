import { z } from "zod";

import type { ValidationTranslator } from "@/features/auth/schemas";

export const REVIEW_COMMENT_MAX = 500;
export const REPLY_MAX = 1000;

const createStarSchema = (t: ValidationTranslator) =>
  z.number().int().min(1, t("ratingRequired")).max(5, t("ratingRequired"));

const createCommentSchema = (
  t: ValidationTranslator,
  max: number,
  requiredKey: "commentRequired" | "guestCommentRequired",
) => z.string().trim().min(1, t(requiredKey)).max(max, t("commentTooLong", { max }));

/** Guest → listing review: overall stars, six category ratings and a comment. */
export const createReviewSchema = (t: ValidationTranslator) => {
  const star = createStarSchema(t);
  return z.object({
    rating: star,
    categories: z.object({
      cleanliness: star,
      accuracy: star,
      checkIn: star,
      communication: star,
      location: star,
      value: star,
    }),
    comment: createCommentSchema(t, REVIEW_COMMENT_MAX, "commentRequired"),
  });
};
export type ReviewFormValues = z.infer<ReturnType<typeof createReviewSchema>>;

/** Host → guest review. */
export const createGuestReviewSchema = (t: ValidationTranslator) =>
  z.object({
    rating: createStarSchema(t),
    comment: createCommentSchema(t, REVIEW_COMMENT_MAX, "guestCommentRequired"),
  });
export type GuestReviewFormValues = z.infer<ReturnType<typeof createGuestReviewSchema>>;

/** Host's public reply to a review. */
export const createReplySchema = (t: ValidationTranslator) =>
  z.object({
    text: z.string().trim().min(1, t("replyRequired")).max(REPLY_MAX, t("commentTooLong", { max: REPLY_MAX })),
  });
export type ReplyFormValues = z.infer<ReturnType<typeof createReplySchema>>;
