/** @type {import('stylelint').Config} */
export default {
  extends: ["stylelint-config-standard"],
  ignoreFiles: [
    "refs/**",
    "node_modules/**",
    "**/bin/**",
    "**/obj/**",
    "artifacts/**",
    "test-results/**"
  ]
};
