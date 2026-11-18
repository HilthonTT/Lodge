import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";
import { format, parseISO } from "date-fns";

import { API_VERSION, BASE_API_URL, DATE_FORMAT } from "@/constants";

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

export function formatCurrency(value: number) {
  return Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
  }).format(value);
}

export function calculatePages(totalPages: number, currentPage: number) {
  let pages = [];
  if (totalPages <= 3) {
    pages = Array.from({ length: totalPages }, (_, i) => i + 1);
  } else if (currentPage <= 2) {
    pages = [1, 2, 3];
  } else if (currentPage >= totalPages - 1) {
    pages = [totalPages - 2, totalPages - 1, totalPages];
  } else {
    pages = [currentPage - 1, currentPage, currentPage + 1];
  }

  return pages;
}

export function formatDate(startDate: string, endDate: string) {
  const start = parseISO(startDate);
  const end = parseISO(endDate);

  const formattedStart = format(start, DATE_FORMAT);
  const formattedEnd = format(end, DATE_FORMAT);

  const formattedDateRange = `${formattedStart} - ${formattedEnd}`;

  return formattedDateRange;
}
