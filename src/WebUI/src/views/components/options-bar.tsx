import { useState, useEffect } from "react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/views/components/ui/dropdown-menu";

import { useUnitMeasure } from "@/lib/change-unity-measure";
import { useJobContext } from "@/lib/change-job-id";

type Unit = "mas" | "parsec" | "lightYears" | "kilometers";

const units: Unit[] = ["mas", "parsec", "lightYears", "kilometers"];

export function OptionsBar() {
  const { exoplanet } = useJobContext();
  const { unitMeasure, setUnitMeasure, convertValue } = useUnitMeasure();

  const [value] = useState<number>(1);
  const [, setConvertedValue] = useState<number>(1);

  useEffect(() => {
    setConvertedValue(convertValue(value, "mas", unitMeasure));
  }, [unitMeasure, value, convertValue]);

  function handleUnitChange(newUnit: Unit) {
    setUnitMeasure(newUnit);
  }

  return (
    <div className="fixed bottom-4 right-4 w-fit z-10 flex gap-2 text-white justify-center rounded-full border-y border-b-white/10 border-t-white/20 bg-black/50 px-4 py-2 shadow-xl shadow-black/30 backdrop-blur-3xl">
      <DropdownMenu>
        <DropdownMenuTrigger className="flex items-center gap-2 rounded-full px-4 py-2 transition hover:bg-white/5">
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
              {unit === "mas"
                ? "Parallax (mas)"
                : unit.charAt(0).toUpperCase() + unit.slice(1)}
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>

      <button
        type="button"
        className="group flex items-center gap-2 rounded-full px-4 py-2 transition bg-white text-black"
      >
        {exoplanet?.name}
      </button>
    </div>
  );
}
