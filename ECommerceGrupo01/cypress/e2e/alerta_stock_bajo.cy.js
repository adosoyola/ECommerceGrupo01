describe('Integración: Alertas de Stock y Restauración', () => {

  // --- CONFIGURACIÓN DEL LOGIN ---
  beforeEach(() => {
    cy.session('adminSession', () => {
      cy.visit('/Identity/Account/Login');
      cy.get('input[name="Input.Email"]').clear().type('admin@ecommerce.com');
      cy.get('input[name="Input.Password"]').clear().type('Admin123!');
      cy.get('button[type="submit"]').click();
      cy.url().should('not.include', '/Login');
    },
    {
      validate: () => {
        cy.getCookie('.AspNetCore.Identity.Application').should('exist');
      }
    });

    cy.visit('/Admin/Products');
  });

  it('Debe generar alerta de stock (3) y luego corregirlo a (10)', () => {
    
    // --- PARTE 1: BAJAR STOCK A 3 ---
    cy.get('table tbody tr').first().find('td').eq(0).then(($celdaNombre) => {
        const nombreProducto = $celdaNombre.text().trim();
        cy.log(`--- PROCESO PARA: ${nombreProducto} ---`);

        // 1. Editar para provocar alerta
        cy.get('table tbody tr').first().find('a[href*="Edit"]').click();
        cy.url().should('include', '/Edit');
        cy.get('input[name="Stock"]').clear().type('3');
        cy.get('button[type="submit"]').click(); // Guarda y vuelve a Index

        // 2. Verificar Alerta en Reportes
        cy.log('--- Verificando Alerta en Reportes ---');
        cy.visit('/Admin/Reports');
        cy.contains('tr', nombreProducto).within(() => {
            // Verifica que diga "3" y tenga el texto de advertencia
            cy.get('td').should('contain.text', '3');
            cy.get('td').should('contain.text', 'Bajo');
        });

        // --- PARTE 2: RESTAURAR STOCK A 10 ---
        cy.log('--- Restaurando Stock a 10 ---');
        
        // 3. Volver a Productos
        cy.visit('/Admin/Products');

        // 4. Buscar el MISMO producto y volver a editarlo
        // Usamos .contains para asegurarnos de editar el correcto
        cy.contains('tr', nombreProducto).find('a[href*="Edit"]').click();

        // 5. Cambiar Stock a 10
        cy.url().should('include', '/Edit');
        cy.get('input[name="Stock"]')
          .clear()
          .type('50'); // Stock saludable
        
        cy.get('button[type="submit"]').click();

        // 6. Verificar que se arregló (ya no debe decir "Bajo")
        cy.url().should('include', '/Admin/Products');
        
        // Opcional: Ir a reportes una última vez para confirmar que está verde/normal
        cy.visit('/Admin/Reports');
        cy.contains('tr', nombreProducto).within(() => {
            // Debe decir 10
            cy.get('td').should('contain.text', '50');
            // Y YA NO debe decir "Bajo"
            cy.get('td').should('not.contain.text', 'Bajo');
        });
        
        cy.log('¡Prueba completada! Stock restaurado exitosamente.');
    });
  });
});