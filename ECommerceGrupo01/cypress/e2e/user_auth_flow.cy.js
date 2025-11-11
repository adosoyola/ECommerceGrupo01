// user_auth_flow.cy.js
// Archivo de pruebas para el Flujo de Autenticación de Usuarios (solo pruebas de Cliente)

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

    // ---------------------------------------------------------------------
    // REQUISITO 1: Registro de clientes con validación de correo electrónico.
    // ---------------------------------------------------------------------
    it('1. Cliente: Debe registrar un nuevo usuario con éxito y redirigir a la página de inicio', () => {
        cy.log('// Requisito: Registro de clientes con validación de correo electrónico.');
        cy.visit(`${baseUrl}/Identity/Account/Register`);
        cy.get('#Input_Email').type(clientEmail);
        cy.get('#Input_Password').type(password);
        cy.get('#Input_ConfirmPassword').type(password);
        cy.get('button[type="submit"]').click();

        // Verificación de redirección al Home/Catálogo tras un registro exitoso.
        cy.url().should('eq', `${baseUrl}/`);
    });

    it('2. Cliente: Debe mostrar un error cuando las contraseñas no coinciden', () => {
        // Generamos un email único para esta prueba
        const testEmail = generateRandomEmail();
        const correctPassword = 'Password123!';
        const wrongPassword = 'Password-wrong!';

        cy.log('// Prueba de validación de contraseñas');
        cy.visit(`${baseUrl}/Identity/Account/Register`);

        cy.get('#Input_Email').type(testEmail);
        cy.get('#Input_Password').type(correctPassword);
        cy.get('#Input_ConfirmPassword').type(wrongPassword);

        cy.get('button[type="submit"]').click();

        // Verificación 1: Debe permanecer en la página de registro.
        cy.url().should('eq', `${baseUrl}/Identity/Account/Register`);

        // Verificación 2: Busca el mensaje de error de validación del modelo.
        cy.contains('The password and confirmation password do not match.').should('be.visible');
    });

    // ---------------------------------------------------------------------
    // REQUISITO 2: Inicio de sesión con roles diferenciados (Cliente)
    // ---------------------------------------------------------------------
    it('3. Cliente: Debe iniciar sesión con el nuevo usuario y redirigir al carrito', () => {
        cy.log('// Requisito: Inicio de sesión como Cliente');
        cy.visit(`${baseUrl}/Identity/Account/Login`);
        cy.get('#Input_Email').type(clientEmail);
        cy.get('#Input_Password').type(password);
        cy.get('button[type="submit"]').click();

        // Verificación de redirección: El informe indica que se redirige al carrito.
        cy.url().should('eq', `${baseUrl}/Cart`);
    });
});