import type { useTranslations } from "next-intl";
import type { Path } from "react-hook-form";
import { z } from "zod";

import { AMENITY_VALUES, CANCELLATION_POLICIES, type ListingRequest, PROPERTY_TYPES } from "@/types/api";

/** Translator for `hosting.wizard.errors`. */
export type WizardTranslator = ReturnType<typeof useTranslations<"hosting.wizard.errors">>;

export const LIMITS = {
  name: 256,
  description: 1000,
  address: 200,
  houseRules: 2000,
  checkInInstructions: 2000,
  photos: 20,
  bedrooms: [0, 50],
  beds: [1, 100],
  bathrooms: [0, 50],
  guests: [1, 50],
  discount: [0, 90],
} as const;

const TIME = /^([01]\d|2[0-3]):[0-5]\d$/;

/** The whole wizard form. Steps validate their own slice with `trigger(STEP_FIELDS[i])`. */
export const createListingSchema = (t: WizardTranslator) => {
  const num = (message: string) => z.number({ error: message });
  return z
    .object({
      name: z.string().trim().min(1, t("nameRequired")).max(LIMITS.name, t("nameTooLong", { max: LIMITS.name })),
      description: z
        .string()
        .trim()
        .min(1, t("descriptionRequired"))
        .max(LIMITS.description, t("descriptionTooLong", { max: LIMITS.description })),
      propertyType: z.enum(PROPERTY_TYPES, { error: t("propertyTypeRequired") }),
      address: z.object({
        country: z.string().trim().min(1, t("countryRequired")).max(LIMITS.address, t("tooLong", { max: LIMITS.address })),
        state: z.string().trim().max(LIMITS.address, t("tooLong", { max: LIMITS.address })),
        zipCode: z.string().trim().max(LIMITS.address, t("tooLong", { max: LIMITS.address })),
        city: z.string().trim().min(1, t("cityRequired")).max(LIMITS.address, t("tooLong", { max: LIMITS.address })),
        street: z.string().trim().min(1, t("streetRequired")).max(LIMITS.address, t("tooLong", { max: LIMITS.address })),
      }),
      latitude: num(t("pinRequired")).min(-90, t("pinRequired")).max(90, t("pinRequired")),
      longitude: num(t("pinRequired")).min(-180, t("pinRequired")).max(180, t("pinRequired")),
      bedrooms: num(t("capacity")).int().min(LIMITS.bedrooms[0]).max(LIMITS.bedrooms[1]),
      beds: num(t("capacity")).int().min(LIMITS.beds[0]).max(LIMITS.beds[1]),
      bathrooms: num(t("capacity"))
        .min(LIMITS.bathrooms[0])
        .max(LIMITS.bathrooms[1])
        .refine((v) => Number.isInteger(v * 2), t("halfSteps")),
      maximumGuestCount: num(t("capacity")).int().min(LIMITS.guests[0]).max(LIMITS.guests[1]),
      amenities: z.array(z.enum(AMENITY_VALUES)),
      photos: z
        .array(z.object({ id: z.string(), url: z.string() }))
        .min(1, t("photosRequired"))
        .max(LIMITS.photos, t("photosTooMany", { max: LIMITS.photos })),
      price: num(t("priceRequired")).positive(t("priceRequired")).max(100_000, t("priceTooHigh")),
      currency: z.enum(["USD", "EUR"]),
      cleaningFee: num(t("feeInvalid")).min(0, t("feeInvalid")).max(100_000, t("priceTooHigh")),
      weekendPrice: num(t("priceRequired")).positive(t("priceRequired")).max(100_000, t("priceTooHigh")).nullable(),
      weeklyDiscountPercent: num(t("discount")).int(t("discount")).min(0, t("discount")).max(90, t("discount")),
      monthlyDiscountPercent: num(t("discount")).int(t("discount")).min(0, t("discount")).max(90, t("discount")),
      minNights: num(t("nightsInvalid")).int(t("nightsInvalid")).min(1, t("nightsInvalid")).max(365, t("nightsInvalid")),
      maxNights: num(t("nightsInvalid")).int(t("nightsInvalid")).min(1, t("nightsInvalid")).max(365, t("nightsInvalid")),
      instantBook: z.boolean(),
      cancellationPolicy: z.enum(CANCELLATION_POLICIES),
      checkInTime: z.string().regex(TIME, t("timeInvalid")),
      checkOutTime: z.string().regex(TIME, t("timeInvalid")),
      petsAllowed: z.boolean(),
      smokingAllowed: z.boolean(),
      partiesAllowed: z.boolean(),
      houseRules: z.string().max(LIMITS.houseRules, t("tooLong", { max: LIMITS.houseRules })),
      checkInInstructions: z
        .string()
        .max(LIMITS.checkInInstructions, t("tooLong", { max: LIMITS.checkInInstructions })),
      isListed: z.boolean(),
    })
    .refine((v) => v.minNights <= v.maxNights, { path: ["maxNights"], message: t("stayLimits") });
};

export type ListingFormValues = z.infer<ReturnType<typeof createListingSchema>>;
export type ListingField = Path<ListingFormValues>;

export type ServerErrorKey =
  | "name"
  | "description"
  | "propertyType"
  | "address"
  | "coordinates"
  | "capacity"
  | "amenity"
  | "photos"
  | "price"
  | "currency"
  | "discount"
  | "stayLimits"
  | "time"
  | "houseRules"
  | "checkInInstructions"
  | "cancellationPolicy";

export const STEPS = ["basics", "location", "details", "photos", "pricing", "rules", "review"] as const;
export type StepKey = (typeof STEPS)[number];

/** Fields each step validates before moving on. */
export const STEP_FIELDS: Record<StepKey, ListingField[]> = {
  basics: ["name", "description", "propertyType"],
  location: ["address.country", "address.street", "address.city", "address.state", "address.zipCode", "latitude", "longitude"],
  details: ["bedrooms", "beds", "bathrooms", "maximumGuestCount", "amenities"],
  photos: ["photos"],
  pricing: [
    "price",
    "currency",
    "cleaningFee",
    "weekendPrice",
    "weeklyDiscountPercent",
    "monthlyDiscountPercent",
    "minNights",
    "maxNights",
    "instantBook",
  ],
  rules: [
    "checkInTime",
    "checkOutTime",
    "petsAllowed",
    "smokingAllowed",
    "partiesAllowed",
    "houseRules",
    "checkInInstructions",
    "cancellationPolicy",
  ],
  review: ["isListed"],
};

/** Backend validation codes → the step and field they belong to (message key under `hosting.wizard.server`). */
export const SERVER_CODES: Record<string, { step: StepKey; field: ListingField; key: ServerErrorKey }> = {
  "Name.Empty": { step: "basics", field: "name", key: "name" },
  "Name.TooLong": { step: "basics", field: "name", key: "name" },
  "Description.Empty": { step: "basics", field: "description", key: "description" },
  "Description.TooLong": { step: "basics", field: "description", key: "description" },
  "Listing.InvalidPropertyType": { step: "basics", field: "propertyType", key: "propertyType" },
  "Listing.InvalidAddress": { step: "location", field: "address.street", key: "address" },
  "Apartment.InvalidCoordinates": { step: "location", field: "latitude", key: "coordinates" },
  "Apartment.InvalidCapacity": { step: "details", field: "bedrooms", key: "capacity" },
  "Listing.InvalidAmenity": { step: "details", field: "amenities", key: "amenity" },
  "Apartment.InvalidImageCount": { step: "photos", field: "photos", key: "photos" },
  "Apartment.DuplicateImage": { step: "photos", field: "photos", key: "photos" },
  "Apartment.InvalidImage": { step: "photos", field: "photos", key: "photos" },
  "Apartment.InvalidPrice": { step: "pricing", field: "price", key: "price" },
  "Listing.InvalidCurrency": { step: "pricing", field: "currency", key: "currency" },
  "Apartment.InvalidDiscount": { step: "pricing", field: "weeklyDiscountPercent", key: "discount" },
  "Apartment.InvalidStayLimits": { step: "pricing", field: "minNights", key: "stayLimits" },
  "Listing.InvalidTime": { step: "rules", field: "checkInTime", key: "time" },
  "Apartment.HouseRulesTooLong": { step: "rules", field: "houseRules", key: "houseRules" },
  "Apartment.CheckInInstructionsTooLong": { step: "rules", field: "checkInInstructions", key: "checkInInstructions" },
  "Apartment.InvalidCancellationPolicy": { step: "rules", field: "cancellationPolicy", key: "cancellationPolicy" },
};

export const EMPTY_LISTING: ListingFormValues = {
  name: "",
  description: "",
  propertyType: "House",
  address: { country: "", state: "", zipCode: "", city: "", street: "" },
  latitude: Number.NaN,
  longitude: Number.NaN,
  bedrooms: 1,
  beds: 1,
  bathrooms: 1,
  maximumGuestCount: 2,
  amenities: [],
  photos: [],
  price: Number.NaN,
  currency: "USD",
  cleaningFee: 0,
  weekendPrice: null,
  weeklyDiscountPercent: 0,
  monthlyDiscountPercent: 0,
  minNights: 1,
  maxNights: 365,
  instantBook: true,
  checkInTime: "15:00",
  checkOutTime: "11:00",
  petsAllowed: false,
  smokingAllowed: false,
  partiesAllowed: false,
  houseRules: "",
  checkInInstructions: "",
  cancellationPolicy: "Moderate",
  isListed: true,
};

export function toListingRequest(values: ListingFormValues): ListingRequest {
  const trim = (s: string) => s.trim();
  return {
    name: trim(values.name),
    description: trim(values.description),
    propertyType: values.propertyType,
    address: {
      country: trim(values.address.country),
      state: trim(values.address.state),
      zipCode: trim(values.address.zipCode),
      city: trim(values.address.city),
      street: trim(values.address.street),
    },
    latitude: Math.round(values.latitude * 1e6) / 1e6,
    longitude: Math.round(values.longitude * 1e6) / 1e6,
    price: values.price,
    currency: values.currency,
    cleaningFee: values.cleaningFee,
    weekendPrice: values.weekendPrice,
    weeklyDiscountPercent: values.weeklyDiscountPercent,
    monthlyDiscountPercent: values.monthlyDiscountPercent,
    bedrooms: values.bedrooms,
    beds: values.beds,
    bathrooms: values.bathrooms,
    maximumGuestCount: values.maximumGuestCount,
    amenities: values.amenities,
    checkInTime: values.checkInTime,
    checkOutTime: values.checkOutTime,
    petsAllowed: values.petsAllowed,
    smokingAllowed: values.smokingAllowed,
    partiesAllowed: values.partiesAllowed,
    houseRules: values.houseRules.trim() || null,
    checkInInstructions: values.checkInInstructions.trim() || null,
    instantBook: values.instantBook,
    minNights: values.minNights,
    maxNights: values.maxNights,
    imageIds: values.photos.map((p) => p.id),
    isListed: values.isListed,
    cancellationPolicy: values.cancellationPolicy,
  };
}
