// Archivo de pruebas para el Flujo de Autenticación de Usuarios (con Checkout E2E)

// Función para generar un correo electrónico único en cada prueba
function generateRandomEmail() {
    const randomString = Math.random().toString(36).substring(2, 8);
    // Usamos el dominio 'system.com' para simular correos de prueba
    return `prueba_user_${randomString}@system.com`;
}

describe('Flujo de Autenticación de Usuarios (Solo Cliente)', () => {

    const baseUrl = 'http://localhost:5012';
    // Generamos un correo de cliente que usaremos en las pruebas 1 y 3
    const clientEmail = generateRandomEmail();
    const password = 'Test@123'; // Contraseña que cumple requisitos

    // 🚨 Datos de Envío/Destinatario para el Checkout (IDs basados en tu formulario)
    const shippingData = {
        fullName: 'Cliente de Prueba',
        address: 'Av. De la Prueba 123',
        postalCode: '08001',
        phone: '987654321',
        instructions: 'Dejar en portería.'
    };


    // ---------------------------------------------------------------------
    // REQUISITO 1: Registro de clientes con validación de correo electrónico.
    // ---------------------------------------------------------------------
    it('1. Cliente: Debe registrar un nuevo usuario con éxito y redirigir a la página de inicio', () => {
        cy.log('// Requisito: Registro de clientes con validación de correo electrónico.');
        cy.visit(`${baseUrl}/Identity/Account/Register`);

        cy.get('#Input_Email').type(clientEmail);
        cy.get('#passwordInput').type(password);
        cy.get('#confirmPasswordInput').type(password);

        cy.get('form#registerForm button[type="submit"]').click();

        // Verificación de redirección al Home/Catálogo tras un registro exitoso.
        cy.url().should('eq', `${baseUrl}/`);
    });

    // ---------------------------------------------------------------------
    // REQUISITO 2: Validación de Contraseñas
    // ---------------------------------------------------------------------
    it('2. Cliente: Debe mostrar un error cuando las contraseñas no coinciden', () => {
        const testEmail = generateRandomEmail();
        const correctPassword = 'Password123!';
        const wrongPassword = 'Password-wrong!';

        cy.log('// Prueba de validación de contraseñas');
        cy.visit(`${baseUrl}/Identity/Account/Register`);

        cy.get('#Input_Email').type(testEmail);
        cy.get('#passwordInput').type(correctPassword);
        cy.get('#confirmPasswordInput').type(wrongPassword);

        cy.get('form#registerForm button[type="submit"]').click();

        // Verificación 1: Debe permanecer en la página de registro.
        cy.url().should('eq', `${baseUrl}/Identity/Account/Register`);

        // Verificación 2: Busca el mensaje de error de validación del modelo (en español).
        cy.contains('La contraseña y la contraseña de confirmación no coinciden.').should('be.visible');
    });

    // ---------------------------------------------------------------------
    // REQUISITO 3: Flujo de Compra (Login -> Añadir Producto -> Checkout Completo)
    // ---------------------------------------------------------------------
    it('3. Cliente: Debe iniciar sesión, agregar un producto y completar el checkout', () => {

        // --- A. LOGIN (Usamos cy.session para persistencia si fuera necesario, pero aquí solo es un login directo) ---
        cy.log('// Paso A: Inicio de sesión como Cliente');
        cy.visit(`${baseUrl}/Identity/Account/Login`);

        cy.get('#Input_Email').type(clientEmail); // Email del Test 1
        cy.get('#passwordInput').type(password);

        cy.get('form#account button[type="submit"]').click();

        // Verificación 1: Redirección al Home.
        cy.url().should('eq', `${baseUrl}/`);

        // --- B. AÑADIR PRODUCTO ---
        cy.log('// Paso B: Navegar a Productos y Agregar un producto al carrito');
        // Hacemos clic en el enlace "Productos" de la barra de navegación
        cy.contains('a.nav-link', 'Productos').click();
        cy.url().should('include', '/Products');

        // Buscamos el primer formulario que apunte a /Cart/Add 
        cy.get('form[action*="/Cart/Add"]').first().within(() => {
            cy.get('button[type="submit"]').click();
        });

        // Verificación 2: El controlador redirige a la página del carrito
        cy.url().should('eq', `${baseUrl}/Cart`);
        cy.get('tbody tr').should('have.length.greaterThan', 0);

        // --- C. INICIAR CHECKOUT (CORRECCIÓN APLICADA AQUÍ) ---
        cy.log('// Paso C: Iniciando el Checkout - Clic en el botón "Pagar" o "Continuar" del Carrito.');

        // CORRECCIÓN: Usamos cy.contains para encontrar el botón que lleva a la confirmación de la compra, 
        // evitando el enlace de historial.
        cy.contains('a, button', /Pagar|Checkout|Finalizar Compra/i)
            .click(); // Asumimos que es el botón principal de acción

        // Verificación 3: Redirección a la página de Checkout/Confirm
        cy.url().should('include', '/Checkout/Confirm', { timeout: 10000 });

        // --- D. LLENAR DATOS DE ENVÍO Y PASAR A PAGO ---
        cy.log('// Paso D: Llenando datos de envío obligatorios y pasando a la simulación de pago.');

        // 1. Llenar los campos de Destinatario (IDs basados en la estructura HTML)
        cy.get('input#FullName').clear().type(shippingData.fullName);
        cy.get('input#Address').clear().type(shippingData.address);
        cy.get('input#PostalCode').clear().type(shippingData.postalCode);
        cy.get('input#PhoneNumber').clear().type(shippingData.phone);
        cy.get('textarea#SpecialInstructions').clear().type(shippingData.instructions); // Opcional

        // 2. Seleccionar el Método de Entrega (Si no está preseleccionado)
        // Buscamos el radio button para "Envío a Domicilio" o "Recojo en Tienda" si existiera
        cy.get('input[name*="MetodoEntrega"], input[type="radio"]').first().check({ force: true });


        // 3. Clic en el botón "Ir a Pagar"
        cy.contains('button', /Ir a Pagar/i)
            .scrollIntoView()
            .click();

        // --- E. VERIFICACIÓN Y PAGO FINAL ---
        cy.log('// Paso E: Manejando la página de Simulación de Pago.');

        // E.1 Verificación de la URL de Simulación o Éxito
        cy.url().should(url => {
            // Se espera que la URL contenga una de las palabras clave de éxito o de simulación de pago
            expect(url).to.satisfy(
                () => url.includes('/PaymentSimulation') || url.includes('/Checkout/Success') || url.includes('/Order/Confirmation'),
                'La URL debe ser el simulador de pago o la página final de éxito.'
            );
        }).then(url => {
            if (url.includes('/PaymentSimulation')) {
                cy.log('// E.2: Realizando Simulación de Pago...');

                // Rellenar campos de tarjeta (basado en el flujo de pago original del usuario)
                cy.get('input#CardNumber, input[name*="CardNumber"]').clear().type('4111111111111111');
                cy.get('input#Expiration, input[name*="Expiration"]').clear().type('12/28');
                cy.get('input#CVV, input[name*="CVV"]').clear().type('123');

                // Enviar el formulario de simulación de pago
                // Clic en el botón "Pagar"
                cy.contains('button', /Pagar/i).click();

                // Esperar la redirección a la página de éxito
                cy.url().should(finalUrl => {
                    expect(finalUrl).to.satisfy(
                        () => finalUrl.includes('/Checkout/Success') || finalUrl.includes('/Order/Confirmation'),
                        'Después de la simulación, debe ir a la página de éxito.'
                    );
                }, { timeout: 10000 });
            }
        });

        cy.log('✅ Flujo de Autenticación y Checkout E2E completado exitosamente hasta la verificación de URL final.');
    });

});