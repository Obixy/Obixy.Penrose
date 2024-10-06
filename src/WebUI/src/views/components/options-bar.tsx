import { useState, useEffect } from "react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/views/components/ui/dropdown-menu";

import { useUnitMeasure } from "@/lib/change-unity-measure";

type Unit = "parallax" | "parsec" | "lightYears" | "kilometers";

const units: Unit[] = ["parallax", "parsec", "lightYears", "kilometers"];

interface OptionsBarProps {
  onUnitChange: (unit: Unit) => void;
}

export function OptionsBar({ onUnitChange }: OptionsBarProps) {
  const { unitMeasure, setUnitMeasure, convertValue } = useUnitMeasure();

  const [value, setValue] = useState<number>(1);
  const [convertedValue, setConvertedValue] = useState<number>(1);

  useEffect(() => {
    setConvertedValue(convertValue(value, "parallax", unitMeasure));
  }, [unitMeasure, value, convertValue]);

  function handleUnitChange(newUnit: Unit) {
    setUnitMeasure(newUnit);
    onUnitChange(newUnit);
  }

  function handleValueChange(event: React.ChangeEvent<HTMLInputElement>) {
    const newValue = parseFloat(event.target.value);
    if (!isNaN(newValue)) {
      setValue(newValue);
    }
  }

  return (
    <div className="fixed bottom-4 right-4 w-fit z-10 flex text-white justify-center rounded-full border-y border-b-white/10 border-t-white/20 bg-black/50 p-4 shadow-xl shadow-black/30 backdrop-blur-3xl">
      <a
        target="_blank"
        href="https://www.obixy.com.br"
        className="group flex items-center gap-2 rounded-full px-4 py-2 transition hover:bg-white/5"
      >
        Obixy.
      </a>

      <div className="ml-4">
        <h3 className="font-semibold text-sm">
          Current Unit of Measure:{" "}
          {unitMeasure.charAt(0).toUpperCase() + unitMeasure.slice(1)}
        </h3>
        <div className="flex items-center">
          <input
            type="number"
            value={value}
            onChange={handleValueChange}
            className="bg-transparent border border-white/20 rounded px-2 py-1 text-sm"
          />
          <span className="ml-2 text-sm">parallax</span>
        </div>
        <h4 className="text-sm">
          {unitMeasure === "kilometers"
            ? convertedValue.toExponential(2)
            : convertedValue.toFixed(2)}{" "}
          {unitMeasure}
        </h4>
      </div>

      <DropdownMenu>
        <DropdownMenuTrigger className="ml-4 flex items-center gap-2 rounded-full px-4 py-2 transition hover:bg-white/5">
          Unit of Measure
        </DropdownMenuTrigger>

        <DropdownMenuContent
          align="center"
          className="flex flex-col text-white rounded-2xl border border-x-0 border-b-white/10 border-t-white/20 bg-black/50 p-2 shadow-xl shadow-black/30 backdrop-blur-3xl"
        >
          {units.map((unit) => (
            <DropdownMenuItem
              key={unit}
              onClick={() => handleUnitChange(unit)}
              className="cursor-pointer hover:bg-white/10 px-4 py-2 rounded-xl"
            >
              {unit.charAt(0).toUpperCase() + unit.slice(1)}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
