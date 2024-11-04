<template>
    <div class="min-h-screen bg-gray-50 flex flex-col">
        <header class="bg-white shadow-md">
            <div class="container mx-auto p-4">
                <h1 class="text-3xl font-bold text-center text-gray-800">话术助手</h1>
            </div>
        </header>
        <nav class="bg-blue-600 text-white shadow-md">
            <div class="container mx-auto">
                <ul class="flex justify-center space-x-8 py-4">
                    <li>
                        <NuxtLink to="/" class="py-2 px-4" :class="{ 'border-b-2 border-white': $route.path === '/' }"
                            bug="1">随机话术</NuxtLink>
                    </li>
                    <li>
                        <NuxtLink to="/list?type=1&output=0" class="py-2 px-4"
                            :class="{ 'border-b-2 border-white': $route.path === '/list' }">话术浏览</NuxtLink>
                    </li>
                    <li>
                        <NuxtLink to="/settings" class="py-2 px-4"
                            :class="{ 'border-b-2 border-white': $route.path === '/settings' }">设置</NuxtLink>
                    </li>
                </ul>
            </div>
        </nav>
        <main class="flex-grow container mx-auto p-4 flex justify-center">
            <div class="w-full max-w-3xl bg-white shadow-md rounded-lg p-6">
                <slot />
            </div>
        </main>
        <footer class="bg-gray-800 text-white text-center py-4">
            <p>&copy; 2024 话术助手. All Rights Reserved.</p>
        </footer>
    </div>
</template>

<script setup>
</script>

<style scoped>
</style>