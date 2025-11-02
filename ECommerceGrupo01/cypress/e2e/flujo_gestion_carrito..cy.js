// cart_management_flow.cy.js

describe('Flujo de Gestión del Carrito (Añadir, Actualizar, Eliminar)', () => {
    
    // Función para limpiar el carrito antes de la prueba, asegurando un estado limpio
    beforeEach(() => {
        // Asumimos que la URL base es http://localhost:5012/
        cy.visit('/Cart');
        // Usamos la acción Update con cantidad 0 para eliminar todos los items (si existen)
        cy.get('body').then(($body) => {
            if ($body.find('table.table-striped').length) {
                // Si la tabla existe, encontramos todos los botones de "Eliminar" y hacemos click.
                // Esto podría fallar si el carrito tiene muchos items, pero es un buen intento.
                // La forma más robusta sería usar cy.clearLocalStorage() o cy.clearCookies() si el carrito está ahí.
                // Por ahora, usamos una limpieza de alto nivel:
                cy.log('Intentando limpiar el carrito antes de iniciar la prueba.');
                cy.get('form button:contains("Eliminar")').each(($btn) => {
                    cy.wrap($btn).click({ force: true });
                });
            }
        });
    });

    it('1. Debe añadir un producto, actualizar la cantidad y eliminarlo con éxito', () => {
        const initialQuantity = 1;
        const updatedQuantity = 3;

        // 1. AÑADIR PRODUCTO (Simulamos clic en el botón 'Agregar' de la página de Index)
        cy.visit('/Products/Index');
        cy.get('.card').first().within(() => {
            cy.get('button[type="submit"]').click(); 
        });

        // Verificación 1: El carrito debe tener 1 item
        cy.url().should('include', '/Cart');
        cy.get('table tbody tr').should('have.length', 1); 
        cy.get('input[name="qty"]').should('have.value', initialQuantity);

        // 2. ACTUALIZAR CANTIDAD
        cy.get('form[action="/Cart/Update"]').first().within(() => {
            // Escribir nueva cantidad
            cy.get('input[name="qty"]').clear().type(updatedQuantity);
            cy.get('button[type="submit"]:contains("Actualizar")').click();
        });

        // Verificación 2: La cantidad debe ser 3 y el total debe reflejarlo
        cy.url().should('include', '/Cart'); // Sigue en la página del carrito
        cy.get('input[name="qty"]').should('have.value', updatedQuantity);
        cy.contains('Cantidad').parent().next().should('not.be.empty'); // Verifica que el total se recalculó

        // 3. ELIMINAR PRODUCTO (Usando la acción de eliminar con qty=0)
        cy.get('form button:contains("Eliminar")').click();
        
        // Verificación 3: El carrito debe estar vacío
        cy.contains('Tu carrito está vacío.').should('be.visible');
        cy.get('table').should('not.exist');
    });
});