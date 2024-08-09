import { create } from "zustand";

type CountApartmentState = {
  isOpen: boolean;
  onOpen: () => void;
  onClose: () => void;
};

export const useCountApartment = create<CountApartmentState>((set) => ({
  isOpen: false,
  onOpen: () => set({ isOpen: true }),
  onClose: () => set({ isOpen: false }),
}));
