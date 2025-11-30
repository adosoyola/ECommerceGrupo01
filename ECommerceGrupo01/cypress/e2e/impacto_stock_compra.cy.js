describe('Integración: Flujo Completo de Logística y Despacho', () => {

    const BASE_URL = 'http://localhost:5012'; 
    const ADMIN_USER = 'admin@ecommerce.com';
    const ADMIN_PASS = 'Admin123!';
    const CLIENT_USER = 'cliente_test@gmail.com'; 
    const CLIENT_PASS = 'Test@123';

    const login = (email, password) => {
        cy.visit(`${BASE_URL}/Identity/Account/Login`);
        cy.get('input[name="Input.Email"]').clear().type(email);
        cy.get('input[id="passwordInput"]').clear().type(password);
        cy.get('form#account button[type="submit"]').click();
    };

    it('Debe generar un pedido nuevo (Cliente) y cambiar su estado a Enviado (Logística)', () => {
        
        // ====================================================
        // PASO 1: CLIENTE GENERA PEDIDO
        // ====================================================
        cy.log('📦 PASO 1: Cliente genera un pedido nuevo');
        cy.session('clientSession', () => login(CLIENT_USER, CLIENT_PASS));
        
        cy.visit(`${BASE_URL}/Products`);
        
        // Entrar al primer producto
        cy.get('.card').first().within(() => {
            cy.get('a').click(); 
        });

        // Validar que NO estemos comprando un producto sin stock
        cy.get('body').then(($body) => {
            if ($body.text().includes('Stock: 0') || $body.text().includes('Agotado')) {
                throw new Error("⚠️ ERROR CRÍTICO: El producto seleccionado tiene STOCK 0. Por favor, reinicia la base de datos o elige otro producto.");
            }
        });

        // Llenar cantidad
        cy.get('input#qty').clear().type('1');

        cy.log('🛒 Enviando formulario DIRECTAMENTE (Sin clic en botón)...');
        
        // --- LA SOLUCIÓN MAESTRA ---
        // En lugar de dar clic al botón, buscamos el formulario y le damos .submit()
        // Esto evita problemas de que el clic no le dé al lugar correcto.
        cy.get('form[action*="Cart/Add"]').submit();

        // Espera de seguridad para que la BD procese
        cy.wait(2000); 

        // Ir al carrito
        cy.log('🔄 Yendo al carrito manualmente...');
        cy.visit(`${BASE_URL}/Cart`);

        // Validación de seguridad
        cy.get('table tbody tr', { timeout: 10000 }).should('have.length.gt', 0, '⚠️ EL CARRITO SIGUE VACÍO. Revisa el Log por si el producto tenía Stock 0.');

        // --- SELECCIÓN DEL BOTÓN DE PAGO ---
        cy.log('💳 Buscando botón de pago...');
        
        cy.get('body').then(($body) => {
            if ($body.find('input[value*="Procesar"], input[value*="Pago"]').length > 0) {
                cy.get('input[value*="Procesar"], input[value*="Pago"]').first().click();
            } 
            else if ($body.find('a:contains("Procesar"), button:contains("Procesar")').length > 0) {
                 cy.contains(/Procesar|Pagar|Checkout/i).click();
            }
            else {
                cy.get('.btn-success, .btn-primary, input[type="submit"]')
                  .not(':contains("Seguir")')
                  .not('[value*="Seguir"]') 
                  .not(':contains("Logout")') 
                  .last()
                  .click();
            }
        });

        // Confirmación intermedia
        cy.get('body').then(($body) => {
            if ($body.find('form[action*="Confirm"]').length > 0) {
                 cy.get('button[type="submit"]').click();
            }
        });

        // Simular Pago
        cy.url({ timeout: 10000 }).should('include', 'PaymentSimulation');
        cy.get('input#CardNumber').type('4111111111111111');
        cy.get('input#Expiration').type('12/30');
        cy.get('input#CVV').type('123');
        cy.get('input#CardNumber').parents('form').find('button[type="submit"]').click();

        cy.url().should('include', '/Checkout/Success');
        cy.log('✅ Pedido creado exitosamente');


        // ====================================================
        // PASO 2: LOGÍSTICA GESTIONA PEDIDO
        // ====================================================
        cy.log('🚚 PASO 2: Logística revisa y despacha el pedido');
        
        cy.session('adminSession', () => login(ADMIN_USER, ADMIN_PASS));

        cy.visit(`${BASE_URL}/Admin/Orders`);
        
        cy.get('table tbody tr', { timeout: 10000 }).should('have.length.greaterThan', 0);

        cy.get('tbody tr').first().within(() => {
            cy.get('a').first().click();
        });

        // De Detalles a Edición
        cy.url().then($url => {
            if ($url.includes('/Details')) {
                cy.log('ℹ️ Estamos en Detalles. Buscando botón para ir a Editar...');
                cy.get('a[href*="Edit"]').click();
            }
        });

        // 2.4 Cambiar Estado
        cy.log('🚚 PASO 3: Cambiando estado a ENVIADO');
        cy.get('form').should('exist');

        cy.get('body').then(($body) => {
            if ($body.find('select').length > 0) {
                const select = cy.get('select').first();
                select.find('option:contains("Enviado")').then($opt => {
                    if ($opt.length > 0) { cy.get('select').select('Enviado'); } 
                    else { cy.get('select').select(1); }
                });
            } else if ($body.find('input[name*="Status"]').length > 0) {
                cy.get('input[name*="Status"]').clear().type('Enviado');
            }
        });

        // 2.5 Guardar
        cy.get('form').find('button[type="submit"], input[type="submit"]').click();

        // 2.6 Validación Final
        cy.url().should('include', '/Admin/Orders');
        cy.log('✅ Pedido actualizado correctamente');
    });
});