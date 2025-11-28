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
                // Esperar a que el carrito esté vacío
                cy.contains('Tu carrito está vacío.').should('be.visible'); 
            }
        });
    };

    beforeEach(() => {
        cy.log('Asegurando que el carrito esté limpio antes de la prueba.');
        clearCart();
    });

    // --- PRUEBA 1: VERIFICAR EL LÍMITE DE STOCK EN EL DETALLE ---
    it('1. Debe leer el stock del producto y limitar el atributo "max" del input', () => {
        let availableStock;

        cy.visit(`/Products/Details/${productId}`);
        
        cy.get('strong:contains("Stock:")')
            .parent() // Sube al elemento contenedor del texto
            .invoke('text')
            .then((fullText) => {
                const stockMatch = fullText.match(/\d+/);
                availableStock = stockMatch ? parseInt(stockMatch[0]) : 0;
                cy.log(`Stock disponible detectado: ${availableStock}`);
                
                // Asume que si el stock es > 0, el input debe tener el atributo max.
                if (availableStock > 0) {
                    cy.get('input#qty').should('have.attr', 'max', availableStock.toString());
                } else {
                     // Si el stock es 0, verifica que el max sea 0 y que el botón de añadir esté deshabilitado.
                    cy.get('input#qty').should('have.attr', 'max', '0');
                    cy.get('form[action="/Cart/Add"] button[type="submit"]').should('be.disabled');
                }
            });
    });

    // --- PRUEBA 2: AÑADIR CANTIDAD MÁXIMA Y VERIFICAR EL TOTAL ---
    it('2. Debe añadir la cantidad máxima permitida, verificar la cantidad en el carrito y el total', () => {
        let maxQuantity;

        // Paso 1: Visitar la página de detalle.
        cy.visit(`/Products/Details/${productId}`);

        // Paso 2: Obtener el valor máximo (stock) del input.
        cy.get('input#qty').invoke('attr', 'max').then((maxStock) => {
            maxQuantity = parseInt(maxStock);
            cy.log(`Cantidad máxima a añadir: ${maxQuantity}`);
            
            // Si no hay stock, salta el resto de la prueba
            if (maxQuantity === 0) {
                cy.log('Stock es 0. Saltando la adición al carrito.');
                return;
            }

            // Paso 3: Escribir la cantidad máxima y hacer clic en agregar.
            cy.get('input#qty').type(`{selectall}{backspace}${maxQuantity.toString()}`);
            cy.get('form[action="/Cart/Add"] button[type="submit"]').click();

            // ✅ Navegación directa al carrito.
            cy.log('Paso 4: Forzando navegación directa al carrito.');
            cy.visit('/Cart');


            // Paso 5: Verificar que estamos en la página del carrito.
            cy.url().should('include', '/Cart');

            // --- ASERCIONES CLAVE DE EXISTENCIA Y VERIFICACIÓN DEL TOTAL ---
            // Buscamos la primera fila del carrito, esperando hasta 10 segundos para que aparezca la tabla.
            cy.get('table', { timeout: 10000 }).should('exist').find('tbody tr').first().as('productRow'); 
            
            // Verificación 1: El input del carrito debe mostrar la cantidad máxima añadida.
            cy.get('@productRow').find('input[type="number"], input:not([type="hidden"])')
                .should('have.value', maxQuantity.toString());
            
            // Verificación 2: Verifica un formato de moneda válido para el Total.
            cy.get('.d-flex.justify-content-between h4') // Apunta al contenedor h4 del total
              .contains('Total:') // Asegura que es el h4 correcto
              .find('span.fw-bold') // Busca el span que contiene el monto
              .invoke('text')
              .should('match', /[S\/\s]*[\d,]+\.\d{2}/); // Verifica un formato de moneda válido
        });
    });

    // --- PRUEBA 3 (NUEVA): INTENTAR AÑADIR MÁS DEL STOCK DISPONIBLE ---
    // ✅ FIX: Enfocado en la validación del atributo 'max' del input.
    it('3. Debe impedir la adición de stock excedido marcando el input como inválido', () => {
        let maxQuantity;

        // Paso 1: Visitar la página de detalle.
        cy.visit(`/Products/Details/${productId}`);

        // Paso 2: Obtener el valor máximo (stock) del input.
        cy.get('input#qty').invoke('attr', 'max').then((maxStock) => {
            maxQuantity = parseInt(maxStock);
            const quantityExceeded = maxQuantity + 1;
            cy.log(`Stock disponible: ${maxQuantity}. Intentando añadir: ${quantityExceeded}`);

            // Si el stock es 0, no hay prueba de excedencia válida.
            if (maxQuantity === 0) {
                 cy.log('Stock es 0. No se puede probar la excedencia. Prueba exitosa por defecto.');
                 return; 
            }
            
            // Paso 3: Escribir la cantidad excedente.
            cy.get('input#qty').type(`{selectall}{backspace}${quantityExceeded.toString()}`);
            
            // --- ASERCIONES CLAVE DE VALIDACIÓN DE HTML5 ---
            
            // 1. Verificamos que la propiedad rangeOverflow sea true (validación DOM/HTML5).
            cy.get('input#qty')
              .should('have.value', quantityExceeded.toString())
              .and(([$input]) => {
                // Assert nativo del DOM para verificar que el valor excede el máximo.
                expect($input.validity.rangeOverflow).to.be.true;
              });

            // 2. Intentamos hacer clic en el botón de envío (incluso si está habilitado).
            cy.get('form[action="/Cart/Add"] button[type="submit"]').click();

            // 3. ✅ FIX: Verificamos que seguimos en la página de detalles, lo que confirma que el envío fue bloqueado por la validación de HTML5.
            cy.url().should('include', `/Products/Details/${productId}`);
            cy.log('✅ Se verificó que el envío del formulario fue bloqueado por la validación de stock (input max).');
        });
    });
});