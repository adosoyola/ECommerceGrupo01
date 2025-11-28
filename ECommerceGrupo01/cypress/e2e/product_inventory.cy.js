describe('Visualización e Interacción de Productos', () => {
  const BASE_URL = 'http://localhost:5012';

  it('1. Debe cargar la página y verificar los datos y la existencia del botón Agregar', () => {
      // Paso 1: Navega a la página del listado de productos
      cy.visit(`${BASE_URL}/Products/Index`);
      
      // Verificación 1: Asegurarse de que la URL es la correcta
      cy.url().should('eq', `${BASE_URL}/Products/Index`);
      
      // Verificación 2: Validar que se muestra al menos un producto (Robustez)
      cy.get('.card').should('have.length.at.least', 1);

      // Verificación 3: Dentro del primer producto, verificar los detalles
      cy.get('.card').first().within(() => {
          // Confirma que la imagen del producto existe y está visible
          cy.get('img').should('be.visible').and('have.attr', 'src').and('not.be.empty');
          
          // Confirma que el título del producto no está vacío
          cy.get('.card-title').should('not.be.empty');
          
          // 🚨 CORRECCIÓN FINAL APLICADA: 
          // 1. Invocamos el texto del elemento.
          // 2. Usamos .then() para trabajar con el texto extraído.
          // 3. Aplicamos .trim() para eliminar cualquier espacio en blanco al inicio o al final.
          // 4. Utilizamos una Regex flexible que busca un patrón numérico con símbolos opcionales.
          cy.get('.card-text')
              .should('not.be.empty')
              .invoke('text')
              .then(text => {
                  const trimmedText = text.trim();
                  // Regex flexible: Busca un número con símbolos opcionales y decimales (punto o coma)
                  expect(trimmedText).to.match(/[\$\€\£A-Z]?\s*\d{1,}(?:[.,]\d{1,})?\s*[\$\€\£A-Z]?/i);
              });
          
          // Confirma que el botón de "Agregar" existe y es un botón de envío
          cy.get('button[type="submit"]').should('contain', 'Agregar').and('be.enabled');
      });
  });

  it('2. Debe navegar correctamente a la página de Detalles de un producto', () => {
      cy.visit(`${BASE_URL}/Products/Index`);
      
      // Encontramos la primera card
      cy.get('.card').first().within(() => {
          // 🚨 CORRECCIÓN CLAVE: Usamos un selector más flexible que busca cualquier enlace (a) o botón (button)
          // que contenga la palabra "Detalles" o que sea un enlace (<a>) cuyo atributo href
          // contenga la ruta '/Products/Details/'.
          cy.get('a:contains("Detalles"), button:contains("Detalles"), a[href*="/Products/Details/"]').first().click();
      });

      // Verificación: Aseguramos que la URL ha cambiado a la página de Detalles
      cy.url().should('include', '/Products/Details/');
      
      // Verificación: Aseguramos que el formulario de Agregar al Carrito está visible
      cy.get('form[action="/Cart/Add"]').should('be.visible');
      
      cy.log('Navegación a Detalles verificada con éxito.');
  });

  it('3. Prueba de funcionalidad: Agregar un producto al carrito (Envío de formulario)', () => {
      // Ejecutamos la prueba de navegación primero para estar en la página de Detalles
      cy.visit(`${BASE_URL}/Products/Index`);
      
      // 🚨 USAMOS EL SELECTOR CORREGIDO para llegar a la página de detalles
      cy.get('.card').first().find('a:contains("Detalles"), button:contains("Detalles"), a[href*="/Products/Details/"]').first().click();
      cy.url().should('include', '/Products/Details/');

      // 🚨 MEJORA: Ejecutamos la acción de agregar al carrito
      cy.get('form[action="/Cart/Add"]').within(() => {
          // Si tienes un campo de cantidad (quantity), asegúrate de que tiene un valor válido (ej. 1)
          // cy.get('#Quantity').clear().type('1'); 
          cy.get('button[type="submit"]').click();
      });

      // Verificación: Después de agregar, la aplicación debe redirigir al carrito
      // (Usamos la corrección que ya definimos: '/Cart')
      cy.url().should('include', '/Cart');
      
      // Verificación: Aseguramos que hay al menos un ítem en el carrito
      cy.get('.cart-items-list, tbody tr').should('have.length.at.least', 1);
      
      cy.log('Producto agregado al carrito exitosamente.');
  });
});