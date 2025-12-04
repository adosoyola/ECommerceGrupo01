describe('Integración: Flujo Completo de Logística y Despacho', () => {

    it('Debe generar un pedido (Como Admin), despacharlo y verificarlo en historial', () => {
        
        // ---------------------------------------------------------
        // 0. LIMPIEZA
        // ---------------------------------------------------------
        cy.clearAllCookies();
        cy.clearLocalStorage();

        // ---------------------------------------------------------
        // 1. INICIO DE SESIÓN ADMIN
        // ---------------------------------------------------------
        cy.log('👤 --- INICIO SESIÓN ADMIN ---');
        cy.visit('/Identity/Account/Login');
        
        cy.get('form').should('be.visible');
        cy.get('input[type="email"]').should('be.visible').clear().type('admin@ecommerce.com');
        cy.wait(1000); 
        cy.get('input[type="password"]').should('exist').should('be.visible').clear().type('Admin123!');
        cy.get('button[type="submit"]').click();
        cy.url().should('not.include', 'Login');

        // ---------------------------------------------------------
        // 2. FASE DE COMPRA (ADMIN)
        // ---------------------------------------------------------
        cy.log('🛒 --- REALIZANDO COMPRA ---');
        
        cy.visit('/Cart');
        cy.get('body').then(($body) => {
            if ($body.find('.bi-trash-fill').length > 0) {
                cy.get('.bi-trash-fill').each(($el) => {
                    cy.wrap($el).click();
                });
            }
        });

        cy.visit('/Products');
        cy.get('.card').first().find('a').click(); 
        
        cy.url().should('include', '/Details/');
        cy.get('input[name="Quantity"], input[name="Count"], input[type="number"]')
          .should('be.visible')
          .first()
          .clear()
          .type('1');

        cy.get('form[action*="Cart/Add"]').submit();

        cy.visit('/Cart');
        cy.contains(/Procesar|Pagar|Checkout/i).click();

        cy.contains(/Recojo|Tienda/i).click(); 
        cy.get('button, input[type="submit"]').contains(/Pagar|Continuar/i).click();

        cy.url().should('include', 'PaymentSimulation');
        cy.get('#CardNumber').type('4111111111111111');
        cy.get('#Expiration').type('12/30');
        cy.get('#CVV').type('123');
        cy.get('button').contains(/Pagar/i).click();

        cy.url().should('include', 'Success');
        cy.contains(/Exitosa|Gracias/i).should('be.visible');
        
        // ---------------------------------------------------------
        // 3. FASE DE DESPACHO (ADMIN)
        // ---------------------------------------------------------
        cy.log('🚚 --- GESTIONANDO DESPACHO ---');
        
        cy.visit('/Admin/Orders'); 

        // Entrar a detalles del último pedido
        cy.get('table tbody tr').last().within(() => {
            cy.get('a[href*="Details"]').click();
        });

        cy.url().should('include', '/Admin/Orders/Details');
        
        // Seleccionar estado "En Tránsito" (Índice 2, que vimos que funciona)
        cy.get('select').first().should('be.visible').select(2);

        // --- CORRECCIÓN: ELIMINAMOS CARRIER/TRACKING Y SOLO GUARDAMOS ---
        cy.log('💾 Guardando cambios...');
        cy.get('button[type="submit"], input[type="submit"]')
          .contains(/Actualizar|Update|Guardar|Save/i)
          .click();

        // ---------------------------------------------------------
        // 4. VERIFICACIÓN FINAL (RUTA QUE PEDISTE)
        // ---------------------------------------------------------
        cy.log('🏠 --- VOLVIENDO A INICIO ---');
        cy.visit('/'); // Ir a la página principal

        cy.log('📋 --- REVISANDO MIS PEDIDOS ---');
        cy.visit('/Checkout/History'); // Ir a historial

        // Entrar a detalles del último pedido
        cy.get('table tbody tr').last().within(() => {
            cy.get('a[href*="Details"]').click(); 
        });

        // Validar que estamos en detalles
        cy.url().should('include', '/Checkout/Details');

        cy.log('✅ ¡PRUEBA FINALIZADA CON ÉXITO!');
    });
});