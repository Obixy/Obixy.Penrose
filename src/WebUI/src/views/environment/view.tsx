import { useRef, useEffect, useState } from "react";
import { useJobContext } from "@/lib/change-job-id";
import { OptionsBar, Sidebar } from "../components";
import { Manual } from "../components/manual";
import { MessageDisplay } from "../components/message-display";

export function View() {
  const { exoplanet } = useJobContext();
  const iframeRef = useRef<HTMLIFrameElement>(null);

  const [open, setOpen] = useState(false);

  function handleFocus() {
    iframeRef.current?.focus();
  }

  useEffect(() => {
    const hasOpened = localStorage.getItem("@nasaspaceapps:manual");
    const hasOpenedBoolean = hasOpened === "true";

    if (!hasOpenedBoolean) {
      setOpen(true);
      localStorage.setItem("@nasaspaceapps:manual", "true");
    }
  }, []);

  useEffect(() => {
    function handleMessage(e: any) {
      if (
        e.origin !==
        "https://nsac-obixy-penrose-f8bygpb0bmavcxez.brazilsouth-01.azurewebsites.net"
      ) {
        return;
      }

      console.log(e.data);
    }

    window.addEventListener("message", handleMessage);
  }, []);

  useEffect(() => {
    const hasShownAlert = localStorage.getItem("@nasaspaceapps:manual");

    if (!hasShownAlert) {
      setOpen(true);
      localStorage.setItem("@nasaspaceapps:manual", "true");
    }
  }, []);

  useEffect(() => {
    const iframe = iframeRef.current;

    if (iframe) {
      iframe.contentWindow?.postMessage(
        {
          jobId: exoplanet?.id,
        },
        "*"
      );
    }
  }, [exoplanet?.id]);

  useEffect(() => {
    handleFocus();
  }, []);

  return (
    <div className="relative">
      <Sidebar
        onFocus={() => handleFocus()}
        isOpen={open}
        setIsOpen={setOpen}
        iframe={iframeRef.current}
      />

      <iframe
        ref={iframeRef}
        src="https://nsac-obixy-penrose-f8bygpb0bmavcxez.brazilsouth-01.azurewebsites.net"
        className="w-full h-screen object-cover"
      ></iframe>

      <OptionsBar />

      <MessageDisplay />

      <Manual isOpen={open} setIsOpen={setOpen} />
    </div>
  );
}
