// cypress/e2e/payment_flow.cy.js

// --- Constantes de Prueba ---
const BASE_URL = 'http://localhost:5012';

// Datos del Cliente de Prueba
const CUSTOMER_USER = 'prueba@gmail.com';
const CUSTOMER_PASS = 'Prueba123@';

// Datos de envío de prueba
const SHIPPING_DATA = {
  fullName: 'Prueba Cliente Cypress',
  address: 'Av. De la Prueba 123',
  postalCode: '08001',
  phone: '987654321'
};

describe('Flujo de Compra Completo (Cliente)', () => {

  // Hacemos login una vez y guardamos la sesión
  beforeEach(() => {
    cy.session('customerLogin', () => {
      cy.visit(`${BASE_URL}/Identity/Account/Login`);

      // 1. LOGIN
      cy.get('input[name="Input.Email"]').type(CUSTOMER_USER);
      cy.get('input[id="passwordInput"]').type(CUSTOMER_PASS);

      cy.get('form#account').find('button[type="submit"]').click();

      // Verificamos que NO estemos en Login
      cy.url().should('not.include', '/Login');
    });
  });

  it('Debería permitir a un usuario logueado comprar un producto', () => {

    // 2. AGREGAR PRODUCTO (Usamos un producto específico, ID=1)
    cy.visit(`${BASE_URL}/Products/Details/1`);

    // Verifica si el formulario de agregar existe y hace click
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
    // Esto asume que el botón lleva directamente a /Checkout/Confirm
    cy.get('form[action*="/Checkout/Confirm"]').find('button[type="submit"]').click();

    // 5. CONFIRMAR COMPRA: LLENAR DATOS DE ENVÍO OBLIGATORIOS
    cy.url().should('include', '/Checkout/Confirm', { timeout: 10000 });

    // A. Seleccionar "Envío a Domicilio" o "Recojo en Tienda" si es necesario
    // Basado en tu HTML, parece que por defecto está seleccionado "Envío a Domicilio" o usa un botón de radio.
    // Si no hay un radio/checkbox para seleccionar, omitimos este paso y solo llenamos los campos.

    // B. Llenar campos de destinatario (los campos tienen ID sin 'Input_' en tu caso)
    // Usamos .clear().type() para asegurar que no haya texto previo
    cy.get('input#FullName').clear().type(SHIPPING_DATA.fullName);
    cy.get('input#Address').clear().type(SHIPPING_DATA.address);
    cy.get('input#PostalCode').clear().type(SHIPPING_DATA.postalCode);
    cy.get('input#PhoneNumber').clear().type(SHIPPING_DATA.phone);
    // El campo 'Ciudad' parece ser un dropdown o campo no obligatorio, lo omitimos si no es necesario.
    // El campo 'Instrucciones' (SpecialInstructions) es opcional.

    // C. Clic en "Ir a Pagar"
    cy.contains('button', /Ir a Pagar/i).click();

    // 6. LLENAR FORMULARIO DE PAGO (Simulación)
    // Esperamos a llegar a la simulación
    cy.url().should('include', '/Checkout/PaymentSimulation', { timeout: 10000 });

    // Tarjeta de prueba
    cy.get('input#CardNumber').clear().type('4111111111111111');
    cy.get('input#Expiration').clear().type('12/28');
    cy.get('input#CVV').clear().type('123');

    // 7. ENVIAR PAGO
    // Usamos la variante de selector por texto para evitar ambigüedad.
    cy.contains('button', /Pagar/i).click();

    // 8. VERIFICAR ÉXITO
    cy.url().should('include', '/Checkout/Success', { timeout: 10000 });

    // Verificaciones visuales
    cy.get('h1').should('contain', 'Compra Exitosa');

    cy.log('🎉 ¡Prueba de compra completada exitosamente!');
  });
});