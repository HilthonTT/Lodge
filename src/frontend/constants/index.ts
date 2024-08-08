import countries from "world-countries";
import {
  FaWifi,
  FaSnowflake,
  FaCar,
  FaPaw,
  FaSwimmingPool,
  FaDumbbell,
  FaSpa,
  FaMountain,
  FaTree,
  FaHome,
  FaRocket,
  FaWater,
  FaCampground,
  FaGlobeAmericas,
  FaHouseDamage,
  FaCity,
} from "react-icons/fa";
import { GiCastle, GiTreehouse, GiCaveEntrance, GiBarn } from "react-icons/gi";
import { MdOutlineAgriculture, MdCabin } from "react-icons/md";
import { LuPalmtree, LuFlower2 } from "react-icons/lu";
import { IoBoatOutline } from "react-icons/io5";

export const BASE_API_URL = process.env.NEXT_PUBLIC_API_BASE_URL!;
export const API_VERSION = process.env.NEXT_PUBLIC_API_VERSION!;

export const AMENITIES = [
  {
    index: 1,
    label: "WiFi",
    icon: FaWifi,
  },
  {
    index: 2,
    label: "Air Conditioning",
    icon: FaSnowflake,
  },
  {
    index: 3,
    label: "Parking",
    icon: FaCar,
  },
  {
    index: 4,
    label: "Pet Friendly",
    icon: FaPaw,
  },
  {
    index: 5,
    label: "Swimming Pool",
    icon: FaSwimmingPool,
  },
  {
    index: 6,
    label: "Gym",
    icon: FaDumbbell,
  },
  {
    index: 7,
    label: "Spa",
    icon: FaSpa,
  },
  {
    index: 8,
    label: "Terrace",
    icon: LuFlower2,
  },
  {
    index: 9,
    label: "Mountain View",
    icon: FaMountain,
  },
  {
    index: 10,
    label: "Garden View",
    icon: FaTree,
  },
  {
    index: 11,
    label: "Countryside",
    icon: FaHome,
  },
  {
    index: 12,
    label: "Tiny Homes",
    icon: FaHome,
  },
  {
    index: 13,
    label: "OMG",
    icon: FaRocket,
  },
  {
    index: 14,
    label: "Cabins",
    icon: MdCabin,
  },
  {
    index: 15,
    label: "Lakefront",
    icon: FaWater,
  },
  {
    index: 16,
    label: "Treehouses",
    icon: GiTreehouse,
  },
  {
    index: 17,
    label: "Camping",
    icon: FaCampground,
  },
  {
    index: 18,
    label: "Castles",
    icon: GiCastle,
  },
  {
    index: 19,
    label: "Farms",
    icon: MdOutlineAgriculture,
  },
  {
    index: 20,
    label: "Boats",
    icon: IoBoatOutline,
  },
  {
    index: 21,
    label: "Domes",
    icon: FaGlobeAmericas,
  },
  {
    index: 22,
    label: "Tropical",
    icon: LuPalmtree,
  },
  {
    index: 23,
    label: "Mansions",
    icon: FaHouseDamage,
  },
  {
    index: 24,
    label: "Caves",
    icon: GiCaveEntrance,
  },
  {
    index: 25,
    label: "Barns",
    icon: GiBarn,
  },
  {
    index: 26,
    label: "Top Cities",
    icon: FaCity,
  },
];
