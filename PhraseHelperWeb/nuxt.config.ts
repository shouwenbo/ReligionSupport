// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  devtools: { enabled: true },
  runtimeConfig: {
    public: {
      env: process.env.NUXT_PUBLIC_ENV,
      api: {
        baseUrl: process.env.NUXT_PUBLIC_API_BASEURL,
      },
    },
  },
  app: {
    head: {
      meta: [
        {
          name: "viewport",
          content:
            "width=device-width,initial-scale=1,minimum-scale=1,user-scalable=no",
        },
      ],
    },
  },
  css: ["~/assets/css/main.css"],
  plugins: [],
  postcss: {
    plugins: {
      tailwindcss: {},
      autoprefixer: {},
    },
  },
  vue: {
    compilerOptions: {
      whitespace: "preserve",
    },
  },
  render: {
    // Disable HTML minification
    minify: false,
  },
});
