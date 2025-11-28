describe('Flujo de Carrito de Compras', () => {
  const BASE_URL = 'http://localhost:5012';

  it('1. Debe agregar, visualizar y luego eliminar un producto del carrito', () => {
      
      // --- 1. Agregar Producto desde la lista (Index) ---
      cy.log('Paso 1: Agregando un producto al carrito desde la página de Índice.');
      cy.visit(`${BASE_URL}/Products/Index`);
      cy.url().should('eq', `${BASE_URL}/Products/Index`);

      // Encontramos el primer producto y hacemos clic en su botón de "Agregar"
      cy.get('.card').first().within(() => {
          // Buscamos el formulario de adición y lo enviamos
          cy.get('form').submit();
      });
      
      // --- 2. Verificar que estamos en la página del Carrito ---
      // 🚨 CORRECCIÓN ANTERIOR: La ruta se corrigió a '/Cart'
      cy.url().should('include', '/Cart'); 
      
      // Verificamos que al menos un elemento está en el carrito
      cy.get('.cart-items-list, tbody tr').should('have.length.at.least', 1);
      
      // --- 3. Eliminar el Producto del Carrito ---
      cy.log('Paso 2: Eliminando el producto del carrito.');

      // Buscamos el botón de eliminar asociado a ese ítem y hacemos clic
      cy.get('.cart-items-list, tbody tr').first().within(() => {
          cy.get('form[action*="/Cart/Remove"] button[type="submit"], button:contains("Eliminar")').click();
      });
      
      // --- 4. Verificar que el carrito está vacío ---
      cy.url().should('include', '/Cart');

      // 🚨 MEJORA CLAVE 1: Verificamos primero que la lista de ítems haya desaparecido.
      cy.get('.cart-items-list, tbody tr').should('not.exist'); 
      
      // 🚨 MEJORA CLAVE 2: Ampliamos los selectores (h1, h2, h3) y la regex.
      // Buscamos el mensaje de carrito vacío, usando una expresión regular
      // que es insensible a mayúsculas/minúsculas y cubre más opciones de texto.
      cy.contains(
          'h1, h2, h3, h4, p, div', 
          /carrito.*vacío|empty cart|no hay productos|no tienes artículos/i, 
          { timeout: 5000 }
      ).should('be.visible');
      
      cy.log('✅ Prueba de Agregar/Eliminar del Carrito completada exitosamente.');
  });
});