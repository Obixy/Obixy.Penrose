import { Manual } from "./manual";

export function Sidebar() {
  return (
    <div className="w-[450px] h-[96vh] absolute inset-0 my-auto ml-4 flex grow overflow-hidden rounded-[2rem] border-y border-b-white/10 border-t-white/15 bg-black/15 shadow-xl shadow-black/30 backdrop-blur-2xl">
      <div className="flex grow flex-col gap-5 overflow-y-auto">
        <div className="p-5 flex grow flex-col gap-5">
          <header className="mb-4 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <img src="/obixy-nasa.png" className="w-12" alt="" />
              <h1 className="text-2xl text-gray-400">Penrose</h1>
            </div>

            <Manual />
          </header>

          <div className="flex gap-2">
            <input
              type="text"
              className="w-full rounded-xl border-0 bg-black/10 px-4 py-2.5 outline-none transition placeholder:text-white/60 focus:bg-black/25 ring-1 ring-white/10 focus:ring-2 focus:ring-white/20"
              placeholder="Search star..."
            />
          </div>

          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3 text-sm">
            <button className="flex flex-col text-start text-gray-200 font-light gap-2 rounded-xl bg-white/10 px-3 py-5 transition hover:bg-white/20 active:scale-95 active:bg-white/10">
              <span>Constelations and score</span>
            </button>
            <button className="flex flex-col text-start text-gray-200 font-light gap-2 rounded-xl bg-white/10 px-3 py-5 transition hover:bg-white/20 active:scale-95 active:bg-white/10">
              <span>Login or register</span>
            </button>
            <button className="flex flex-col text-start text-gray-200 font-light gap-2 rounded-xl bg-white/10 px-3 py-5 transition hover:bg-white/20 active:scale-95 active:bg-white/10">
              <span>Planets and Sky</span>
            </button>
          </div>
        </div>

        <div className="w-full flex flex-col gap-4 overflow-y-auto pl-5 pb-28 pr-2.5 relative">
          <button className="w-full flex flex-col gap-4 rounded-xl ring-1 ring-white/10 bg-white/10 px-3 py-6 text-center text-sm transition hover:bg-white/20 active:scale-95 active:bg-white/10 relative">
            <img
              src="./planet.png"
              className="absolute top-2 right-2 w-24"
            />
            <p className="max-w-[60%] text-start text-white text-2xl">
              Proxima Centauri b
            </p>

            <span className="ring-1 ring-white/10 text-gray-300 rounded-full px-2 py-1">
              9.3 Light-years
            </span>
          </button>

          <button className="w-full flex flex-col gap-4 rounded-xl ring-1 ring-white/10 bg-white/10 px-3 py-6 text-center text-sm transition hover:bg-white/20 active:scale-95 active:bg-white/10 relative">
            <img
              src="./public/image-removebg-preview.png"
              className="absolute -top-2 -right-2 w-32"
            />
            <p className="max-w-[60%] text-start text-white text-2xl">
              TRAPPIST-1e
            </p>

            <span className="ring-1 ring-white/10 text-gray-300 rounded-full px-2 py-1">
              7 Light-years
            </span>
          </button>

          <button className="w-full flex flex-col gap-4 rounded-xl ring-1 ring-white/10 bg-white/10 px-3 py-6 text-center text-sm transition hover:bg-white/20 active:scale-95 active:bg-white/10 relative">
            <img
              src="./public/image-removebg-preview (1).png"
              className="absolute top-2 right-2 w-28"
            />
            <p className="max-w-[60%] text-start text-white text-2xl">
              LHS 1140 b
            </p>

            <span className="ring-1 ring-white/10 text-gray-300 rounded-full px-2 py-1">
              4 Light-years
            </span>
          </button>
        </div>

        <div className="pointer-events-none absolute inset-x-0 bottom-0 w-full h-1/3 bg-gradient-to-t from-current hidden sm:flex"></div>
      </div>
    </div>
  );
}
