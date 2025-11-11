// view_products.cy.js

describe('Visualización de Productos', () => {
  it('1. Debe cargar la página de productos y mostrar al menos un producto', () => {
    // Paso 1: Navega a la página del listado de productos
    cy.visit('http://localhost:5012/Products/Index');

    // Verificación 1: Asegurarse de que la URL es la correcta
    cy.url().should('eq', 'http://localhost:5012/Products/Index');

    // Verificación 2: Validar que se muestra al menos un producto
    // Buscamos un contenedor de producto, que según tu código es una "card".
    cy.get('.card').should('have.length.at.least', 1);

    // Verificación 3: Dentro del primer producto, verificar que tiene un título, una imagen y un precio
    // Basado en la estructura de tu Index.cshtml, estos son los selectores correctos.
    cy.get('.card').first().within(() => {
      // Confirma que la imagen del producto existe
      cy.get('img').should('be.visible');
      // Confirma que el título del producto no está vacío
      cy.get('.card-title').should('not.be.empty');
      // Confirma que el precio del producto no está vacío
      cy.get('.card-text').should('not.be.empty');
      // Confirma que el botón de "Agregar" existe
      cy.get('button[type="submit"]').should('contain', 'Agregar');
    });
  });
});


/*
// visual_error_test.cy.js

describe('Pruebas de Errores Visuales', () => {

  it('1. Debe mostrar la página de error al intentar ver un producto inexistente', () => {
    // Definimos un ID que sabemos que no existe en tu base de datos.
    const nonExistentProductId = 999999;

    // Paso 1: Intentamos visitar la URL del producto inexistente.
    // Usamos { failOnStatusCode: false } para que Cypress no falle automáticamente
    // cuando reciba la respuesta 404 del servidor.
    cy.visit(`/Products/Details/${nonExistentProductId}`, { failOnStatusCode: false });

    // Verificación 1: Aseguramos que la URL redirige a la página de error o muestra la ruta del producto.
    // Esto demuestra que la solicitud fue enviada correctamente.
    cy.url().should('include', `/Products/Details/${nonExistentProductId}`);

    // Verificación 2: Buscamos un texto de la página de error.
    // Según tu archivo Error.cshtml, el título es "Error.".
    cy.contains('h1', 'Error.').should('be.visible');

    // Opcional: Busca el mensaje de error para más seguridad
    cy.contains('h2', 'An error occurred while processing your request.').should('be.visible');
  });
});
*/