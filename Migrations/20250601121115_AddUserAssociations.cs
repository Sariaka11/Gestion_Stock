using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace back.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAssociations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_USERS_AGENCES_AGENCE_ID",
            //    schema: "SYSTEM",
            //    table: "USERS");

            //migrationBuilder.DropIndex(
            //    name: "IX_USERS_AGENCE_ID",
            //    schema: "SYSTEM",
            //    table: "USERS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AGENCE_FOURNITURE",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE");

            migrationBuilder.DropIndex(
                name: "IX_AGENCE_FOURNITURE_AGENCE_ID",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE");

            //migrationBuilder.DropColumn(
            //    name: "AGENCE_ID",
            //    schema: "SYSTEM",
            //    table: "USERS");

            migrationBuilder.DropColumn(
                name: "DATE",
                schema: "SYSTEM",
                table: "FOURNITURES");

            migrationBuilder.DropColumn(
                name: "MONTANT",
                schema: "SYSTEM",
                table: "FOURNITURES");

            migrationBuilder.DropColumn(
                name: "PRIX_TOTAL",
                schema: "SYSTEM",
                table: "FOURNITURES");

            migrationBuilder.DropColumn(
                name: "QUANTITE",
                schema: "SYSTEM",
                table: "FOURNITURES");

            migrationBuilder.RenameTable(
                name: "AGENCES",
                schema: "SYSTEM",
                newName: "AGENCES");

            migrationBuilder.AddColumn<string>(
                name: "FONCTION",
                schema: "SYSTEM",
                table: "USERS",
                type: "NVARCHAR2(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AddColumn<int>(
                name: "QUANTITE",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AGENCE_FOURNITURE",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE",
                columns: new[] { "AGENCE_ID", "FOURNITURE_ID" });

            migrationBuilder.CreateTable(
                name: "CATEGORIES",
                columns: table => new
                {
                    ID_CATEGORIE = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOM_CATEGORIE = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    DUREE_AMORTISSEMENT = table.Column<int>(type: "NUMBER(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORIES", x => x.ID_CATEGORIE);
                });

            migrationBuilder.CreateTable(
                name: "ENTREE_FOURNITURES",
                schema: "SYSTEM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    FOURNITURE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    QUANTITE_ENTREE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DATE_ENTREE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    PRIX_UNITAIRE = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MONTANT = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENTREE_FOURNITURES", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ENTREE_FOURNITURES_FOURNITURES_FOURNITURE_ID",
                        column: x => x.FOURNITURE_ID,
                        principalSchema: "SYSTEM",
                        principalTable: "FOURNITURES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_AGENCE",
                schema: "SYSTEM",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    USER_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AGENCE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DATE_ASSOCIATION = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_AGENCE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_USER_AGENCE_AGENCES_AGENCE_ID",
                        column: x => x.AGENCE_ID,
                        principalTable: "AGENCES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_AGENCE_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalSchema: "SYSTEM",
                        principalTable: "USERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "USER_FOURNITURE",
                schema: "SYSTEM",
                columns: table => new
                {
                    USER_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    FOURNITURE_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DATE_ASSOCIATION = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_FOURNITURE", x => new { x.USER_ID, x.FOURNITURE_ID });
                    table.ForeignKey(
                        name: "FK_USER_FOURNITURE_FOURNITURES_FOURNITURE_ID",
                        column: x => x.FOURNITURE_ID,
                        principalSchema: "SYSTEM",
                        principalTable: "FOURNITURES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_USER_FOURNITURE_USERS_USER_ID",
                        column: x => x.USER_ID,
                        principalSchema: "SYSTEM",
                        principalTable: "USERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IMMOBILISATIONS",
                columns: table => new
                {
                    ID_BIEN = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOM_BIEN = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    CODE_BARRE = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    VALEUR_ACQUISITION = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    TAUX_AMORTISSEMENT = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    QUANTITE = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    STATUT = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    ID_CATEGORIE = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    DATE_ACQUISITION = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IMMOBILISATIONS", x => x.ID_BIEN);
                    table.ForeignKey(
                        name: "FK_IMMOBILISATIONS_CATEGORIES_ID_CATEGORIE",
                        column: x => x.ID_CATEGORIE,
                        principalTable: "CATEGORIES",
                        principalColumn: "ID_CATEGORIE");
                });

            migrationBuilder.CreateTable(
                name: "AMORTISSEMENTS",
                columns: table => new
                {
                    ID_AMORTISSEMENT = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ID_BIEN = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ANNEE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    MONTANT = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    VALEUR_RESIDUELLE = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                    DATE_CALCUL = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AMORTISSEMENTS", x => x.ID_AMORTISSEMENT);
                    table.ForeignKey(
                        name: "FK_AMORTISSEMENTS_IMMOBILISATIONS_ID_BIEN",
                        column: x => x.ID_BIEN,
                        principalTable: "IMMOBILISATIONS",
                        principalColumn: "ID_BIEN",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BIEN_AGENCE",
                columns: table => new
                {
                    ID_BIEN = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ID_AGENCE = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    DATE_AFFECTATION = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    QUANTITE = table.Column<int>(type: "NUMBER(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BIEN_AGENCE", x => new { x.ID_BIEN, x.ID_AGENCE, x.DATE_AFFECTATION });
                    table.ForeignKey(
                        name: "FK_BIEN_AGENCE_AGENCES_ID_AGENCE",
                        column: x => x.ID_AGENCE,
                        principalTable: "AGENCES",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BIEN_AGENCE_IMMOBILISATIONS_ID_BIEN",
                        column: x => x.ID_BIEN,
                        principalTable: "IMMOBILISATIONS",
                        principalColumn: "ID_BIEN",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AMORTISSEMENTS_ID_BIEN_ANNEE",
                table: "AMORTISSEMENTS",
                columns: new[] { "ID_BIEN", "ANNEE" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BIEN_AGENCE_ID_AGENCE",
                table: "BIEN_AGENCE",
                column: "ID_AGENCE");

            migrationBuilder.CreateIndex(
                name: "IX_ENTREE_FOURNITURES_FOURNITURE_ID",
                schema: "SYSTEM",
                table: "ENTREE_FOURNITURES",
                column: "FOURNITURE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_IMMOBILISATIONS_ID_CATEGORIE",
                table: "IMMOBILISATIONS",
                column: "ID_CATEGORIE");

            migrationBuilder.CreateIndex(
                name: "IX_USER_AGENCE_AGENCE_ID",
                schema: "SYSTEM",
                table: "USER_AGENCE",
                column: "AGENCE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_AGENCE_USER_ID",
                schema: "SYSTEM",
                table: "USER_AGENCE",
                column: "USER_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USER_FOURNITURE_FOURNITURE_ID",
                schema: "SYSTEM",
                table: "USER_FOURNITURE",
                column: "FOURNITURE_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AMORTISSEMENTS");

            migrationBuilder.DropTable(
                name: "BIEN_AGENCE");

            migrationBuilder.DropTable(
                name: "ENTREE_FOURNITURES",
                schema: "SYSTEM");

            migrationBuilder.DropTable(
                name: "USER_AGENCE",
                schema: "SYSTEM");

            migrationBuilder.DropTable(
                name: "USER_FOURNITURE",
                schema: "SYSTEM");

            migrationBuilder.DropTable(
                name: "IMMOBILISATIONS");

            migrationBuilder.DropTable(
                name: "CATEGORIES");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AGENCE_FOURNITURE",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE");

            migrationBuilder.DropColumn(
                name: "FONCTION",
                schema: "SYSTEM",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "QUANTITE",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE");

            migrationBuilder.RenameTable(
                name: "AGENCES",
                newName: "AGENCES",
                newSchema: "SYSTEM");

            migrationBuilder.AddColumn<int>(
                name: "AGENCE_ID",
                schema: "SYSTEM",
                table: "USERS",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DATE",
                schema: "SYSTEM",
                table: "FOURNITURES",
                type: "TIMESTAMP(7)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "MONTANT",
                schema: "SYSTEM",
                table: "FOURNITURES",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PRIX_TOTAL",
                schema: "SYSTEM",
                table: "FOURNITURES",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "QUANTITE",
                schema: "SYSTEM",
                table: "FOURNITURES",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "ID",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AGENCE_FOURNITURE",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE",
                column: "ID");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_AGENCE_ID",
                schema: "SYSTEM",
                table: "USERS",
                column: "AGENCE_ID");

            migrationBuilder.CreateIndex(
                name: "IX_AGENCE_FOURNITURE_AGENCE_ID",
                schema: "SYSTEM",
                table: "AGENCE_FOURNITURE",
                column: "AGENCE_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_USERS_AGENCES_AGENCE_ID",
                schema: "SYSTEM",
                table: "USERS",
                column: "AGENCE_ID",
                principalSchema: "SYSTEM",
                principalTable: "AGENCES",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
