import { useRef, useEffect } from "react";
import { useJobContext } from "@/lib/change-job-id";

import { Sidebar, OptionsBar } from "../components";

export function View() {
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const { jobId } = useJobContext();

  useEffect(() => {
    const iframe = iframeRef.current;

    if (iframe) {
      iframe.onload = function setIframeFocus() {
        iframe.focus();
      };
    }
  }, []);

  useEffect(() => {
    const iframe = iframeRef.current;

    if (iframe) {
      iframe.contentWindow?.postMessage(
        {
          jobId: jobId,
        },
        "*"
      );
    }
  }, [jobId]);

  return (
    <div className="relative">
      <Sidebar />

      <iframe
        ref={iframeRef}
        src="https://nsac-obixy-penrose-f8bygpb0bmavcxez.brazilsouth-01.azurewebsites.net/"
        className="w-full h-screen object-cover"
      ></iframe>

      <OptionsBar />
    </div>
  );
}
