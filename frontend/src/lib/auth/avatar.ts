import { API_URL } from "@/lib/env";

/** Public URL of an uploaded file, e.g. the avatar referenced by the token's `imageId` claim. */
export function fileUrl(imageId: string | null | undefined): string | null {
  return imageId ? `${API_URL}/files/${encodeURIComponent(imageId)}` : null;
}
