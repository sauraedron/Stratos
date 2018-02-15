using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Stratos_alpha.Migrations
{
    public partial class statusadd : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DirectoryStatusSet",
                columns: table => new
                {
                    Path = table.Column<string>(nullable: false),
                    Direction = table.Column<string>(nullable: true),
                    Level = table.Column<long>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryStatusSet", x => x.Path);
                });

            migrationBuilder.CreateTable(
                name: "UbuntuFileSet",
                columns: table => new
                {
                    filePath = table.Column<string>(nullable: false),
                    fileHash = table.Column<string>(nullable: true),
                    fileName = table.Column<string>(nullable: true),
                    fileSize = table.Column<long>(nullable: false),
                    fileStatus = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UbuntuFileSet", x => x.filePath);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DirectoryStatusSet");

            migrationBuilder.DropTable(
                name: "UbuntuFileSet");
        }
    }
}
