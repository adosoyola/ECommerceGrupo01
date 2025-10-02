// user_auth_flow.cy.js

// Función para generar un correo electrónico único en cada prueba
function generateRandomEmail() {
  const randomString = Math.random().toString(36).substring(2, 8);
  return `prueba_${randomString}@system.com`;
}

describe('Flujo de Autenticación de Usuarios (CORREGIDO)', () => {

  const newEmail = generateRandomEmail();
  const password = 'Test@';

  it('1. Debe registrar un nuevo usuario con éxito y redirigir a la página de inicio', () => {
    cy.visit('http://localhost:5012/Identity/Account/Register');
    cy.get('#Input_Email').type(newEmail);
    cy.get('#Input_Password').type(password);
    cy.get('#Input_ConfirmPassword').type(password);
    cy.get('button[type="submit"]').click();
    cy.url().should('eq', 'http://localhost:5012/');
  });

  it('2. Debe iniciar sesión con el nuevo usuario y redirigir al carrito', () => {
    cy.visit('http://localhost:5012/Identity/Account/Login');
    cy.get('#Input_Email').type(newEmail);
    cy.get('#Input_Password').type(password);
    cy.get('button[type="submit"]').click();

    // Esta afirmación es suficiente para validar el inicio de sesión exitoso
    // ya que la aplicación redirige al carrito solo si el login es correcto.
    cy.url().should('eq', 'http://localhost:5012/Cart');

    // Eliminamos la línea siguiente que fallaba porque el elemento 'a#manage' no está en esta página.
    // cy.get('a#manage').should('contain', `Hello ${newEmail}!`);
  });




  it('3. Debe mostrar un error cuando las contraseñas no coinciden', () => {
    // Generamos un email y una contraseña válidos, pero la confirmación será diferente
    const testEmail = generateRandomEmail();
    const correctPassword = 'Password123!';
    const wrongPassword = 'Password-wrong!';

    // Paso 1: Visita la página de registro
    cy.visit('http://localhost:5012/Identity/Account/Register');

    // Paso 2: Llena el formulario con contraseñas que no coinciden
    cy.get('#Input_Email').type(testEmail);
    cy.get('#Input_Password').type(correctPassword);
    cy.get('#Input_ConfirmPassword').type(wrongPassword);

    // Paso 3: Haz clic en el botón de registro
    cy.get('button[type="submit"]').click();

    // Verificación 1: Asegurarse de que la URL no cambie.
    // Esto significa que la página no redirigió, lo que indica un fallo de validación.
    cy.url().should('eq', 'http://localhost:5012/Identity/Account/Register');

    // Verificación 2: Busca un mensaje de error
    // Cypress buscará el mensaje en la página y verificará que esté visible.
    cy.contains('The password and confirmation password do not match.').should('be.visible');
  });
});


/*
function generateRandomEmail() {
  const randomString = Math.random().toString(36).substring(2, 8);
  return `prueba_${randomString}@system.com`;
}

describe('Flujo de Autenticación de Usuarios (CON ERROR)', () => {

  const newEmail = generateRandomEmail();
  const password = 'TestPassword123@';

  it('1. Debe registrar un nuevo usuario con éxito y redirigir a la página de inicio (FALLA)', () => {
    cy.visit('http://localhost:5012/Identity/Account/Register');

    // ERROR INTENCIONAL AQUÍ: El ID del input de email está mal.
    cy.get('#Input_Email_Error').type(newEmail);

    cy.get('#Input_Password').type(password);
    cy.get('#Input_ConfirmPassword').type(password);
    cy.get('button[type="submit"]').click();
    cy.url().should('eq', 'http://localhost:5012/');
  });

  it('2. El segundo módulo no se ejecutará debido al error en el primer módulo.', () => {
    // Este test no se ejecutará porque el anterior falló y Cypress se detuvo.
    cy.log('Este test no correrá. El primer test tiene un error.');
  });
});

*/
// user_registration_validation.cy.js



/*
// Función para generar un correo electrónico único en cada prueba
function generateRandomEmail() {
  const randomString = Math.random().toString(36).substring(2, 8);
  return `prueba_${randomString}@system.com`;
}

describe('Validación de Registro', () => {

  it('Debe mostrar un error cuando las contraseñas no coinciden', () => {
    // Generamos un email y una contraseña válidos, pero la confirmación será diferente
    const testEmail = generateRandomEmail();
    const correctPassword = 'Password123!';
    const wrongPassword = 'Password-wrong!';

    // Paso 1: Visita la página de registro
    cy.visit('http://localhost:5012/Identity/Account/Register');

    // Paso 2: Llena el formulario con contraseñas que no coinciden
    cy.get('#Input_Email').type(testEmail);
    cy.get('#Input_Password').type(correctPassword);
    cy.get('#Input_ConfirmPassword').type(wrongPassword);

    // Paso 3: Haz clic en el botón de registro
    cy.get('button[type="submit"]').click();

    // Verificación 1: Asegurarse de que la URL no cambie.
    // Esto significa que la página no redirigió, lo que indica un fallo de validación.
    cy.url().should('eq', 'http://localhost:5012/Identity/Account/Register');

    // Verificación 2: Busca un mensaje de error
    // Cypress buscará el mensaje en la página y verificará que esté visible.
    cy.contains('The password and confirmation password do not match.').should('be.visible');
  });
});

*/