// cypress/e2e/payment_flow.cy.js

const BASE_URL = 'http://localhost:5012';
const CUSTOMER_USER = 'prueba@gmail.com';
const CUSTOMER_PASS = 'Prueba123@';

describe('Flujo de Compra Completo con Pago (Simulación)', () => {

  beforeEach(() => {
    cy.session('customerLogin', () => {
      cy.visit(`${BASE_URL}/Identity/Account/Login`);
      cy.get('input[name="Input.Email"]').type(CUSTOMER_USER,{ delay: 150 });
      cy.get('input[id="passwordInput"]').type(CUSTOMER_PASS,{ delay: 150 });
      cy.get('form#account').find('button[type="submit"]').click();
      cy.url().should('not.include', '/Login');
    });
  });

  it('Compra un producto con envío a domicilio y completa PaymentSimulation', () => {

    // 1️⃣ Agregar producto al carrito
    cy.visit(`${BASE_URL}/Products/Details/1`);
    cy.get('body').then(($body) => {
      if ($body.find('form[action*="/Cart/Add"]').length > 0) {
        cy.get('form[action*="/Cart/Add"]').find('button[type="submit"]').click();
      }
    });

    // 2️⃣ Verificar que estamos en el carrito
    cy.url().should('include', '/Cart');

    // 3️⃣ Ir a Confirmación
    cy.get('form[action*="/Checkout/Confirm"]').find('button[type="submit"]').click();

    // 4️⃣ Seleccionar envío a domicilio en Confirmación
    cy.get('input[type="radio"][value="HomeDelivery"]').check({ force: true });
    cy.get('form[action*="/Checkout/ProcessConfirm"]').find('button[type="submit"]').click();

    // 5️⃣ Llegar a PaymentSimulation
    cy.url().should('include', '/Checkout/PaymentSimulation');

    // 6️⃣ Escribir datos de tarjeta **simulando la escritura real**
    const cardNumber = '4111111111111111';
    for (let char of cardNumber) {
      cy.get('#CardNumber').type(char,{ delay: 150 });
    }

    const expiration = '1228';
    for (let char of expiration) {
      cy.get('#Expiration').type(char,{ delay: 150 });
    }

    const cvv = '123';
    for (let char of cvv) {
      cy.get('#CVV').type(char,{ delay: 150 });
    }

    // 7️⃣ Enviar pago
    cy.get('form[action*="ProcessPaymentSimulation"]')
      .find('button[type="submit"]')
      .click();

    // 8️⃣ Verificar éxito
    cy.url().should('include', '/Checkout/Success');
    cy.get('h1').should('contain', '¡Compra Exitosa!');
    cy.log('🎉 Prueba completada: pago simulado con envío a domicilio');

    
  });
});


