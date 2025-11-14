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

      // ✅ Selector Específico: Apuntar al formulario de login por su ID
      cy.get('form#account').find('button[type="submit"]').click();

      // ✅ CORRECCIÓN: No obligamos a ir al Cart.
      // Solo verificamos que NO estemos en Login (significa que entró)
      // O verificamos que aparezca el menú de usuario
      cy.url().should('not.include', '/Login');
    });
  });

  it('Debería permitir a un usuario logueado comprar un producto', () => {

    // 2. AGREGAR PRODUCTO 
    // Visitamos un detalle de producto (Asumimos que ID=1 existe, si no, cambia el número)
    cy.visit(`${BASE_URL}/Products/Details/1`);

    // ✅ Selector Específico: Formulario de "Agregar al Carrito"
    // Usamos cy.get('body') para asegurar que la página cargó
    cy.get('body').then(($body) => {
      if ($body.find('form[action*="/Cart/Add"]').length > 0) {
        cy.get('form[action*="/Cart/Add"]').find('button[type="submit"]').click();
      } else {
        // Fallback si no encuentra el form (útil para depurar)
        cy.log('No se encontró el formulario de agregar al carrito');
      }
    });

    // 3. VERIFICAR CARRITO
    // Ahora sí deberíamos estar en el carrito
    cy.url().should('include', '/Cart');

    // 4. IR A CONFIRMAR (CHECKOUT)
    // ✅ Selector Específico: Botón que lleva a Confirmar
    cy.get('form[action*="/Checkout/Confirm"]').find('button[type="submit"]').click();

    // 5. CONFIRMAR COMPRA (Si hay paso intermedio de confirmación)
    // A veces hay una pantalla intermedia antes de la simulación.
    // Si tu flujo va directo a Simulation, este paso pasará rápido.
    cy.url().then(($url) => {
      if ($url.includes('/Checkout/Confirm')) {
        // Estamos en la pantalla de "Confirmar Compra" (la tabla resumen)
        // Buscamos el botón de confirmar final
        cy.get('form[action*="ProcessPaymentSimulation"]') // A veces el Confirm manda directo al Process
          .find('button[type="submit"]')
          .click();
        // NOTA: Si tu vista Confirm.cshtml tiene un botón que lleva a PaymentSimulation, ajusta aquí.
        // Basado en tus archivos, Confirm.cshtml hace POST a PaymentSimulation? No, suele ir al controlador.
        // Si Confirm.cshtml tiene un form que va a 'PaymentSimulation', úsalo.
        // Si Confirm.cshtml tiene un form que va a 'Confirm' (POST), úsalo.

        // Vamos a intentar ser genéricos para avanzar:
        cy.get('button[type="submit"]').last().click();
      }
    });

    // 6. LLENAR FORMULARIO DE PAGO (Simulación)
    // Esperamos a llegar a la simulación
    cy.url().should('include', '/Checkout/PaymentSimulation');

    // Tarjeta de prueba
    cy.get('input#CardNumber').clear().type('4111111111111111');
    cy.get('input#Expiration').clear().type('12/28');
    cy.get('input#CVV').clear().type('123');

    // 7. ENVIAR PAGO
    // ✅ SOLUCIÓN AMBIGÜEDAD: Usar el selector específico para el formulario de pago
    // El formulario en PaymentSimulation.cshtml suele enviar a ProcessPaymentSimulation
    cy.get('form[action*="ProcessPaymentSimulation"]')
      .find('button[type="submit"]')
      .click();

    // 8. VERIFICAR ÉXITO
    cy.url().should('include', '/Checkout/Success');

    // Verificaciones visuales
    cy.get('h1').should('contain', 'Compra Exitosa');
    cy.get('.alert-success').should('contain', 'exitoso');

    cy.log('🎉 ¡Prueba de compra completada exitosamente!');
  });
});