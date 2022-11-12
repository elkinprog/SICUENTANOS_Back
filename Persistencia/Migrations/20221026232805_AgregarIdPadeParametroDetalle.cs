using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistencia.Migrations
{
    public partial class AgregarIdPadeParametroDetalle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.AddColumn<long>(
                name: "IdPadre",
                table: "ParametroDetalle",
                type: "bigint",
                nullable: true);

           
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdPadre",
                table: "ParametroDetalle");

          
        }
    }
}
