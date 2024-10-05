import { Sidebar, OptionsBar } from "../components";

export function View() {
  return (
    <div className="relative">
      <Sidebar />

      <iframe
        src="https://nsac-obixy-penrose-f8bygpb0bmavcxez.brazilsouth-01.azurewebsites.net/"
        className="w-full h-screen object-cover"
        autoFocus
      ></iframe>

      <OptionsBar onUnitChange={(unit: number) => console.log(unit)} />
    </div>
  );
}
