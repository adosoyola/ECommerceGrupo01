// cypress/e2e/admin_flow_only.cy.js

// --- Constantes de Prueba ---
const BASE_URL = 'http://localhost:5012';

// Datos del Administrador
const ADMIN_USER = 'admin@ecommerce.com';
const ADMIN_PASS = 'Admin123!';

// Datos del Producto a crear (Nombre único cada vez para evitar conflictos)
const TEST_PRODUCT = {
    name: `Cypress Laptop Test ${Date.now()}`,
    price: 100.50,
    stock: 25,
    description: 'Un producto de prueba creado por Cypress.',
};

describe('Flujo de Administrador E2E - ECommerce SystemCusco', () => {

    // --- LOGIN DE ADMINISTRADOR (Se ejecuta antes de cada prueba) ---
    beforeEach(() => {
        cy.session('adminLogin', () => {
            cy.visit(`${BASE_URL}/Identity/Account/Login`);
            cy.get('input[name="Input.Email"]').type(ADMIN_USER);
            cy.get('input[id="passwordInput"]').type(ADMIN_PASS);

            // Selector específico: Botón dentro del formulario #account
            cy.get('form#account').find('button[type="submit"]').click();

            cy.url().should('eq', `${BASE_URL}/`);

            // Ir al Panel Admin
            cy.contains('a', 'Panel Admin').should('be.visible').click();
            cy.url().should('contain', '/Admin');
        });
    });

    // --- 1. GESTIÓN DE PRODUCTOS ---
    context('1. Flujo de Administrador - Gestión de Productos', () => {

        it('Debería crear un nuevo producto (CRUD - Create)', () => {
            cy.visit(`${BASE_URL}/Admin/Products/Create`);

            cy.get('input[name="Name"]').type(TEST_PRODUCT.name);
            cy.get('input[name="Price"]').type(TEST_PRODUCT.price);
            cy.get('input[name="Stock"]').type(TEST_PRODUCT.stock);
            cy.get('textarea[name="Description"]').type(TEST_PRODUCT.description);

            // ✅ SOLUCIÓN AMBIGÜEDAD: Usamos la clase 'btn-success' para identificar el botón Guardar
            cy.get('form[action*="/Admin/Products/Create"]')
                .find('button.btn-success')
                .click();

            // Verificamos redirección y que el producto aparezca en la lista
            cy.url().should('contain', '/Admin/Products');
            cy.contains('td', TEST_PRODUCT.name).should('be.visible');
        });

        it('Debería editar el producto (CRUD - Update)', () => {
            cy.visit(`${BASE_URL}/Admin/Products`);

            // 1. Clic en el botón "Editar" del producto
            cy.contains('td', TEST_PRODUCT.name)
                .parent('tr')
                .find('a[href*="/Admin/Products/Edit"]')
                .click();

            // 2. Cambiar el stock a 42
            cy.get('input[name="Stock"]').clear().type('42');

            // ✅ SOLUCIÓN AMBIGÜEDAD: El botón de editar usa 'btn-primary'
            cy.get('form[action*="/Admin/Products/Edit"]')
                .find('button.btn-primary')
                .click();

            // 3. Verificación adaptada a tu tabla (que no muestra stock):
            // Volvemos a entrar a "Editar" para verificar que el dato se guardó en la BD
            cy.visit(`${BASE_URL}/Admin/Products`);
            cy.contains('td', TEST_PRODUCT.name)
                .parent('tr')
                .find('a[href*="/Admin/Products/Edit"]')
                .click();

            // 4. Verificamos que el input tenga el valor '42'
            cy.get('input[name="Stock"]').should('have.value', '42');
        });
    });

    // --- 2. REPORTES ---
    context('2. Flujo de Administrador - Reportes', () => {
        it('Debería mostrar datos del producto creado en los Reportes', () => {
            cy.visit(`${BASE_URL}/Admin/Reports`);
            // Verificamos que cargue la página y aparezca el nombre del producto
            cy.contains('h2', 'Reportes').should('be.visible');
            cy.contains(TEST_PRODUCT.name).should('be.visible');
        });
    });

    // --- 3. GESTIÓN DE USUARIOS ---
    context('3. Flujo de Administrador - Gestión de Usuarios', () => {
        it('Debería poder crear y eliminar un usuario de prueba', () => {
            const NEW_USER_EMAIL = `testuser_${Date.now()}@example.com`;

            // 1. Crear usuario
            cy.visit(`${BASE_URL}/Admin/Users/Create`);
            cy.get('input[name="Email"]').type(NEW_USER_EMAIL);
            cy.get('input[id="passwordInput"]').type('TestPass123@');

            // ✅ SOLUCIÓN AMBIGÜEDAD: Botón guardar es 'btn-success'
            cy.get('form[action*="/Admin/Users/Create"]')
                .find('button.btn-success')
                .click();

            // Verificar creación en la tabla
            cy.url().should('contain', '/Admin/Users');
            cy.get('.alert-success').should('contain', 'Usuario creado');
            cy.contains('td', NEW_USER_EMAIL).should('be.visible');

            // 2. Eliminar usuario
            cy.contains('td', NEW_USER_EMAIL)
                .parent('tr')
                .find('a[href*="/Admin/Users/Delete"]')
                .click();

            cy.url().should('contain', '/Admin/Users/Delete');

            // ✅ SOLUCIÓN AMBIGÜEDAD: Botón eliminar es 'btn-danger'
            cy.get('form[action*="/Admin/Users/Delete"]')
                .find('button.btn-danger')
                .click();

            // Verificar eliminación
            cy.get('.alert-success').should('contain', 'Usuario eliminado');
            cy.contains('td', NEW_USER_EMAIL).should('not.exist');
        });
    });

    // --- 4. LIMPIEZA ---
    context('4. Flujo de Administrador - Limpieza', () => {
        it('Debería eliminar el producto de prueba (CRUD - Delete)', () => {
            cy.visit(`${BASE_URL}/Admin/Products`);

            // Buscar y hacer clic en Eliminar
            cy.contains('td', TEST_PRODUCT.name)
                .parent('tr')
                .find('a[href*="/Admin/Products/Delete"]')
                .click();

            cy.url().should('contain', '/Admin/Products/Delete');

            // ✅ SOLUCIÓN AMBIGÜEDAD: Botón eliminar es 'btn-danger'
            cy.get('form[action*="/Admin/Products/Delete"]')
                .find('button.btn-danger')
                .click();

            // Verificar que el producto ya no existe en la tabla
            cy.contains('td', TEST_PRODUCT.name).should('not.exist');
        });
    });
});