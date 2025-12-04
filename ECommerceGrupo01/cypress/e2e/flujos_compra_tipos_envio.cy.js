// Archivo: cypress/e2e/compra_envio_recojo.cy.js

describe('Flujos de Compra: Envío a Domicilio vs Recojo en Tienda', () => {

    const BASE_URL = 'http://localhost:5012';
    // Credenciales del cliente de prueba
    const CLIENT_USER = 'prueba@gmail.com';
    const CLIENT_PASS = 'Prueba123@';

    // Datos de Envío Requeridos para Caso A
    const SHIPPING_DATA = {
        fullName: 'Prueba Cliente Cypress',
        address: 'Av. La Cultura 123, Cusco',
        postalCode: '08001',
        phone: '987654321'
    };

    // --- Funciones Reutilizables ---

    // 1. Función para Login
    const login = () => {
        cy.visit(`${BASE_URL}/Identity/Account/Login`);
        cy.get('input[name="Input.Email"]').clear().type(CLIENT_USER);
        cy.get('input[id="passwordInput"]').clear().type(CLIENT_PASS);
        cy.get('form#account button[type="submit"]').click();
        cy.url().should('not.include', '/Login');
    };

    // 2. Función para Agregar Producto al Carrito
    const agregarProductoAlCarrito = () => {
        cy.visit(`${BASE_URL}/Products`);

        // Entrar al primer producto
        cy.get('.card').first().within(() => { cy.get('a').click(); });

        // Verificar Stock
        cy.get('body').then(($body) => {
            if ($body.text().includes('Stock: 0') || $body.text().includes('Agotado')) {
                assert.fail("⚠️ El producto no tiene stock. Asegúrate de que el producto de prueba tenga stock en la BD.");
            }
        });

        // Llenar cantidad y enviar formulario a la fuerza para evitar problemas de clic
        cy.get('input#qty').clear().type('1');
        cy.get('form[action*="Cart/Add"]').submit();
        cy.wait(1500); // Espera técnica para la BD
    };

    // 3. Función para Limpiar Carrito
    const limpiarCarrito = () => {
        cy.visit(`${BASE_URL}/Cart`);
        cy.log('🗑️ Limpiando el carrito antes de la prueba con enfoque iterativo robusto...');

        const removeNextItem = () => {
            cy.get('body').then(($body) => {
                const removeForm = $body.find('form[action*="Cart/Remove"]').first();

                if (removeForm.length) {
                    cy.log('🔄 Ítem encontrado. Removiendo...');
                    cy.wrap(removeForm).submit();
                    cy.wait(1500).then(() => {
                        cy.visit(`${BASE_URL}/Cart`);
                        removeNextItem();
                    });
                } else {
                    cy.log('🛒 Carrito vacío. Verificando mensaje de éxito.');
                    cy.contains('Tu carrito está vacío', { timeout: 10000 }).should('be.visible');
                }
            });
        };

        removeNextItem();
    };


    // --- Ejecutar antes de CADA prueba ---
    beforeEach(() => {
        // Aseguramos el login (o restauramos la sesión)
        cy.session('clientSession', login);
        cy.visit(BASE_URL);

        // Limpieza y preparación
        limpiarCarrito();
        agregarProductoAlCarrito();
        cy.visit(`${BASE_URL}/Cart`); // Ir al carrito para iniciar el checkout
    });

    // --- PRUEBA 1: ENVÍO A DOMICILIO ---
    it('Caso A: Compra con ENVÍO A DOMICILIO (Llena dirección y paga)', () => {
        // PASO 1: Confirmar Compra / Procesar Pago desde el carrito
        cy.log('1. Clic en "Confirmar Compra" / "Procesar Pago"');
        cy.contains('a, button, input[type="submit"]', /Confirmar Compra|Procesar Pago|Pagar|Checkout/i).click();

        cy.log('2. PANTALLA DE DATOS DE ENVÍO - Seleccionando Envío a Domicilio y llenando dirección');

        // Seleccionar Envío a Domicilio
        cy.contains('label', /Envío|Domicilio/i).click();

        // Llenar TODOS los campos obligatorios para que el formulario avance (CORRECCIÓN)
        cy.get('input#FullName').clear().type(SHIPPING_DATA.fullName);
        cy.get('input[name*="Address"], input[name*="Direccion"]').clear().type(SHIPPING_DATA.address);
        // Asumimos que estos campos también son requeridos
        cy.get('input#PostalCode').clear().type(SHIPPING_DATA.postalCode);
        cy.get('input#PhoneNumber').clear().type(SHIPPING_DATA.phone);

        // Clic en "Ir a Pagar" / "Continuar" para avanzar a la simulación de pago
        cy.log('Clic en "Ir a Pagar" o "Continuar"');
        cy.get('button[type="submit"], input[type="submit"]').contains(/Pagar|Continuar|Siguiente|Ir a Pagar/i).click();

        cy.log('3. PANTALLA DE PAGO - Simulación');
        // El .should('include', 'PaymentSimulation') ahora debería pasar después de llenar los campos
        cy.url().should('include', 'PaymentSimulation', { timeout: 10000 });

        // Llenar datos de la tarjeta (usando la secuencia de pago estándar)
        cy.get('input#CardNumber').type('4111111111111111');
        cy.get('input#Expiration').type('12/26');
        cy.get('input#CVV').type('123');

        // Pagar: Usamos cy.contains para evitar la ambigüedad (CORRECCIÓN)
        cy.contains('button', /Pagar/i).click();

        cy.log('4. Validación Final');
        cy.url().should('include', '/Checkout/Success');
        cy.contains('Compra Exitosa', { timeout: 10000 }).should('be.visible');
        cy.log('¡Compra Exitosa! ✅');
    });


    // --- PRUEBA 2: RECOJO EN TIENDA ---
    it('Caso B: Compra con RECOJO EN TIENDA (Salta dirección y paga)', () => {
        // PASO 1: Confirmar Compra / Procesar Pago desde el carrito
        cy.log('1. Clic en "Confirmar Compra" / "Procesar Pago"');
        cy.contains('a, button, input[type="submit"]', /Confirmar Compra|Procesar Pago|Pagar|Checkout/i).click();

        cy.log('2. PANTALLA DE DATOS DE ENVÍO - Seleccionando Recojo en Tienda');

        // Seleccionar Recojo en Tienda
        cy.contains('label', /Recojo|Tienda/i).click();

        // Clic en "Ir a Pagar" / "Continuar"
        cy.log('Clic en "Ir a Pagar" o "Continuar"');
        cy.get('button[type="submit"], input[type="submit"]').contains(/Pagar|Continuar|Siguiente|Ir a Pagar/i).click();

        cy.log('3. PANTALLA DE PAGO - Simulación');
        cy.url().should('include', 'PaymentSimulation');

        // Llenar datos de la tarjeta (usando la secuencia de pago estándar)
        cy.get('input#CardNumber').type('4111111111111111');
        cy.get('input#Expiration').type('12/26');
        cy.get('input#CVV').type('123');

        // Pagar: Usamos cy.contains para evitar la ambigüedad (CORRECCIÓN)
        cy.contains('button', /Pagar/i).click();

        cy.log('4. Validación Final');
        cy.url().should('include', '/Checkout/Success');
        cy.contains('Compra Exitosa', { timeout: 10000 }).should('be.visible');
        cy.log('¡Compra Exitosa! ✅');
    });

});