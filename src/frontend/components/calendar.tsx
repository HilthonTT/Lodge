"use client";

import "react-date-range/dist/styles.css";
import "react-date-range/dist/theme/default.css";

import { DateRange, RangeKeyDict, Range } from "react-date-range";

type Props = {
  value: Range;
  disabledDates?: Date[];
  onChange: (value: RangeKeyDict) => void;
};

export const Calendar = ({ value, disabledDates, onChange }: Props) => {
  return (
    <DateRange
      editableDateInputs
      rangeColors={["#6366F1"]}
      ranges={[value]}
      date={new Date()}
      onChange={onChange}
      direction="vertical"
      showDateDisplay={false}
      disabledDates={disabledDates}
    />
  );
};
