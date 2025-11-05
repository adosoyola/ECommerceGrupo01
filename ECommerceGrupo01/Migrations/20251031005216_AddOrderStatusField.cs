using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Migrations
{
    // El nombre de esta clase y el namespace dependen de tu proyecto
    public partial class AddOrderStatusField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ❌ SE ELIMINARON TODAS LAS LÍNEAS DE PRODUCTS, AlterColumn, CreateIndex y AddForeignKey
            // ❌ que causaron el error "column 'ImageUrl' does not exist".

            // ✅ ÚNICA INSTRUCCIÓN REQUERIDA: Añadir la columna Status a la tabla Orders
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Si la migración es revertida, solo se eliminará la columna Status.
            // Las instrucciones Drop de las otras columnas deben estar en sus migraciones originales.
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            // Si necesitas revertir los otros cambios (ImagePath, AlterColumn, etc.), 
            // asegúrate de que esa lógica esté en las migraciones que realmente la crearon.
            // Por ahora, dejamos solo la instrucción que revierte el cambio que hicimos.

            // Si tu método Down original tenía más instrucciones, debes dejarlas si son necesarias para 
            // revertir otros cambios *que sí se aplicaron anteriormente* en tu historial de migraciones.
            // Si no estás seguro, usa el método Down generado automáticamente por EF Core, 
            // pero el método Up debe verse como el de arriba.
        }
    }
}