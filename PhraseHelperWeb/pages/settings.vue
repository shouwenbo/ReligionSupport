<template>
  <div class="space-y-6">
    <button @click="updateResources"
      class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded shadow">更新资源</button>
  </div>
</template>

<script setup>
  const updateResources = () => {
    settingReload()
      .then(res => {
        alert('更新完成');
      });
  }
</script>