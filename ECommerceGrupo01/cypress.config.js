const { defineConfig } = require("cypress");

module.exports = defineConfig({
  e2e: {
    baseUrl: 'http://localhost:5012',
    setupNodeEvents(on, config) {
      // implementa los eventos de los nodos aquí
    },
    // Esto le dice a Cypress que busque en toda la carpeta e2e
    specPattern: 'cypress/e2e/**/*.cy.{js,jsx,ts,tsx}',
  },
});