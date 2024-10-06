import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import { View } from "./views/environment/view.tsx";
import { UnitMeasureProvider } from "./lib/change-unity-measure.tsx";

import "./styles/index.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <UnitMeasureProvider>
      <View />
    </UnitMeasureProvider>
  </StrictMode>
);
