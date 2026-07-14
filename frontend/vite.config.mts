import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import checker from "vite-plugin-checker";
import tsconfigPaths from "vite-tsconfig-paths";

export default defineConfig({
  plugins: [
    react(),
    tsconfigPaths(),
    //   svgrPlugin(),
    //   handlebars({
    //     partialDirectory: resolve(__dirname, 'src/partials'),
    //   }) as Plugin,
  ],
  server: {
    port: 3000,
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: "./tests/setup.js",
    server: {
      deps: {
        inline: ["ag-grid-react"],
      },
    },
    reporters: ["html", "junit"],
    outputFile: "./output/test/junit.xml",
    coverage: {
      provider: "v8",
      reporter: ["html", "cobertura", "text"],
      reportsDirectory: "./output/coverage",
    },
  },
});
