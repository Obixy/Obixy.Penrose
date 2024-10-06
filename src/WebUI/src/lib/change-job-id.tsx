import { createContext, useState, ReactNode, useContext } from "react";

interface JobContextType {
  jobId: string | null;
  setJobId: (id: string) => void;
}

const JobContext = createContext<JobContextType | undefined>(undefined);

interface JobProviderProps {
  children: ReactNode;
}

export function JobProvider({ children }: JobProviderProps) {
  const [jobId, setJobId] = useState<string | null>(null);

  return (
    <JobContext.Provider value={{ jobId, setJobId }}>
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
