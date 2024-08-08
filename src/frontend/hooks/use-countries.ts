import countries from "world-countries";
import { useMemo, useCallback } from "react";

export const useCountries = () => {
  const formattedCountries = useMemo(
    () =>
      countries.map((country) => ({
        value: country.cca2,
        label: country.name.common,
        flag: country.flag,
        latlng: country.latlng,
        region: country.region,
      })) as Country[],
    []
  );

  const getByValue = useCallback(
    (label: string) => {
      return formattedCountries.find((item) => item.label === label);
    },
    [formattedCountries]
  );

  return { countries: formattedCountries, getByValue };
};
