using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AwesomeNetwork.Migrations
{
    public partial class CreateUserFriendsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Оставляем ТОЛЬКО таблицу UserFriends!
            migrationBuilder.CreateTable(
                name: "UserFriends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false) // Первичный ключ
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false), // Инициатор дружбы
                    CurrentFriendId = table.Column<string>(type: "nvarchar(450)", nullable: false) // Друг
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFriends", x => x.Id);

                    // Внешние ключи на AspNetUsers
                    table.ForeignKey(
                        name: "FK_UserFriends_AspNetUsers_CurrentFriendId",
                        column: x => x.CurrentFriendId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // Защита от удаления друга

                    table.ForeignKey(
                        name: "FK_UserFriends_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade); // Удалять дружбу при удалении инициатора
                });

            // Добавим индексы по обеим колонкам для быстрого поиска друзей
            migrationBuilder.CreateIndex(
                name: "IX_UserFriends_CurrentFriendId",
                table: "UserFriends",
                column: "CurrentFriendId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFriends_UserId",
                table: "UserFriends",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // При откате просто удаляем нашу таблицу и её индексы
            migrationBuilder.DropIndex(name: "IX_UserFriends_UserId", table: "UserFriends");
            migrationBuilder.DropIndex(name: "IX_UserFriends_CurrentFriendId", table: "UserFriends");

            migrationBuilder.DropTable(
                name: "UserFriends");
        }
    }
}