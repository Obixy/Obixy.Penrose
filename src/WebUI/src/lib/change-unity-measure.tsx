import { createContext, useContext, useState, ReactNode } from "react";

type Unit = "mas" | "parsec" | "lightYears" | "kilometers";

type UnitMeasureContextType = {
  unitMeasure: Unit;
  setUnitMeasure: (unit: Unit) => void;
  convertValue: (value: number, fromUnit: Unit, toUnit: Unit) => number;
};

const UnitMeasureContext = createContext<UnitMeasureContextType | undefined>(
  undefined
);

interface UnitMeasureProviderProps {
  children: ReactNode;
}

export function UnitMeasureProvider({ children }: UnitMeasureProviderProps) {
  const [unitMeasure, setUnitMeasure] = useState<Unit>("mas");

  function convertValue(value: number, fromUnit: Unit, toUnit: Unit): number {
    if (fromUnit === toUnit) {
      return value;
    }

    let parallaxValue: number = value;
    const parsecValue: number = 1000 / value;

    switch (fromUnit) {
      case "mas":
        parallaxValue = value;
        break;
      case "parsec":
        parallaxValue = 1000 / value;
        break;
      case "lightYears":
        parallaxValue = parsecValue * 3.262;
        break;
      case "kilometers":
        parallaxValue = parsecValue * 3.086e13;
        break;
    }

    switch (toUnit) {
      case "mas":
        return parallaxValue;
      case "parsec":
        return 1000 / parallaxValue;
      case "lightYears":
        return parsecValue * 3.262;
      case "kilometers":
        return parallaxValue * 3.086e13;
    }

    return value;
  }

  return (
    <UnitMeasureContext.Provider
      value={{ unitMeasure, setUnitMeasure, convertValue }}
    >
      {children}
    </UnitMeasureContext.Provider>
  );
}

export function useUnitMeasure() {
  const context = useContext(UnitMeasureContext);

  if (!context) {
    throw new Error("useUnitMeasure must be used within a UnitMeasureProvider");
  }

  return context;
}
