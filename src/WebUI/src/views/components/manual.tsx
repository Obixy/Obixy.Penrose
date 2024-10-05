import { useEffect, useMemo, useState } from "react";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogTrigger,
} from "@/views/components/ui/dialog";
import {
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ChevronUp,
  Cog,
} from "lucide-react";

export function Manual() {
  const [focusIndex, setFocusIndex] = useState(0);

  const buttons = useMemo(
    () => ["up-btn", "left-btn", "down-btn", "right-btn"],
    []
  );

  useEffect(() => {
    const interval = setInterval(() => {
      setFocusIndex((prevIndex) => (prevIndex + 1) % buttons.length);
    }, 3000);

    return () => clearInterval(interval);
  }, [buttons.length]);

  useEffect(() => {
    const button = document.getElementById(buttons[focusIndex]);
    button?.classList.add("border-blue-500");

    return () => {
      button?.classList.remove("border-blue-500");
    };
  }, [buttons, focusIndex]);

  return (
    <Dialog>
      <DialogTrigger className="group h-fit flex text-white items-center gap-2 rounded-full px-4 py-2 transition hover:bg-white/5">
        <Cog className="w-5 h-5" />
      </DialogTrigger>

      <DialogContent className="w-[400px] h-fit !rounded-3xl px-6 py-10 flex flex-col border-white/10 bg-black/15 shadow-xl shadow-black/30 backdrop-blur-2xl text-gray-300">
        <div className="flex flex-col items-center gap-2 justify-between pb-6 my-4 relative">
          <button
            id="up-btn"
            className="w-14 h-14 border rounded-md flex items-center justify-center transition-all duration-300 ease-in"
          >
            <ChevronUp />
          </button>

          <div className="flex items-center gap-2">
            <button
              id="left-btn"
              className="w-14 h-14 border rounded-md flex items-center justify-center transition-all duration-300 ease-in"
            >
              <ChevronLeft />
            </button>

            <button
              id="down-btn"
              className="w-14 h-14 border rounded-md flex items-center justify-center transition-all duration-300 ease-in"
            >
              <ChevronDown />
            </button>

            <button
              id="right-btn"
              className="w-14 h-14 border rounded-md flex items-center justify-center transition-all duration-300 ease-in"
            >
              <ChevronRight />
            </button>
          </div>
          <div className="pointer-events-none absolute inset-x-0 bottom-0 w-full h-1/3 bg-gradient-to-t from-black hidden sm:flex"></div>
        </div>

        <div className="flex flex-col items-center justify-center gap-2">
          <h1 className="text-xl">
            Use the keyboard arrows to move around the map
          </h1>

          <p className="text-sm text-gray-400">
            Use the keyboard arrows to move around the map
          </p>
        </div>

        <DialogFooter className="mt-4">
          <DialogClose type="submit">Continue</DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
