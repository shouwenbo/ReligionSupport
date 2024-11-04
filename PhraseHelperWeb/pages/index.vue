<template>
    <div class="space-y-4">
        <div class="flex items-center space-x-4">
            <label class="flex items-center space-x-1">
                <input type="checkbox" v-model="shortPhrase" class="form-checkbox h-4 w-4 text-blue-600">
                <span class="text-md font-medium">短话术</span>
            </label>
            <label class="flex items-center space-x-1">
                <input type="checkbox" v-model="longPhrase" class="form-checkbox h-4 w-4 text-blue-600">
                <span class="text-md font-medium">长话术</span>
            </label>
            <label class="flex items-center space-x-1">
                <input type="checkbox" v-model="registrationPhrase" class="form-checkbox h-4 w-4 text-blue-600">
                <span class="text-md font-medium">报名话术</span>
            </label>
        </div>
        <div class="flex items-center space-x-2 mt-4">
            <button @click="getRandomData"
                class="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded shadow">随机获取</button>
            <div class="flex items-center">
                <button @click="copyPhrase" data-clipboard-target="#copy"
                    :class="{'bg-gray-500 btn hover:bg-gray-700': !autoCopy, 'bg-gray-300 cursor-not-allowed': autoCopy}"
                    :disabled="autoCopy" class="text-white font-bold py-2 px-4 rounded shadow">
                    复制
                </button>
                <div v-if="copySuccess" class="text-sm text-red-500 ml-2">
                    复制成功
                </div>
            </div>
        </div>
        <div class="flex items-center space-x-2 mt-4">
            <label class="flex items-center space-x-1">
                <input type="radio" v-model="outputType" :value="0" class="form-radio h-4 w-4 text-blue-600">
                <span class="text-md font-medium">原文输出</span>
            </label>
            <label class="flex items-center space-x-1">
                <input type="radio" v-model="outputType" :value="1" class="form-radio h-4 w-4 text-blue-600">
                <span class="text-md font-medium">轻度替换</span>
            </label>
            <label class="flex items-center space-x-1">
                <input type="radio" v-model="outputType" :value="2" class="form-radio h-4 w-4 text-blue-600">
                <span class="text-md font-medium">重度替换</span>
            </label>
        </div>
        <div id="copy" v-if="currentPhrase" class="bg-gray-100 p-4 rounded shadow mt-4 whitespace-pre-wrap">
            {{ currentPhrase.content }}
        </div>
    </div>
</template>

<script setup>
    import { ref } from 'vue';
    import ClipboardJS from 'clipboard';

    const shortPhrase = ref(true);
    const longPhrase = ref(true);
    const registrationPhrase = ref(true);
    const autoCopy = ref(false);
    const currentPhrase = ref('');
    const copySuccess = ref(false);
    const outputType = ref(0); // 新增状态，用于单选框

    const getRandomData = async () => {
        let types = [];
        if (shortPhrase.value) types.push(1); // 短话术
        if (longPhrase.value) types.push(2); // 长话术
        if (registrationPhrase.value) types.push(3); // 报名话术

        // 假设后端API接受 types 和 outputType 作为参数
        getRandomPhrase({ types, outputType: outputType.value })
            .then(data => {
                currentPhrase.value = data; // 假设后端返回的数据中有 content 字段
                if (autoCopy.value) {
                    copyPhrase();
                }
            });
    };

    const copyPhrase = () => {
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
            console.log(e, 'err copy')
        });
    };
</script>

<style scoped>
    /* 可以根据需要添加样式 */
</style>
