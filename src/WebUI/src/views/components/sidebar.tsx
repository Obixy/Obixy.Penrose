import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { explanetsService } from "@/services/exoplanets";
import { ExoplanetList } from "./exoplanet";
import { Cog, Orbit, SidebarIcon } from "lucide-react";
import { useJobContext } from "@/lib/change-job-id";

import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from "@/views/components/ui/tabs";
import { constellationService } from "@/services/constellations";

interface SidebarProps {
  onFocus: () => void;
  isOpen: boolean;
  setIsOpen: (isOpen: boolean) => void;
}

export function Sidebar({ onFocus, isOpen, setIsOpen }: SidebarProps) {
  const { data, isLoading } = useQuery({
    queryKey: ["exoplanets"],
    queryFn: explanetsService.getAll,
  });

  const { exoplanet, setExoplanet } = useJobContext();

  const { data: constellations } = useQuery({
    queryKey: ["constellations"],
    queryFn: () => constellationService.getByJobId(exoplanet?.id ?? ""),
    enabled: !!exoplanet,
  });

  console.log(constellations);

  const [search, setSearch] = useState("");
  const [isVisible, setIsVisible] = useState(true);

  const filteredExoplanets = data?.filter((exoplanet) =>
    exoplanet.name.toLowerCase().includes(search.toLowerCase())
  );

  useEffect(() => {
    if (data) {
      setExoplanet(data[0]);
    }
  }, [data, setExoplanet]);

  const sortedExoplanets = filteredExoplanets?.sort((a, b) => {
    if (a.name === "Proxima Centauri") return -1;
    if (b.name === "Proxima Centauri") return 1;
    return 0;
  });

  function toggleSidebar() {
    setIsVisible((prev) => {
      const newVisibleState = !prev;

      onFocus();
      return newVisibleState;
    });
  }

  return isVisible ? (
    <div className="relative w-fit sm:fixed right-0 p-2 rounded-0 border-t sm:border sm:bottom-6 sm:left-6 border-b-white/10 border-t-white/20 !border-x-0 bg-black/50 backdrop-blur-md sm:rounded-xl flex justify-center gap-2">
      <button
        type="button"
        onClick={toggleSidebar}
        className="z-50 px-4 py-2 flex flex-col items-center rounded-md border-white/20 text-white bg-white/10 transition hover:bg-white/20 active:bg-white/10 
      duration-300 transform active:scale-95 aria-[current=page]:bg-primary hover:bg-[#f2f2f2] aria-[current=page]:text-[#ffffff] p-2.5 justify-center ease-in-out"
      >
        <Orbit className="w-5 h-5" />
        <p className="text-sm">Exoplanets</p>
      </button>
    </div>
  ) : (
    <Tabs defaultValue="account" asChild>
      <div
        className={`animate-slidein200 opacity-0 w-[450px] h-[96vh] absolute inset-0 my-auto ml-4 flex grow overflow-hidden rounded-[2rem] border-y border-b-white/10 border-t-white/15 bg-black/15 shadow-xl shadow-black/30 backdrop-blur-2xl`}
      >
        <div className="flex grow flex-col overflow-y-auto">
          <div className="h-fit p-5 flex flex-col gap-5">
            <header className="mb-4 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <img src="/obixy-nasa.png" className="w-12" alt="" />
                <h1 className="text-2xl text-gray-400">Penrose</h1>
              </div>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => setIsOpen(!isOpen)}
                  className="group h-fit flex text-white items-center gap-2 rounded-full px-4 py-2 transition hover:bg-white/5"
                >
                  <Cog className="w-5 h-5" />
                </button>

                <button
                  onClick={toggleSidebar}
                  className="group h-fit flex text-white items-center gap-2 rounded-full px-4 py-2 transition hover:bg-white/5"
                >
                  <SidebarIcon className="w-5 h-5" />
                </button>
              </div>
            </header>

            <div className="flex gap-2">
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="w-full rounded-xl border-0 bg-black/10 text-white px-4 py-2.5 outline-none transition placeholder:text-white/60 focus:bg-black/25 ring-1 ring-white/10 focus:ring-2 focus:ring-white/20"
                placeholder="Search star..."
              />
            </div>

            <TabsList className="grid grid-cols-1 gap-4 lg:grid-cols-3 text-sm">
              <TabsTrigger
                value="account"
                className="flex flex-col text-start text-gray-200 font-light gap-2 rounded-xl bg-white/10 px-3 py-5 transition hover:bg-white/20 active:scale-95 active:bg-white/10"
              >
                <span>Login or register</span>
              </TabsTrigger>

              <TabsTrigger
                value="password"
                className="flex flex-col text-start text-gray-200 font-light gap-2 rounded-xl bg-white/10 px-3 py-5 transition hover:bg-white/20 active:scale-95 active:bg-white/10 disabled:opacity-50"
                disabled={!exoplanet}
              >
                <span>Constelations and score</span>
              </TabsTrigger>

              <TabsTrigger
                value="example"
                className="flex flex-col text-start text-gray-200 font-light gap-2 rounded-xl bg-white/10 px-3 py-5 transition hover:bg-white/20 active:scale-95 active:bg-white/10"
              >
                <span>Planets and Sky</span>
              </TabsTrigger>
            </TabsList>
          </div>

          <TabsContent value="account">
            <div className="w-full flex flex-col gap-4 overflow-y-auto pl-5 pb-28 pr-2.5 relative">
              {isLoading ? (
                <div className="flex justify-start h-full">
                  <div className="animate-spin rounded-full h-32 w-32 border-t-2 border-b-2 border-white/10"></div>
                  <span className="sr-only">Loading...</span>
                </div>
              ) : (
                sortedExoplanets?.map((exoplanet) => (
                  <ExoplanetList key={exoplanet.id} exoplanet={exoplanet} />
                ))
              )}
            </div>
          </TabsContent>

          <TabsContent value="password"></TabsContent>

          <TabsContent value="example">Example content</TabsContent>

          <div className="pointer-events-none absolute inset-x-0 bottom-0 w-full h-1/3 bg-gradient-to-t from-current hidden sm:flex"></div>
        </div>
      </div>
    </Tabs>
  );
}
