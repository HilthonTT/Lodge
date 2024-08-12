"use client";

import "react-date-range/dist/styles.css";
import "react-date-range/dist/theme/default.css";

import { addDays } from "date-fns";
import { DateRange, RangeKeyDict, Range } from "react-date-range";

type Props = {
  value: Range;
  disabledDates?: Date[];
  onChange: (value: RangeKeyDict) => void;
};

export const Calendar = ({ value, disabledDates, onChange }: Props) => {
  const tomorrow = addDays(new Date(), 1);

  return (
    <DateRange
      rangeColors={["#6366F1"]}
      ranges={[value]}
      minDate={tomorrow}
      onChange={onChange}
      direction="vertical"
      showDateDisplay={false}
      disabledDates={disabledDates}
    />
  );
};
