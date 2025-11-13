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
        cy.get('#passwordInput').type(password); // ID Corregido
        cy.get('#confirmPasswordInput').type(password); // ID Corregido
        
        cy.get('form#registerForm button[type="submit"]').click();

        // Verificación de redirección al Home/Catálogo tras un registro exitoso.
        cy.url().should('eq', `${baseUrl}/`);
    });

    // ---------------------------------------------------------------------
    // REQUISITO: Validación de Contraseñas
    // ---------------------------------------------------------------------
    it('2. Cliente: Debe mostrar un error cuando las contraseñas no coinciden', () => {
        const testEmail = generateRandomEmail();
        const correctPassword = 'Password123!';
        const wrongPassword = 'Password-wrong!';

        cy.log('// Prueba de validación de contraseñas');
        cy.visit(`${baseUrl}/Identity/Account/Register`);

        cy.get('#Input_Email').type(testEmail);
        cy.get('#passwordInput').type(correctPassword); // ID Corregido
        cy.get('#confirmPasswordInput').type(wrongPassword); // ID Corregido

        cy.get('form#registerForm button[type="submit"]').click();

        // Verificación 1: Debe permanecer en la página de registro.
        cy.url().should('eq', `${baseUrl}/Identity/Account/Register`);

        // Verificación 2: Busca el mensaje de error de validación del modelo (en español).
        cy.contains('La contraseña y la contraseña de confirmación no coinciden.').should('be.visible');
    });

    // ---------------------------------------------------------------------
    // REQUISITO 3: Flujo de Compra (Login -> Añadir Producto -> Verificar Carrito)
    // ---------------------------------------------------------------------
    it('3. Cliente: Debe iniciar sesión, agregar un producto y verlo en el carrito', () => {
    
        cy.log('// Requisito: Inicio de sesión como Cliente');
        cy.visit(`${baseUrl}/Identity/Account/Login`);
        
        cy.get('#Input_Email').type(clientEmail); // Email del Test 1
        cy.get('#passwordInput').type(password); // ID Corregido
        
        cy.get('form#account button[type="submit"]').click();
    
        // Verificación 1: Redirección al Home.
        cy.url().should('eq', `${baseUrl}/`); 
    
        // --- INICIO DE LA MEJORA ---
    
        cy.log('// Requisito: Navegar a Productos');
        // Hacemos clic en el enlace "Productos" de la barra de navegación
        // (Este selector funciona porque está en tu Home/Index.cshtml)
        cy.contains('a.nav-link', 'Productos').click();
    
        // Verificación 2: Estamos en la página de productos
        cy.url().should('include', '/Products');
    
        cy.log('// Requisito: Agregar un producto al carrito');
        // Buscamos el primer formulario que apunte a /Cart/Add (basado en Products/Index.cshtml)
        cy.get('form[action*="/Cart/Add"]').first().within(() => {
            cy.get('button[type="submit"]').click();
        });
    
        // --- ¡AQUÍ ESTÁ LA CORRECCIÓN! ---
        cy.log('// Verificación 3: El controlador redirige a la página del carrito');
        // Tu CartController redirige a "Index" (es decir, /Cart).
        // Así que verificamos que la URL sea la del carrito.
        cy.url().should('eq', `${baseUrl}/Cart`);
    
        // (Hemos eliminado la búsqueda del Toast que no existe)
    
        cy.log('// Verificación 4: El carrito NO está vacío');
        
        // ¡AJUSTAR ESTO! Cambia el texto si tu mensaje de "carrito vacío" es diferente.
        cy.contains('Tu carrito está vacío').should('not.exist');
    
        // Verificamos que SÍ haya al menos un item en el carrito.
        // ¡AJUSTAR ESTO! Si no usas una tabla (tbody tr), cambia el selector.
        cy.get('tbody tr').should('have.length.greaterThan', 0);
    });

});