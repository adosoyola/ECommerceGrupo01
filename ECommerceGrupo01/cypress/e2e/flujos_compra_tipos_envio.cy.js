describe('Flujos de Compra: Envío a Domicilio vs Recojo en Tienda', () => {

    const BASE_URL = 'http://localhost:5012'; 
    const CLIENT_USER = 'cliente_test@gmail.com'; 
    const CLIENT_PASS = 'Test@123';

    // Función para Login (Reutilizable)
    const login = () => {
        cy.visit(`${BASE_URL}/Identity/Account/Login`);
        cy.get('input[name="Input.Email"]').clear().type(CLIENT_USER);
        cy.get('input[id="passwordInput"]').clear().type(CLIENT_PASS);
        cy.get('form#account button[type="submit"]').click();
    };

    // Función para Agregar Producto al Carrito (Reutilizable y Robusta)
    const agregarProductoAlCarrito = () => {
        cy.visit(`${BASE_URL}/Products`);
        
        // Entrar al primer producto
        cy.get('.card').first().within(() => { cy.get('a').click(); });

        // Verificar Stock
        cy.get('body').then(($body) => {
            if ($body.text().includes('Stock: 0')) {
                throw new Error("⚠️ El producto no tiene stock. Cambia de producto en la BD.");
            }
        });

        // Llenar cantidad y enviar formulario a la fuerza (Fix del carrito vacío)
        cy.get('input#qty').clear().type('1');
        cy.get('form[action*="Cart/Add"]').submit();
        cy.wait(2000); // Espera técnica para la BD
    };

    // --- PRUEBA 1: ENVÍO A DOMICILIO ---
    it('Caso A: Compra con ENVÍO A DOMICILIO (Llena dirección y paga)', () => {
        
        // 1. Preparación
        cy.session('clientSession', login);
        agregarProductoAlCarrito();

        // 2. Ir al Carrito
        cy.visit(`${BASE_URL}/Cart`);
        
        // 3. Clic en "Procesar Pago" (Buscando el botón correcto)
        cy.get('body').then(($body) => {
            if ($body.find('input[value*="Procesar"]').length > 0) {
                cy.get('input[value*="Procesar"]').click();
            } else {
                cy.contains(/Procesar|Pagar|Checkout/i).click();
            }
        });

        // 4. PANTALLA DE DATOS DE ENVÍO (La de tu imagen image_7a8c6f.png)
        // Aquí es donde seleccionamos el tipo de envío
        cy.log('🚚 Seleccionando: Envío a Domicilio');
        
        // Buscamos el texto "Envío a domicilio" y le damos clic
        // Esto debería activar el radio button asociado
        cy.contains('label', /Envío|Domicilio/i).click();

        // Llenar la dirección (Solo necesario para envío a domicilio)
        // Buscamos un input que sea de dirección, ciudad o calle
        cy.get('body').then(($body) => {
            if ($body.find('input[name*="Address"], input[name*="Direccion"]').length > 0) {
                cy.get('input[name*="Address"], input[name*="Direccion"]')
                  .clear().type('Av. La Cultura 123, Cusco');
            }
        });

        // Clic en "Ir a Pagar" o "Continuar"
        cy.get('button[type="submit"]').contains(/Pagar|Continuar|Siguiente/i).click();

        // 5. PANTALLA DE PAGO
        cy.url().should('include', 'PaymentSimulation');
        
        cy.get('input#CardNumber').type('4111111111111111');
        cy.get('input#Expiration').type('12/30');
        cy.get('input#CVV').type('123');
        
        // Pagar
        cy.get('input#CardNumber').parents('form').find('button[type="submit"]').click();

        // 6. Validación Final
        cy.url().should('include', '/Checkout/Success');
        cy.log('✅ Compra con Envío a Domicilio Exitosa');
    });


    // --- PRUEBA 2: RECOJO EN TIENDA ---
    it('Caso B: Compra con RECOJO EN TIENDA (Salta dirección y paga)', () => {
        
        // 1. Preparación
        cy.session('clientSession', login);
        agregarProductoAlCarrito();

        // 2. Ir al Carrito
        cy.visit(`${BASE_URL}/Cart`);
        
        // 3. Clic en "Procesar Pago"
        cy.get('body').then(($body) => {
            if ($body.find('input[value*="Procesar"]').length > 0) {
                cy.get('input[value*="Procesar"]').click();
            } else {
                cy.contains(/Procesar|Pagar|Checkout/i).click();
            }
        });

        // 4. PANTALLA DE DATOS DE ENVÍO
        cy.log('🏪 Seleccionando: Recojo en Tienda');

        // Buscamos el texto "Recojo en tienda" o "Tienda" y le damos clic
        cy.contains('label', /Recojo|Tienda/i).click();

        // NOTA: Al seleccionar tienda, normalmente NO se llena dirección.
        // Así que vamos directo al botón de continuar.

        // Clic en "Ir a Pagar"
        cy.get('button[type="submit"]').contains(/Pagar|Continuar|Siguiente/i).click();

        // 5. PANTALLA DE PAGO
        cy.url().should('include', 'PaymentSimulation');
        
        cy.get('input#CardNumber').type('4111111111111111');
        cy.get('input#Expiration').type('12/30');
        cy.get('input#CVV').type('123');
        
        // Pagar
        cy.get('input#CardNumber').parents('form').find('button[type="submit"]').click();

        // 6. Validación Final
        cy.url().should('include', '/Checkout/Success');
        cy.log('✅ Compra con Recojo en Tienda Exitosa');
    });

});