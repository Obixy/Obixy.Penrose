import { QueryClient } from "@tanstack/react-query";

import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:5001/api",
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
