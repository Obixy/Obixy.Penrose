import { QueryClient } from "@tanstack/react-query";

import axios from "axios";

const api = axios.create({
  baseURL:
    "https://nsac-obixy-penrose-data-auefcgedgjhyanbw.canadacentral-01.azurewebsites.net",
});

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: true,
      staleTime: 1000 * 30,
    },
  },
});

export { api };
