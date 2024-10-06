import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";

import { View } from "@/views/environment/view.tsx";
import { UnitMeasureProvider } from "@/lib/change-unity-measure.tsx";
import { queryClient } from "@/services/index.ts";

import "./styles/index.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <UnitMeasureProvider>
      <QueryClientProvider client={queryClient}>
        <View />
      </QueryClientProvider>
    </UnitMeasureProvider>
  </StrictMode>
);
