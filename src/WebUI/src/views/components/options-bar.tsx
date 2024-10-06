import { useState } from "react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/views/components/ui/dropdown-menu";

enum Units {
  "Light-Years" = 1,
  "Parsec" = 2,
  "Kilometer" = 3,
}

interface OptionsBarProps {
  onUnitChange: (unit: number) => void;
}

export function OptionsBar({ onUnitChange }: OptionsBarProps) {
  const [, setSelectedUnit] = useState<number | null>(null);

  function handleUnitChange(unit: number) {
    setSelectedUnit(unit);
    onUnitChange(unit);
  }

  return (
    <div className="fixed bottom-4 right-4 w-fit z-10 flex text-white flex-none justify-center overflow-hidden rounded-full border-y border-b-white/10 border-t-white/20 bg-black/50 p-2 shadow-xl shadow-black/30 backdrop-blur-3xl sm:mr-2">
      <a
        target="_blank"
        href="https://www.obixy.com.br"
        className="group h-fit flex items-center gap-2 rounded-full px-4 py-2 transition hover:bg-white/5"
      >
        Obixy.
      </a>

      <DropdownMenu>
        <DropdownMenuTrigger className="group h-fit flex items-center gap-2 rounded-full px-4 py-2 transition hover:bg-white/5">
          Unit of measure
        </DropdownMenuTrigger>

        <DropdownMenuContent
          align="center"
          className="flex flex-col text-white flex-none justify-center overflow-hidden rounded-2xl border border-x-0 border-b-white/10 border-t-white/20 bg-black/50 p-2 shadow-xl shadow-black/30 backdrop-blur-3xl sm:mr-2"
        >
          {Object.entries(Units)
            .filter(([key]) => isNaN(Number(key)))
            .map(([key, value]) => (
              <DropdownMenuItem
                key={value}
                onClick={() => handleUnitChange(value as number)}
              >
                {key}
              </DropdownMenuItem>
            ))}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
