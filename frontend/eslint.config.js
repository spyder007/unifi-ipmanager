// @ts-check
const eslint = require('@eslint/js');
const tseslint = require('typescript-eslint');
const eslintConfigPrettier = require('eslint-config-prettier');
//import hooksPlugin from "eslint-plugin-react-hooks";

module.exports = tseslint.config(
  {
    files: ['**/*.ts', '**/*.tsx'],
    ignores: [
      '**/build/**/*',
      '**/dist/**/*',
      '**/lib/**/*',
      '**/output/**/*',
      '**/coverage/**/*',
    ],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.recommended,
      ...tseslint.configs.stylistic,
    ],
    // plugins: [{
    // 	"react-hooks": hooksPlugin,
    // }],
    rules: {
      '@typescript-eslint/no-inferrable-types': 'off',
      '@typescript-eslint/class-literal-property-style': ['error', 'getters'],
      //hooksPlugin.configs.recommended.rules,
    },
  },
  {
    files: ['**/*.html'],
    rules: {
      // our project thinks using negated async pipes is ok
      //'@angular-eslint/template/no-negated-async': 'off',
    },
  },
  eslintConfigPrettier,
);
