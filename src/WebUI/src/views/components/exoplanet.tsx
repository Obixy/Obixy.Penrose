import { useEffect, useState } from "react";
import { ExoplanetProps } from "@/types";
import { useUnitMeasure } from "@/lib/change-unity-measure";
import { useJobContext } from "@/lib/change-job-id";

interface ExoplanetListProps {
  exoplanet: ExoplanetProps;
}

export function ExoplanetList({ exoplanet }: ExoplanetListProps) {
  const { unitMeasure, convertValue } = useUnitMeasure();
  const { exoplanet: selectedExoplanet, setExoplanet } = useJobContext();

  const [isSelected, setIsSelected] = useState(false);

  useEffect(() => {
    setIsSelected(selectedExoplanet?.name === exoplanet.name);
  }, [selectedExoplanet, exoplanet.name]);

  const parallaxValue = exoplanet.parallax;
  const convertedValue = convertValue(parallaxValue, "mas", unitMeasure);

  console.log(exoplanet);

  return (
    <button
      key={exoplanet.name}
      className={`w-full flex flex-col gap-4 rounded-xl ring-1 ring-white/10 bg-white/10 px-3 py-6 text-center text-sm transition hover:bg-white/20 active:scale-95 active:bg-white/10 relative ${
        isSelected
          ? "bg-secondary hover:bg-secondary/90 text-white"
          : "bg-white text-black"
      }`}
      onClick={() => {
        setExoplanet(exoplanet);
      }}
    >
      <img
        src="./image-removebg-preview (1).png"
        className="absolute top-2 right-2 w-28"
        alt="exoplanet"
      />

      <span className="ring-1 ring-white/10 text-gray-300 rounded-full px-2 py-1">
        {unitMeasure === "kilometers"
          ? convertedValue.toExponential(2)
          : convertedValue.toFixed(2)}{" "}
        {unitMeasure}
      </span>
      <p className="max-w-[60%] text-start text-white text-2xl">
        {exoplanet.name}
      </p>
    </button>
  );
}
