import { useRef, useEffect } from "react";
import { Sidebar, OptionsBar } from "../components";

export function View() {
  const iframeRef = useRef<HTMLIFrameElement>(null);

  useEffect(() => {
    const iframe = iframeRef.current;

    if (iframe) {
      iframe.onload = function setIframeFocus() {
        iframe.focus();
      };
    }

  }, []);

  return (
    <div className="relative">
      <Sidebar />

      <iframe
        ref={iframeRef}
        src="https://nsac-obixy-penrose-f8bygpb0bmavcxez.brazilsouth-01.azurewebsites.net/"
        className="w-full h-screen object-cover"
      ></iframe>

      <OptionsBar onUnitChange={(unit: number) => console.log(unit)} />
    </div>
  );
}
