<template>
  <div class="space-y-6">
    <div class="flex justify-between space-x-4">
      <div class="w-1/3">
        <label for="type" class="block text-sm font-medium text-gray-700">选择话术类型</label>
        <select id="type" v-model="typeRef"
          @change="(event) => { $router.push({ query: { ...$route.query, ...{ type: event.target.value } } }); }"
          class="mt-1 block w-full py-2 px-3 border border-gray-300 bg-white rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm">
          <option value="1">短话术</option>
          <option value="2">长话术</option>
          <option value="3">招生话术</option>
        </select>
      </div>

      <div class="w-2/3">
        <label for="search" class="block text-sm font-medium text-gray-700">搜索话术内容</label>
        <input id="search" v-model="searchRef" type="text"
          class="mt-1 block w-full py-2 px-3 border border-gray-300 bg-white rounded-md shadow-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm">
      </div>
    </div>
--
{{outputType}}
    <div class="flex items-center space-x-4 mt-4">
      <label class="flex items-center space-x-1">
        <input type="radio" v-model="outputTypeRef" :value="0" @change="(event) => { $router.push({ query: { ...$route.query, ...{ output: event.target.value } } }); }" class="form-radio h-4 w-4 text-blue-600">
        <span class="text-md font-medium">原文输出</span>
      </label>
      <label class="flex items-center space-x-1">
        <input type="radio" v-model="outputTypeRef" :value="1" @change="(event) => { $router.push({ query: { ...$route.query, ...{ output: event.target.value } } }); }" class="form-radio h-4 w-4 text-blue-600">
        <span class="text-md font-medium">轻度替换</span>
      </label>
      <label class="flex items-center space-x-1">
        <input type="radio" v-model="outputTypeRef" :value="2" @change="(event) => { $router.push({ query: { ...$route.query, ...{ output: event.target.value } } }); }" class="form-radio h-4 w-4 text-blue-600">
        <span class="text-md font-medium">重度替换</span>
      </label>
    </div>

    <div v-if="phraseRes" class="space-y-2 mt-4">
      <ul class="divide-y divide-gray-200">
        <li v-for="(phrase, index) in phraseRes.list" :key="index" @click="selectPhrase(phrase)"
          class="cursor-pointer py-2 hover:bg-blue-50 transition duration-150 ease-in-out">
          <span class="text-blue-600 hover:text-blue-800">{{ phrase.title }}</span>
        </li>
      </ul>
    </div>

    <div class="flex items-center space-x-2 mt-4">
      <button v-if="currentPhrase" @click="copyPhrase" data-clipboard-target="#copy"
        class="bg-gray-500 btn hover:bg-gray-700 text-white font-bold py-2 px-4 rounded shadow ml-2">
        复制
      </button>
      <div v-if="copySuccess" class="text-sm text-red-500 ml-2">
        复制成功
      </div>
    </div>

    <div v-if="currentPhrase" class="bg-gray-100 p-4 rounded shadow mt-4 phrase-container">
      <div class="phrase-content" id="copy">{{ currentPhrase.content }}</div>
    </div>

    <div class="flex justify-between items-center mt-4">
      <div class="pagination-container">
        <button @click="goToPage(page - 1)" :disabled="page === 1" class="pagination-btn prev-btn">上一页</button>
        <span class="page-info" v-if="totalPages && totalPages > 0">第 {{ page }} 页，共 {{ totalPages }} 页</span>
        <button @click="goToPage(page + 1)" :disabled="page >= totalPages" class="pagination-btn next-btn">下一页</button>
      </div>
      <div>
        <span class="text-gray-700">共 {{ phraseRes?.total }} 条</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed } from 'vue';
  import { useRoute } from 'vue-router';
  import ClipboardJS from 'clipboard';

  const route = useRoute();

  const currentPhrase = ref(null);
  const type = route.params.type as string;
  const outputType = route.params.outputType as number;
  const search = route.params.search as string;

  const { data: phraseRes, refresh: refreshPhrase, page, pageSize, typeRef, outputTypeRef, searchRef } = useQueryPhraseList({ type, search });

  const selectPhrase = (phrase) => {
    currentPhrase.value = phrase;
  };

  const copySuccess = ref(false);
  const copyPhrase = () => {
    if (currentPhrase.value) {
      var clipboard = new ClipboardJS('.btn', {
        text: () => currentPhrase.value.content,
      });
      clipboard.on('success', function (e) {
        console.log(e, 'success copy');
        copySuccess.value = true;
        setTimeout(() => {
          copySuccess.value = false;
        }, 3000);
      });
      clipboard.on('error', function (e) {
        console.log(e, 'error copy');
      });
    }
  };

  const totalPages = computed(() => phraseRes.value ? Math.ceil(phraseRes.value.total / 5) : 0);

  const goToPage = (pageIndex) => {
    if (pageIndex >= 1 && pageIndex <= totalPages.value) {
      page.value = pageIndex;
    }
  };
</script>

<style scoped>
  .pagination-container {
    display: flex;
    align-items: center;
  }

  .pagination-btn {
    background-color: #e5e7eb;
    color: #4b5563;
    font-weight: bold;
    padding: 0.5rem;
    border-radius: 0.375rem;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    transition: background-color 0.3s;
    margin: 0 0.5rem;
  }

  .pagination-btn:hover {
    background-color: #d1d5db;
  }

  .pagination-btn:disabled {
    cursor: not-allowed;
    background-color: #e5e7eb;
  }

  .page-info {
    margin: 0 1rem;
    color: #6b7280;
    font-size: 0.875rem;
  }

  @media (max-width: 640px) {
    .page-info {
      display: none;
    }
  }

  .phrase-container {
    display: flex;
    flex-direction: column;
    align-items: stretch;
  }

  .phrase-content {
    word-wrap: break-word;
    white-space: pre-wrap;
  }

  .list-disc > li {
    padding: 0.5rem 0;
  }

  .list-disc > li:hover {
    background-color: #f3f4f6;
    border-radius: 0.375rem;
    padding-left: 1rem;
  }
</style>
