import type { UseFetchOptions } from "nuxt/app";
import { defu } from "defu";
import fetchApi from "../utils/fetch-api";

export default function useFetchApi<T = any> (url: string, options?: UseFetchOptions<T>) {
  const defaults: UseFetchOptions<T> = {
    $fetch: fetchApi,
  };

  // for nice deep defaults, please use unjs/defu
  const params = defu(options, defaults);

  return useFetch(url, params);
}
