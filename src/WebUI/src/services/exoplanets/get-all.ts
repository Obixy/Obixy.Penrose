import { ExoplanetProps } from "@/types";
import { api } from "..";

type ExoplanetResponse = Array<ExoplanetProps>;

export async function getAll(): Promise<ExoplanetResponse> {
  const { signal } = new AbortController();

  const { data } = await api.get<ExoplanetResponse>(`/exoplanets`, { signal });

  return data;
}
