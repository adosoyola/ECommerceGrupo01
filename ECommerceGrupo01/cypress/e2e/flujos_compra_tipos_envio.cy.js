// Archivo: cypress/e2e/flujos_compra_tipos_envio.cy.js

describe('Flujos de Compra: Envío a Domicilio vs Recojo en Tienda', () => {

    // Credenciales del cliente de prueba
    const CLIENT_USER = 'prueba@gmail.com';
    const CLIENT_PASS = 'Prueba123@'; // Cumple con requisitos complejos por si acaso

    // Datos de Envío
    const SHIPPING_DATA = {
        fullName: 'Prueba Cliente Cypress',
        address: 'Av. La Cultura 123, Cusco',
        postalCode: '08001',
        phone: '987654321'
    };

    // --- 1. Login Inteligente (Si no existe, se registra) ---
    const loginOrRegister = () => {
        cy.visit('/Identity/Account/Login');
        
        // Llenar formulario de Login
        cy.get('input[name="Input.Email"]').clear().type(CLIENT_USER);
        // Intentamos selector estándar de Identity 'Input.Password' o el id 'passwordInput'
        cy.get('input[name="Input.Password"], input[id="passwordInput"]').clear().type(CLIENT_PASS);
        cy.get('button[type="submit"]').click();

        // VERIFICACIÓN INTELIGENTE
        cy.get('body').then(($body) => {
            // Si vemos un mensaje de error o seguimos en /Login, el usuario no existe
            if ($body.text().includes('Invalid login') || $body.text().includes('no válido') || $body.find('.validation-summary-errors').length > 0) {
                cy.log('⚠️ Usuario no encontrado. Procediendo a REGISTRARSE...');
                
                // Ir al registro
                cy.visit('/Identity/Account/Register');
                
                // Llenar registro
                cy.get('input[name="Input.Email"]').clear().type(CLIENT_USER);
                cy.get('input[name="Input.Password"]').clear().type(CLIENT_PASS);
                cy.get('input[name="Input.ConfirmPassword"]').clear().type(CLIENT_PASS);
                
                // Submit Registro
                cy.get('button[type="submit"]').click();
            }
        });

        // Asegurar que ya no estamos en Login o Register
        cy.url().should('not.include', '/Login');
        cy.url().should('not.include', '/Register');
    };

    // --- 2. Agregar al Carrito ---
    const agregarProductoAlCarrito = () => {
        cy.visit('/Products'); // Asumiendo que esta es la ruta pública

        // Verificar que hay productos
        cy.get('.card', { timeout: 10000 }).should('have.length.gt', 0);

        // Entrar al primer producto disponible
        cy.get('.card').first().within(() => { 
            cy.get('a').click(); 
        });

        // Verificar Stock visualmente antes de intentar añadir
        cy.get('body').then(($body) => {
            if ($body.text().includes('Stock: 0') || $body.text().includes('Agotado')) {
                // Si está agotado, volvemos e intentamos con el segundo producto
                cy.log('Primer producto agotado, intentando con otro...');
                cy.visit('/Products');
                cy.get('.card').eq(1).find('a').click();
            }
        });

        // Añadir "1" cantidad
        cy.get('input[name="Quantity"], input#qty').clear().type('1');
        
        // Enviar formulario (funciona mejor que click en botón a veces)
        cy.get('form[action*="Cart/Add"]').submit();
        
        // Espera breve para que la DB procese
        cy.wait(1000);
    };

    // --- 3. Limpiar Carrito (Robusto) ---
    const limpiarCarrito = () => {
        cy.visit('/Cart');
        cy.get('body').then(($body) => {
            // Si encuentra botones de eliminar (generalmente form con action Remove)
            const removeButtons = $body.find('button[formaction*="Remove"], a[href*="Remove"], form[action*="Remove"] button');
            
            if (removeButtons.length > 0) {
                cy.log(`🗑️ Encontrados ${removeButtons.length} ítems. Limpiando...`);
                // Hacer clic en el primero y recursividad simple
                cy.wrap(removeButtons).first().click();
                cy.wait(500);
                limpiarCarrito(); // Llamada recursiva hasta que esté vacío
            } else {
                cy.log('✅ Carrito limpio.');
            }
        });
    };

    // --- CONFIGURACIÓN PREVIA (BEFORE EACH) ---
    beforeEach(() => {
        // Usamos cy.session para guardar las cookies y no loguearnos cada vez
        cy.session('clientSession', () => {
            loginOrRegister();
        }, 
        {
            validate: () => {
                // Validamos que la cookie de sesión exista
                cy.getCookie('.AspNetCore.Identity.Application').should('exist');
            }
        });

        // Pasos de preparación de datos
        limpiarCarrito();
        agregarProductoAlCarrito();
        cy.visit('/Cart'); // Ir al carrito listo para el test
    });

    // --- TEST CASO A: ENVÍO A DOMICILIO ---
    it('Caso A: Compra con ENVÍO A DOMICILIO (Llena dirección y paga)', () => {
        // 1. Iniciar Checkout
        cy.contains(/Confirmar|Procesar|Pagar|Checkout/i).click();

        // 2. Selección de Envío
        // Busca un label o input que diga "Domicilio" o "Envío"
        cy.get('body').contains(/Domicilio|Envío/i).click();

        // 3. Llenar Dirección (Buscamos inputs visibles)
        // Usamos selectores flexibles por si cambian los IDs
        cy.get('input[name*="FullName"], input[name*="Nombre"]').clear().type(SHIPPING_DATA.fullName);
        cy.get('input[name*="Address"], input[name*="Direccion"]').clear().type(SHIPPING_DATA.address);
        cy.get('input[name*="Postal"], input[name*="Zip"]').clear().type(SHIPPING_DATA.postalCode);
        cy.get('input[name*="Phone"], input[name*="Telef"]').clear().type(SHIPPING_DATA.phone);

        // 4. Continuar a Pago
        cy.get('button[type="submit"]').contains(/Pagar|Continuar|Siguiente/i).click();

        // 5. Simulación de Pago
        cy.url().should('include', 'PaymentSimulation');
        
        // Tarjeta Dummy
        cy.get('input[name*="Card"], input#CardNumber').type('4111111111111111');
        cy.get('input[name*="Expir"], input#Expiration').type('12/30');
        cy.get('input[name*="CVV"], input#CVV').type('123');

        // Confirmar Pago
        cy.get('button').contains(/Pagar/i).click();

        // 6. Validación Final
        cy.url().should('include', 'Success');
        cy.contains(/Exitosa|Gracias/i).should('be.visible');
    });

    // --- TEST CASO B: RECOJO EN TIENDA ---
    it('Caso B: Compra con RECOJO EN TIENDA (No pide dirección)', () => {
        // 1. Iniciar Checkout
        cy.contains(/Confirmar|Procesar|Pagar|Checkout/i).click();

        // 2. Selección de Recojo
        // Busca un label o input que diga "Tienda" o "Recojo"
        cy.get('body').contains(/Tienda|Recojo/i).click();

        // NOTA: Al seleccionar tienda, los inputs de dirección deberían ocultarse o no ser obligatorios
        // Si tu sistema pide dirección aun en recojo en tienda, avísame.

        // 3. Continuar a Pago directamente
        cy.get('button[type="submit"]').contains(/Pagar|Continuar|Siguiente/i).click();

        // 4. Simulación de Pago
        cy.url().should('include', 'PaymentSimulation');
        
        // Tarjeta Dummy
        cy.get('input[name*="Card"], input#CardNumber').type('4111111111111111');
        cy.get('input[name*="Expir"], input#Expiration').type('12/30');
        cy.get('input[name*="CVV"], input#CVV').type('123');

        // Confirmar Pago
        cy.get('button').contains(/Pagar/i).click();

        // 5. Validación Final
        cy.url().should('include', 'Success');
        cy.contains(/Exitosa|Gracias/i).should('be.visible');
    });

});