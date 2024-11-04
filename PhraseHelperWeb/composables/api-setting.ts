export function settingReload() {
  return fetchApi("/setting/reload", {
    method: "get",
  });
}
