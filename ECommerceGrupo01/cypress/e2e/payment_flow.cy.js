// cypress/e2e/payment_flow.cy.js

// cypress/e2e/payment_flow.cy.js

// --- Constantes de Prueba ---
// cypress/e2e/payment_flow.cy.js

// --- Constantes de Prueba ---
// cypress/e2e/payment_flow.cy.js

// --- Constantes de Prueba ---
// cypress/e2e/payment_flow.cy.js

// --- Constantes de Prueba ---
const BASE_URL = 'http://localhost:5012';

// Datos del Cliente de Prueba
const CUSTOMER_USER = 'prueba@gmail.com';
const CUSTOMER_PASS = 'Prueba123@';

describe('Flujo de Compra Completo (Cliente)', () => {

  // Hacemos login una vez y guardamos la sesión
  beforeEach(() => {
    cy.session('customerLogin', () => {
      cy.visit(`${BASE_URL}/Identity/Account/Login`);

      // 1. LOGIN
      cy.get('input[name="Input.Email"]').type(CUSTOMER_USER);
      cy.get('input[id="passwordInput"]').type(CUSTOMER_PASS);

      // Selector Específico: Apuntar al formulario de login por su ID
      cy.get('form#account').find('button[type="submit"]').click();

      // Verificamos que NO estemos en Login (significa que entró)
      cy.url().should('not.include', '/Login');
    });
  });

  it('Debería permitir a un usuario logueado comprar un producto', () => {

    // 2. AGREGAR PRODUCTO 
    cy.visit(`${BASE_URL}/Products/Details/1`);

    // Selector Específico: Formulario de "Agregar al Carrito"
    cy.get('body').then(($body) => {
      if ($body.find('form[action*="/Cart/Add"]').length > 0) {
        cy.get('form[action*="/Cart/Add"]').find('button[type="submit"]').click();
      } else {
        cy.log('No se encontró el formulario de agregar al carrito');
      }
    });

    // 3. VERIFICAR CARRITO
    cy.url().should('include', '/Cart');

    // 4. IR A CONFIRMAR (CHECKOUT)
    cy.get('form[action*="/Checkout/Confirm"]').find('button[type="submit"]').click();

    // 5. CONFIRMAR COMPRA
    // --- CORRECCIÓN AQUÍ ---
    // Esperamos estar en la página de Confirm
    cy.url().should('include', '/Checkout/Confirm');

    // Hay múltiples botones de submit, necesitamos el botón "Ir a Pagar"
    // Opción 1: Buscar por texto del botón
    cy.contains('button', /Ir a Pagar/i).click();
    
    // Opción alternativa si la anterior no funciona:
    // cy.get('button[type="submit"]').last().click();

    // 6. LLENAR FORMULARIO DE PAGO (Simulación)
    // Esperamos a llegar a la simulación
    cy.url().should('include', '/Checkout/PaymentSimulation', { timeout: 10000 });

    // Tarjeta de prueba
    cy.get('input#CardNumber').clear().type('4111111111111111');
    cy.get('input#Expiration').clear().type('12/28');
    cy.get('input#CVV').clear().type('123');

    // 7. ENVIAR PAGO
    // Buscar específicamente el botón de "Pagar" (evita ambigüedad con múltiples submit buttons)
    cy.contains('button', /Pagar/i).click();

    // 8. VERIFICAR ÉXITO
    cy.url().should('include', '/Checkout/Success', { timeout: 10000 });

    // Verificaciones visuales
    cy.get('h1').should('contain', 'Compra Exitosa');

    cy.log('🎉 ¡Prueba de compra completada exitosamente!');
  });
});