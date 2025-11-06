// inventory_validation.cy.js

describe('Validación de Stock y Límite de Compra (FINAL FIX)', () => {
    
    // NOTA: Asegúrate de que el producto con ID 1 exista y tenga stock > 0
    const productId = 1; 

    // Helper para limpiar el carrito antes de cada test que lo necesite
    const clearCart = () => {
        cy.visit('/Cart');
        cy.get('body').then(($body) => {
            // Verifica si el carrito no está vacío buscando el botón 'Eliminar'
            if ($body.find('form button:contains("Eliminar")').length) {
                cy.log('Limpiando carrito antes de la prueba...');
                // Haz clic en el primer botón de eliminar
                cy.get('form button:contains("Eliminar")').first().click();
                cy.contains('Tu carrito está vacío.').should('be.visible');
            }
        });
    };

    // --- PRUEBA 1: VERIFICAR EL LÍMITE DE STOCK EN EL DETALLE (No se toca, ya funciona) ---

    it('1. Debe leer el stock del producto y limitar el atributo "max" del input', () => {
        let availableStock;

        cy.visit(`/Products/Details/${productId}`);
        
        cy.get('strong:contains("Stock:")')
            .parent() // Sube al elemento <p> que contiene todo el texto
            .invoke('text')
            .then((fullText) => {
                const stockMatch = fullText.match(/\d+/);
                availableStock = stockMatch ? parseInt(stockMatch[0]) : 0;
                cy.log(`Stock disponible detectado: ${availableStock}`);
                
                cy.get('input#qty').should('have.attr', 'max', availableStock.toString());
            });
    });

    // --- PRUEBA 2: AÑADIR CANTIDAD MÁXIMA Y VERIFICAR EL TOTAL ---
    
    it('2. Debe añadir la cantidad máxima permitida, verificar la cantidad en el carrito y el total', () => {
        
        // Limpiamos el carrito al inicio para asegurar un estado limpio
        clearCart();
        
        let maxQuantity;

        // Paso 1: Visitar la página de detalle.
        cy.visit(`/Products/Details/${productId}`);

        // Paso 2: Obtener el valor máximo (stock) del input.
        cy.get('input#qty').invoke('attr', 'max').then((maxStock) => {
            maxQuantity = parseInt(maxStock);
            cy.log(`Cantidad máxima a añadir: ${maxQuantity}`);
            
            // Paso 3: Escribir la cantidad máxima y hacer clic en agregar.
            cy.get('input#qty').type(`{selectall}{backspace}${maxQuantity.toString()}`);
            cy.get('form[action="/Cart/Add"] button[type="submit"]').click();

            // Paso 4: Ir a la página del carrito. (Ya redirige automáticamente)
            cy.url().should('include', '/Cart');

            // --- ASERCIONES CLAVE DE EXISTENCIA Y VERIFICACIÓN DEL TOTAL ---
            
            // Verificamos que la tabla de productos se ha renderizado
            cy.get('table.table-striped').should('exist');
            cy.get('table tbody tr').should('have.length', 1);

            // Verificación 1: El input del carrito debe mostrar la cantidad máxima añadida.
            cy.get('form input[name="qty"]').should('have.value', maxQuantity.toString());
            
            // ❌ LÍNEA CORREGIDA: Buscamos el total en la estructura del <h4>/div exterior.
            // Esto apunta al contenedor del Total: y extrae el monto del <span> fw-bold.
            cy.get('.d-flex.justify-content-between h4') // Apunta al contenedor h4 del total
              .contains('Total:') // Asegura que es el h4 correcto
              .find('span.fw-bold') // Busca el span que contiene el monto
              .invoke('text')
              .should('match', /[S\/\s]*[\d,]+\.\d{2}/); // Verifica un formato de moneda válido

            // Opcional: Limpiar después de la prueba
            clearCart(); 
        });
    });
});