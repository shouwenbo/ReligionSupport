import type { LocationQueryValue } from "vue-router";

type Phrase = {
  title: string;
  content: string;
};

type PhraseList = {
  list: Phrase[];
  total: number;
};

export const useQueryPhraseList = (body: Ref<any | null>) => {
  const route = useRoute();
  const page = ref(1);
  const pageSize = ref(5);
  const typeRef = ref(route.query.type?.toString() ?? body.value?.type?.value);
  const outputTypeRef = ref(route.query.output?.toString() ?? body.value?.outputType?.value);
  const searchRef = ref(
    route.query.search?.toString() ?? body.value?.search?.value
  );
  const type = computed(() => typeRef.value);
  const outputType = computed(() => outputTypeRef.value);
  const search = computed(() => searchRef.value);

  watch([typeRef, searchRef], () => {
    page.value = 1;
  });

  const { data, pending, refresh } = useFetchApi<PhraseList>(
    "/phrase/QueryPhrasesList",
    {
      method: "POST",
      body: {
        type,
        outputType,
        search,
        page,
        pageSize,
      },
    }
  );

  return {
    data,
    pending,
    refresh,
    page,
    pageSize,
    typeRef,
    outputTypeRef,
    searchRef,
  };
};

export function getRandomPhrase(body: Ref<any | null>) {
  return fetchApi("/phrase/GetRandomPhrase", {
    method: "post",
    body
  });
}
