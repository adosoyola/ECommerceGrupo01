describe('Flujo de pago E2E con Stripe', () => {
  it('Debe iniciar sesión, agregar un producto y completar el pago exitosamente', () => {
    // 1. Inicia sesión en la aplicación.
    cy.visit('http://localhost:5012/Identity/Account/Login');
    cy.get('#Input_Email').type('prueba@gmail.com');
    cy.get('#Input_Password').type('Prueba123@');
    cy.get('button[type="submit"]').contains('Log in').click();

    // 2. Después del inicio de sesión, la aplicación redirige al carrito.
    cy.url().should('include', '/Cart');

    // 3. Navega explícitamente a la página de detalles de un producto para agregar un artículo.
    cy.visit('http://localhost:5012/Products/Details/1');

    // 4. Agrega el producto al carrito.
    cy.get('form[asp-action="Add"] button[type="submit"]').should('be.visible').click();

    // 5. La aplicación te redirigirá a la página del carrito. Espera a que la URL cambie.
    cy.url().should('include', '/Cart/Index');

    // 6. Haz clic en el botón para pagar con Stripe.
    cy.get('form[asp-controller="Payments"][asp-action="CreateCheckoutSession"] button[type="submit"]').click();

    // 7. Usa cy.origin() para ejecutar comandos en el dominio de Stripe.
    cy.origin('https://checkout.stripe.com', () => {
      // 8. Rellena los campos del formulario de pago con datos de prueba de Stripe.
      // Usa selectores robustos para los campos de Stripe.
      cy.get('#cardNumber').type('4242424242424242'); // Número de tarjeta de prueba
      cy.get('#cardExpiry').type('12/26');          // Fecha de expiración de prueba
      cy.get('#cardCvc').type('123');               // CVV de prueba

      // 9. Haz clic en el botón de pagar.
      cy.get('button[type="submit"]').click();
    });

    // 10. Después de completar el pago, la aplicación te redirigirá a tu dominio local.
    cy.url().should('include', '/Payments/Success');

    // 11. Valida el contenido de la página de confirmación.
    cy.contains('h2', '✅ Pago exitoso').should('be.visible');
  });
});