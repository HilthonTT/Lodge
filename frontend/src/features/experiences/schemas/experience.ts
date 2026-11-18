import type { useTranslations } from "next-intl";
import { z } from "zod";

import { EXPERIENCE_CATEGORIES, type Experience, type ExperienceCategory, type ExperienceRequest } from "@/types/api";

/** Limits enforced by the API (ExperienceRequest validation). */
export const EXPERIENCE_LIMITS = {
  title: 120,
  description: 4000,
  meetingPoint: 500,
  location: 100,
  duration: [30, 1440] as const,
  guests: [1, 50] as const,
  languages: 10,
  included: 20,
  includedItem: 100,
  photos: 20,
};

export const EXPERIENCE_STEPS = ["basics", "location", "details", "photos", "price", "review"] as const;
export type ExperienceStep = (typeof EXPERIENCE_STEPS)[number];

export type ExperienceErrorsTranslator = ReturnType<typeof useTranslations<"experiences.hosting.wizard.errors">>;

export const createExperienceSchema = (t: ExperienceErrorsTranslator) =>
  z.object({
    title: z.string().trim().min(1, t("titleRequired")).max(EXPERIENCE_LIMITS.title, t("tooLong", { max: EXPERIENCE_LIMITS.title })),
    description: z
      .string()
      .trim()
      .min(1, t("descriptionRequired"))
      .max(EXPERIENCE_LIMITS.description, t("tooLong", { max: EXPERIENCE_LIMITS.description })),
    category: z.enum(EXPERIENCE_CATEGORIES, { error: t("categoryRequired") }),
    city: z.string().trim().min(1, t("cityRequired")).max(EXPERIENCE_LIMITS.location),
    country: z.string().trim().min(1, t("countryRequired")).max(EXPERIENCE_LIMITS.location),
    latitude: z.number({ error: t("pinRequired") }).min(-90).max(90),
    longitude: z.number({ error: t("pinRequired") }).min(-180).max(180),
    meetingPoint: z
      .string()
      .trim()
      .min(1, t("meetingPointRequired"))
      .max(EXPERIENCE_LIMITS.meetingPoint, t("tooLong", { max: EXPERIENCE_LIMITS.meetingPoint })),
    durationMinutes: z
      .number()
      .int()
      .min(EXPERIENCE_LIMITS.duration[0], t("durationRange"))
      .max(EXPERIENCE_LIMITS.duration[1], t("durationRange")),
    maxGuests: z.number().int().min(EXPERIENCE_LIMITS.guests[0]).max(EXPERIENCE_LIMITS.guests[1]),
    languages: z
      .array(z.string())
      .min(1, t("languagesRequired"))
      .max(EXPERIENCE_LIMITS.languages, t("languagesTooMany", { max: EXPERIENCE_LIMITS.languages })),
    included: z
      .array(z.string().trim().min(1).max(EXPERIENCE_LIMITS.includedItem))
      .max(EXPERIENCE_LIMITS.included, t("includedTooMany", { max: EXPERIENCE_LIMITS.included })),
    photos: z.array(z.object({ id: z.string(), url: z.string() })).max(EXPERIENCE_LIMITS.photos),
    pricePerGuest: z.number({ error: t("priceRequired") }).positive(t("priceRequired")).max(100_000),
    currency: z.enum(["USD", "EUR"]),
    isListed: z.boolean(),
  });

export type ExperienceFormValues = z.infer<ReturnType<typeof createExperienceSchema>>;

export const EXPERIENCE_STEP_FIELDS: Record<ExperienceStep, (keyof ExperienceFormValues)[]> = {
  basics: ["title", "description", "category"],
  location: ["city", "country", "latitude", "longitude", "meetingPoint"],
  details: ["durationMinutes", "maxGuests", "languages", "included"],
  photos: ["photos"],
  price: ["pricePerGuest", "currency"],
  review: ["isListed"],
};

/** Server validation codes → the field and step to show them on. */
export const EXPERIENCE_SERVER_CODES: Record<string, { field: keyof ExperienceFormValues; step: ExperienceStep }> = {
  "Experience.InvalidTitle": { field: "title", step: "basics" },
  "Experience.InvalidDescription": { field: "description", step: "basics" },
  "Experience.InvalidCategory": { field: "category", step: "basics" },
  "Experience.InvalidLocation": { field: "latitude", step: "location" },
  "Experience.InvalidMeetingPoint": { field: "meetingPoint", step: "location" },
  "Experience.InvalidDuration": { field: "durationMinutes", step: "details" },
  "Experience.InvalidMaxGuests": { field: "maxGuests", step: "details" },
  "Experience.InvalidLanguages": { field: "languages", step: "details" },
  "Experience.InvalidIncluded": { field: "included", step: "details" },
  "Experience.TooManyImages": { field: "photos", step: "photos" },
  "Experience.InvalidImage": { field: "photos", step: "photos" },
  "Experience.InvalidPrice": { field: "pricePerGuest", step: "price" },
  "Experience.InvalidCurrency": { field: "currency", step: "price" },
};

export const EMPTY_EXPERIENCE: ExperienceFormValues = {
  title: "",
  description: "",
  category: "Food" as ExperienceCategory,
  city: "",
  country: "",
  latitude: Number.NaN,
  longitude: Number.NaN,
  meetingPoint: "",
  durationMinutes: 120,
  maxGuests: 8,
  languages: ["en"],
  included: [],
  photos: [],
  pricePerGuest: Number.NaN,
  currency: "USD",
  isListed: true,
};

export function fromExperience(e: Experience): ExperienceFormValues {
  return {
    title: e.title,
    description: e.description,
    category: e.category,
    city: e.city,
    country: e.country,
    latitude: e.latitude,
    longitude: e.longitude,
    meetingPoint: e.meetingPoint,
    durationMinutes: e.durationMinutes,
    maxGuests: e.maxGuests,
    languages: e.languages,
    included: e.included,
    photos: e.images.map((image) => ({ id: image.id, url: image.url })),
    pricePerGuest: e.pricePerGuest,
    currency: e.currency === "EUR" ? "EUR" : "USD",
    isListed: e.isListed,
  };
}

export function toExperienceRequest(values: ExperienceFormValues): ExperienceRequest {
  return {
    title: values.title.trim(),
    description: values.description.trim(),
    category: values.category,
    city: values.city.trim(),
    country: values.country.trim(),
    latitude: values.latitude,
    longitude: values.longitude,
    meetingPoint: values.meetingPoint.trim(),
    durationMinutes: values.durationMinutes,
    pricePerGuest: values.pricePerGuest,
    currency: values.currency,
    maxGuests: values.maxGuests,
    languages: values.languages,
    included: values.included.map((item) => item.trim()).filter(Boolean),
    imageIds: values.photos.map((photo) => photo.id),
    isListed: values.isListed,
  };
}
