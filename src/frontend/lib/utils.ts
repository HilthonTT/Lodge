import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

import { API_VERSION, BASE_API_URL } from "@/constants";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function getImageUrl(imageId: string) {
  return `${BASE_API_URL}/api/${API_VERSION}/files/${imageId}`;
}

export function formatPrice(currency: string, price: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: currency,
    minimumFractionDigits: 0, // No decimal places
    maximumFractionDigits: 0, // No decimal places
  }).format(price);
}
