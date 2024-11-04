import { ofetch } from "ofetch";

type ResponseBody<T> = {
  data: T;
  status: string;
  code: number;
  message: string;
};

/**
 *
 *  { "status": "success", "code": 200, "message": "OK", "data": 1, "total": 14 }, "timestamp": "2024-06-29 13:08:59" }
 *
 *
 */

const fetchApi = ofetch.create({
  baseURL: import.meta.env.VITE_BASE_URL,
  onResponse(_ctx) {
    const res: ResponseBody<any> = _ctx.response._data;
    if (res.status !== "success") throw res;
    _ctx.response._data = res.data;
  },
  onRequest: (_ctx) => {
    const headers: Record<string, string> = {};
    _ctx.options.headers = headers;
  },
});

export default fetchApi;
