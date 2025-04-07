using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace back.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "SYSTEM");

            migrationBuilder.CreateTable(
                name: "AGENCES",
                schema: "SYSTEM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NUMERO = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    NOM = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AGENCES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "FOURNITURES",
                schema: "SYSTEM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOM = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    DATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    PRIX_UNITAIRE = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QUANTITE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    PRIX_TOTAL = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QUANTITE_RESTANTE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MONTANT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CATEGORIE = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FOURNITURES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "USERS",
                schema: "SYSTEM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOM = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    PRENOM = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    MOT_DE_PASSE = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    AGENCE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_USERS_AGENCES_AGENCE_ID",
                        column: x => x.AGENCE_ID,
                        principalSchema: "SYSTEM",
                        principalTable: "AGENCES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AGENCE_FOURNITURE",
                schema: "SYSTEM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    AGENCE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FOURNITURE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DATE_ASSOCIATION = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AGENCE_FOURNITURE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_AGENCE_FOURNITURE_AGENCES_AGENCE_ID",
                        column: x => x.AGENCE_ID,
                        principalSchema: "SYSTEM",
                        principalTable: "AGENCES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AGENCE_FOURNITURE_FOURNITURES_FOURNITURE_ID",
                        column: x => x.FOURNITURE_ID,
                        principalSchema: "SYSTEM",
                        principalTable: "FOURNITURES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AGENCE_FOURNITURE_AGENCE_ID",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE",
                column: "AGENCE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_AGENCE_FOURNITURE_FOURNITURE_ID",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE",
                column: "FOURNITURE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_AGENCES_NUMERO",
                schema: "SYSTEM",
                table: "AGENCES",
                column: "NUMERO",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_USERS_AGENCE_ID",
                schema: "SYSTEM",
                table: "USERS",
                column: "AGENCE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_EMAIL",
                schema: "SYSTEM",
                table: "USERS",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AGENCE_FOURNITURE",
                schema: "SYSTEM");

            migrationBuilder.DropTable(
                name: "USERS",
                schema: "SYSTEM");

            migrationBuilder.DropTable(
                name: "FOURNITURES",
                schema: "SYSTEM");

            migrationBuilder.DropTable(
                name: "AGENCES",
                schema: "SYSTEM");
        }
    }
}
