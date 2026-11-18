import { create } from "zustand";

type LocationApartmentState = {
  isOpen: boolean;
  onOpen: () => void;
  onClose: () => void;
};

export const useLocationApartment = create<LocationApartmentState>((set) => ({
  isOpen: false,
  onOpen: () => set({ isOpen: true }),
  onClose: () => set({ isOpen: false }),
}));
