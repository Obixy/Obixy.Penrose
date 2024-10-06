import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";

import { View } from "@/views/environment/view.tsx";
import { UnitMeasureProvider } from "@/lib/change-unity-measure.tsx";
import { queryClient } from "@/services/index.ts";

import "./styles/index.css";
import { JobProvider } from "./lib/change-job-id";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <UnitMeasureProvider>
      <JobProvider>
        <QueryClientProvider client={queryClient}>
          <View />
        </QueryClientProvider>
      </JobProvider>
    </UnitMeasureProvider>
  </StrictMode>
);
