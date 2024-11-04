<!-- Toast全局组件 toast.vue -->
<template>
    <div v-show="toast && title" class="toast-all">
        <span class="toast-all-title" v-html="title"></span>
    </div>
</template>

<script>
    export default {
        name: 'Toast',
        data() {
            return {
                title: '',
                toast: false
            }
        }
    }
</script>

<style lang="scss" scoped>
    .toast-all {
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        background: rgba(0, 0, 0, 0.8);
        border-radius: 8px;
        padding: 8px 12px;
        box-sizing: border-box;
        dispaly: flex;
        justify-content: center;
        align-items: center;
        z-index: 99;

        &-title {
            font-size: 14px;
            font-weight: 400;
            color: #ffffff;
            line-height: 20px;
        }
    }
</style>