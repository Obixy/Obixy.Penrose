import { ConstellationProps } from "@/types";
import { api } from "..";

type ConstellationResponse = Array<ConstellationProps>;

export async function getByJobId(id: string): Promise<ConstellationResponse> {
  const { signal } = new AbortController();

  const { data } = await api.get<ConstellationResponse>(
    `/exoplanets/${id}/constellation`,
    {
      signal,
    }
  );

  return data;
}
