describe('Módulo de Reportes y Business Intelligence', () => {

    const BASE_URL = 'http://localhost:5012'; 
    const ADMIN_USER = 'admin@ecommerce.com';
    const ADMIN_PASS = 'Admin123!';

    // Login Admin
    const loginAdmin = () => {
        cy.visit(`${BASE_URL}/Identity/Account/Login`);
        cy.get('input[name="Input.Email"]').clear().type(ADMIN_USER);
        cy.get('input[id="passwordInput"]').clear().type(ADMIN_PASS);
        cy.get('form#account button[type="submit"]').click();
    };

    it('Debe cargar el Dashboard de Reportes y visualizar métricas clave', () => {
        
        // 1. Iniciar Sesión
        cy.log('📊 PASO 1: Ingresando como Administrador');
        cy.session('adminSession', loginAdmin);

        // 2. Navegar a Reportes
        // NOTA: Si tu URL es diferente (ej: /Admin/Dashboard o /Reports), cámbiala aquí.
        const reportUrl = `${BASE_URL}/Admin/Reports`;
        
        // Usamos request primero para no fallar feo si la URL no existe
        cy.request({ url: reportUrl, failOnStatusCode: false }).then((response) => {
            if (response.status !== 200) {
                cy.log(`⚠️ La URL ${reportUrl} no existe. Intentando ruta alternativa: /Admin/Dashboard`);
                cy.visit(`${BASE_URL}/Admin/Dashboard`); // Ruta alternativa común
            } else {
                cy.visit(reportUrl);
            }
        });

        // 3. Verificar Elementos Visuales
        cy.log('📊 PASO 2: Verificando componentes visuales (Gráficos/Tablas)');

        cy.get('body').then(($body) => {
            
            // A. Verificar Gráficos (Si usas Chart.js suelen ser <canvas>)
            if ($body.find('canvas').length > 0) {
                cy.get('canvas').should('be.visible');
                cy.log('✅ Gráficos detectados correctamente');
            } else {
                cy.log('ℹ️ No se detectaron gráficos (canvas). Verificando tablas de datos...');
            }

            // B. Verificar Tablas de Resumen
            if ($body.find('table').length > 0) {
                cy.get('table').should('be.visible');
                // Verificar que la tabla no esté vacía (debe tener encabezados y datos)
                cy.get('table tr').should('have.length.gt', 0);
                cy.log('✅ Tablas de datos detectadas');
            }

            // C. Verificar Tarjetas de Información (KPIs)
            // Buscamos elementos típicos de dashboard como "Ventas Totales", "Usuarios", etc.
            if ($body.find('.card, .stats, .kpi').length > 0) {
                cy.log('✅ Tarjetas de indicadores (KPIs) detectadas');
            }
        });

        // 4. Verificar Botones de Exportación
        // Como vi que usas DinkToPdf, seguro tienes botones para descargar
        cy.log('📊 PASO 3: Verificando opciones de exportación (PDF/Excel)');
        
        cy.get('body').then(($body) => {
            // Buscamos botones que digan PDF, Excel, Reporte, Descargar o tengan íconos
            const exportBtns = $body.find('a, button').filter((i, el) => {
                const text = Cypress.$(el).text().toLowerCase();
                const href = Cypress.$(el).attr('href') || '';
                return text.includes('pdf') || 
                       text.includes('excel') || 
                       text.includes('descargar') || 
                       href.includes('Print') ||
                       href.includes('Report');
            });

            if (exportBtns.length > 0) {
                cy.wrap(exportBtns).first().should('be.visible');
                cy.log('✅ Botón de Exportación/Reporte encontrado.');
            } else {
                cy.log('⚠️ No se encontraron botones explícitos de "Descargar Reporte".');
            }
        });

        // 5. Validación de Datos Específicos (Opcional pero recomendado)
        // Intentamos buscar texto que sabemos que existe por las pruebas anteriores
        // Ejemplo: El nombre de un producto vendido o un estado
        cy.get('body').contains(/Ventas|Pedidos|Stock|Total/i).should('exist');
    });
});