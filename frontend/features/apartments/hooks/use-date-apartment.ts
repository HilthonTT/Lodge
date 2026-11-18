import { create } from "zustand";

type DateApartmentState = {
  isOpen: boolean;
  onOpen: () => void;
  onClose: () => void;
};

export const useDateApartment = create<DateApartmentState>((set) => ({
  isOpen: false,
  onOpen: () => set({ isOpen: true }),
  onClose: () => set({ isOpen: false }),
}));
