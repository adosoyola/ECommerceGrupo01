describe('Flujo de Compra Completo', () => {
  it('Debería permitir a un usuario logueado comprar un producto', () => {

    // 1. Login
    cy.visit('/Identity/Account/Login');
    cy.get('#Input_Email').type('prueba@gmail.com');
    cy.get('#Input_Password').type('Prueba123@');
    cy.get('form').submit();
    cy.url().should('include', '/Cart');

    // 2. Agregar producto
    cy.visit('/Products/Details/1');
    cy.get('form[action="/Cart/Add"] button[type="submit"]').click();

    // 3. Ir a checkout
    cy.get('form[action="/Checkout/Confirm"] button[type="submit"]').click();

    // 4. Llenar formulario
    cy.url().should('include', '/Checkout/PaymentSimulation');
    cy.get('#CardNumber').clear().type('4111111111111111');
    cy.get('#Expiration').clear().type('12/28');
    cy.get('#CVV').clear().type('123');

    // 5. Enviar pago
    cy.get('form button[type="submit"]').click();

    // 6. Verificar éxito (cualquier página que contenga el mensaje)
    cy.get('body', { timeout: 10000 }).should(($body) => {
      // Verificar que estamos en una página de éxito
      expect($body).to.satisfy(($el) => {
        const text = $el.text();
        return text.includes('Compra Exitosa') ||
          text.includes('exitoso') ||
          text.includes('Success') ||
          $el.find('h1:contains("Compra Exitosa")').length > 0;
      }, 'Se esperaba mensaje de compra exitosa');
    });

    // 7. Verificar elementos específicos
    cy.get('h1').should('contain', 'Compra Exitosa');
    cy.get('.alert-success').should('contain', 'exitoso');

    cy.log('🎉 ¡Prueba completada exitosamente!');
  });
});