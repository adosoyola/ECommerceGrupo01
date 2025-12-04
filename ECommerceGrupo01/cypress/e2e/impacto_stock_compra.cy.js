// Archivo: cypress/e2e/logistica_despacho.cy.js

describe('Integración: Flujo Completo de Logística y Despacho', () => {

    const BASE_URL = 'http://localhost:5012';
    const ADMIN_USER = 'admin@ecommerce.com';
    const ADMIN_PASS = 'Admin123!';
    const CLIENT_USER = 'cliente_test@gmail.com';
    const CLIENT_PASS = 'Test@123';

    // --- Funciones Reutilizables ---

    // 1. Función para Login
    const login = (email, password) => {
        cy.visit(`${BASE_URL}/Identity/Account/Login`);
        cy.get('input[name="Input.Email"]').clear().type(email);
        cy.get('input[id="passwordInput"]').clear().type(password);
        cy.get('form#account button[type="submit"]').click();
    };

    // 2. Función para Limpiar Carrito (para garantizar un pedido limpio)
    const limpiarCarrito = () => {
        cy.visit(`${BASE_URL}/Cart`);
        cy.get('body').then($body => {
            if ($body.find('form[action*="Cart/Remove"]').length) {
                cy.get('form[action*="Cart/Remove"]').each(($form) => {
                    cy.wrap($form).submit();
                });
                cy.wait(1000);
            }
        });
        cy.contains('El carrito está vacío', { timeout: 10000 }).should('be.visible');
    };

    it('Debe generar un pedido nuevo (Cliente) y cambiar su estado a Enviado (Logística)', () => {

        // ====================================================
        // PASO 1: CLIENTE GENERA PEDIDO COMPLETO
        // ====================================================
        cy.log('📦 PASO 1: Cliente genera un pedido nuevo');
        cy.session('clientSession', () => login(CLIENT_USER, CLIENT_PASS));
        limpiarCarrito(); // Aseguramos que el pedido es independiente

        // 1.1 Agregar Producto
        cy.visit(`${BASE_URL}/Products`);
        cy.get('.card').first().within(() => { cy.get('a').click(); });

        cy.get('body').then(($body) => {
            if ($body.text().includes('Stock: 0') || $body.text().includes('Agotado')) {
                assert.fail("⚠️ ERROR CRÍTICO: El producto seleccionado tiene STOCK 0.");
            }
        });

        cy.get('input#qty').clear().type('1');
        cy.get('form[action*="Cart/Add"]').submit();
        cy.wait(1500);

        // 1.2 Ir a Checkout
        cy.visit(`${BASE_URL}/Cart`);
        cy.contains('a, button, input[type="submit"]', /Procesar|Pagar|Checkout/i).click();

        // 1.3 Pantalla de Envío: Seleccionar Recojo en Tienda (para simplificar)
        cy.log('🏪 Seleccionando Recojo en Tienda');
        cy.contains('label', /Recojo|Tienda/i).click();

        // Clic en "Ir a Pagar"
        cy.get('button[type="submit"], input[type="submit"]').contains(/Pagar|Continuar|Siguiente/i).click();

        // 1.4 Simular Pago
        cy.url({ timeout: 10000 }).should('include', 'PaymentSimulation');
        cy.get('input#CardNumber').type('4111111111111111');
        cy.get('input#Expiration').type('12/30');
        cy.get('input#CVV').type('123');
        cy.get('form').find('button[type="submit"], input[type="submit"]').click();

        // 1.5 Validación
        cy.url().should('include', '/Checkout/Success');
        cy.contains('Pedido exitoso', { timeout: 10000 }).should('be.visible');
        cy.log('✅ Pedido de Cliente creado exitosamente');


        // ====================================================
        // PASO 2: LOGÍSTICA GESTIONA PEDIDO
        // ====================================================
        cy.log('🚚 PASO 2: Logística revisa y despacha el pedido');

        // 2.1 Login de Admin
        cy.session('adminSession', () => login(ADMIN_USER, ADMIN_PASS));

        // 2.2 Ir a Órdenes
        cy.visit(`${BASE_URL}/Admin/Orders`);
        cy.get('table tbody tr', { timeout: 10000 }).should('have.length.greaterThan', 0);

        // 2.3 Entrar al detalle del primer pedido (el recién creado)
        cy.get('tbody tr').first().find('a').contains(/Detalles|Ver/i).click();

        // 2.4 De Detalles a Edición
        cy.url().then($url => {
            if ($url.includes('/Details')) {
                cy.log('ℹ️ Estamos en Detalles. Buscando botón para ir a Editar...');
                // Buscamos el botón 'Editar' por su texto o atributo href
                cy.get('a[href*="/Edit"], button:contains("Editar")').first().click();
            }
        });

        // 2.5 Cambiar Estado
        cy.log('🚚 Cambiando estado a ENVIADO');
        cy.url().should('include', '/Edit');

        // Usamos .select() con el texto de la opción si es un dropdown
        cy.get('select[name*="Status"], select[name*="Estado"]').then($select => {
            if ($select.length > 0) {
                // Selecciona la opción que contenga el texto 'Enviado' o 'Shipping'
                cy.wrap($select).select('Enviado', { force: true });
            } else {
                // Fallback para input: asume que hay un input text si no hay select
                cy.get('input[name*="Status"]').clear().type('Enviado');
            }
        });

        // 2.6 Guardar (Selector robusto)
        cy.get('form').find('button.btn-primary, input[value="Guardar"]').click();

        // 2.7 Validación Final
        cy.url().should('include', '/Admin/Orders');
        cy.contains('td', 'Enviado', { timeout: 10000 }).should('be.visible');
        cy.log('✅ Pedido actualizado por Logística correctamente');
    });
});