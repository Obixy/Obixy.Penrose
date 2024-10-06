import { ConstellationProps } from "@/types";
import { api } from "..";

export async function create(params: ConstellationProps): Promise<void> {
  await api.post(`/entity/`, params);
}
