describe('Integración: Alertas de Stock Bajo', () => {

    // CONFIGURACIÓN
    // Usa el puerto 5012 ya que estás usando 'dotnet run'
    const BASE_URL = 'http://localhost:5012'; 

    const ADMIN_USER = 'admin@ecommerce.com';
    const ADMIN_PASS = 'Admin123!';

    beforeEach(() => {
        // Iniciamos sesión antes de cada prueba
        cy.session('adminSession', () => {
            cy.visit(`${BASE_URL}/Identity/Account/Login`);
            cy.get('input[name="Input.Email"]').type(ADMIN_USER);
            cy.get('input[id="passwordInput"]').type(ADMIN_PASS);
            cy.get('form#account button[type="submit"]').click();
            
            // Verificamos que login fue exitoso
            cy.url().should('not.include', '/Login');
        });
    });

    it('Debe forzar un stock bajo (3 unidades) y verificar la alerta visual roja', () => {
        
        // 1. Ir al listado de productos
        cy.visit(`${BASE_URL}/Admin/Products`);

        // 2. MODIFICAR DATOS: Editar el primer producto para que tenga Stock = 3
        cy.log('--- PASO 1: Forzando Stock Bajo ---');
        
        // Entramos al primer botón de "Editar" que encontremos
        cy.get('table tbody tr').first().find('a[href*="Edit"]').click();

        // Verificamos estar en la pantalla de edición
        cy.url().should('include', '/Edit');

        // Cambiamos el stock a 3
        // NOTA: Si tu input se llama diferente, cambia 'input[name="Stock"]'
        cy.get('input[name="Stock"]').clear().type('3');
        cy.get('button[type="submit"]').click();

        // 3. VERIFICAR RESULTADO: El sistema debe alertarnos
        cy.log('--- PASO 2: Verificando Alerta Visual ---');
        
        // Debemos haber regresado a la lista
        cy.url().should('include', '/Admin/Products');

        // Buscamos la fila que tiene el número "3" en la columna de stock
        cy.contains('td', '3').parent('tr').should(($row) => {
            
            // Obtenemos el HTML de esa fila para analizarlo
            const claseFila = $row.attr('class');
            const contenidoHtml = $row.html();

            // Imprimimos en consola para depurar si falla
            console.log('Clases encontradas:', claseFila);
            console.log('HTML encontrado:', contenidoHtml);

            // Validaciones: Buscamos la clase roja O el badge de alerta
            const tieneClaseRoja = claseFila && claseFila.includes('table-danger');
            const tieneBadge = contenidoHtml.includes('Bajo Stock');

            // La aserción final
            expect(tieneClaseRoja || tieneBadge, 
                '¡FALLO! La fila debería ser roja (table-danger) o tener un aviso de "Bajo Stock"'
            ).to.be.true;
        });
    });
});