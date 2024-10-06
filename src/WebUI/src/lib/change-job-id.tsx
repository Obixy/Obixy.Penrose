import { ExoplanetProps } from "@/types";
import { createContext, useState, ReactNode, useContext } from "react";

interface JobContextType {
  exoplanet: ExoplanetProps | null;
  setExoplanet: (exoplanet: ExoplanetProps) => void;
}

const JobContext = createContext<JobContextType | undefined>(undefined);

interface JobProviderProps {
  children: ReactNode;
}

export function JobProvider({ children }: JobProviderProps) {
  const [exoplanet, setExoplanet] = useState<ExoplanetProps | null>(null);

  return (
    <JobContext.Provider value={{ exoplanet, setExoplanet }}>
      {children}
    </JobContext.Provider>
  );
}

export function useJobContext() {
  const context = useContext(JobContext);

  if (!context) {
    throw new Error("useJobContext must be used within a JobProvider");
  }

  return context;
}
