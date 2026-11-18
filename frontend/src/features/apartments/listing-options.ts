import {
  AirVent,
  Bath,
  Building,
  Building2,
  Castle,
  Caravan,
  CookingPot,
  Dumbbell,
  EvCharger,
  Flame,
  Flower2,
  Globe,
  Heater,
  Home,
  House,
  KeyRound,
  Landmark,
  Laptop,
  type LucideIcon,
  Mountain,
  MountainSnow,
  PawPrint,
  Sailboat,
  Ship,
  Snowflake,
  Sofa,
  SquareParking,
  Tent,
  TentTree,
  Tractor,
  TreeDeciduous,
  TreePalm,
  TreePine,
  Trees,
  Umbrella,
  Warehouse,
  WashingMachine,
  Waves,
  WavesLadder,
  Wifi,
} from "lucide-react";

import type { Amenity, PropertyType } from "@/types/api";

/**
 * Shared listing vocabulary for search filters, the category bar, listing pages
 * and the hosting wizard. Labels live in messages/*.json:
 * `listing.amenities.<value>`, `listing.propertyTypes.<value>` and
 * `listing.amenityGroups.<group>`.
 */

/** Real amenities, grouped as on the listing page and in the filters dialog. */
export const AMENITY_GROUPS = [
  {
    key: "essentials",
    amenities: ["WiFi", "Kitchen", "Washer", "Heating", "AirConditioning", "Workspace", "SelfCheckIn"],
  },
  {
    key: "features",
    amenities: ["SwimmingPool", "HotTub", "Spa", "Gym", "Fireplace", "Terrace", "Parking", "EVCharger", "PetFriendly"],
  },
  {
    key: "location",
    amenities: ["MountainView", "GardenView", "Lakefront", "BeachAccess"],
  },
] as const satisfies readonly { key: string; amenities: readonly Amenity[] }[];

export type AmenityGroupKey = (typeof AMENITY_GROUPS)[number]["key"];

/** Every real amenity (not a category tag). */
export const REAL_AMENITIES: readonly Amenity[] = AMENITY_GROUPS.flatMap((g) => g.amenities);

/** Airbnb-style category tags shown in the category bar; they filter through `amenities`. */
export const CATEGORY_TAGS = [
  "Cabins",
  "Treehouses",
  "Lakefront",
  "Castles",
  "Domes",
  "Tropical",
  "Mansions",
  "Boats",
  "TopCities",
  "Camping",
  "Farms",
  "Caves",
  "Barns",
] as const satisfies readonly Amenity[];

export type CategoryTag = (typeof CATEGORY_TAGS)[number];

/** Tags a host can attach to a listing in the wizard (Lakefront is a real amenity there). */
export const LISTING_TAGS: readonly Amenity[] = CATEGORY_TAGS.filter((c) => c !== "Lakefront");

export const AMENITY_ICONS: Record<Amenity, LucideIcon> = {
  WiFi: Wifi,
  AirConditioning: AirVent,
  Parking: SquareParking,
  PetFriendly: PawPrint,
  SwimmingPool: WavesLadder,
  Gym: Dumbbell,
  Spa: Flower2,
  Terrace: Sofa,
  MountainView: Mountain,
  GardenView: Trees,
  Cabins: TreePine,
  Lakefront: Waves,
  Treehouses: TreeDeciduous,
  Camping: Tent,
  Castles: Castle,
  Farms: Tractor,
  Boats: Sailboat,
  Domes: Globe,
  Tropical: TreePalm,
  Mansions: Landmark,
  Caves: MountainSnow,
  Barns: Warehouse,
  TopCities: Building2,
  Kitchen: CookingPot,
  Washer: WashingMachine,
  Heating: Heater,
  Workspace: Laptop,
  HotTub: Bath,
  Fireplace: Flame,
  BeachAccess: Umbrella,
  EVCharger: EvCharger,
  SelfCheckIn: KeyRound,
};

export const PROPERTY_TYPE_ICONS: Record<PropertyType, LucideIcon> = {
  Apartment: Building,
  House: House,
  Cabin: TreePine,
  Villa: Home,
  Cottage: TentTree,
  Treehouse: TreeDeciduous,
  Houseboat: Ship,
  Igloo: Snowflake,
  Other: Caravan,
};

export const isCategoryTag = (value: string): value is CategoryTag =>
  (CATEGORY_TAGS as readonly string[]).includes(value);

