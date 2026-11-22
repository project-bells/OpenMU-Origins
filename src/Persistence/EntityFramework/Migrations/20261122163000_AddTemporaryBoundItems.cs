// <copyright file="20261122163000_AddTemporaryBoundItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework.Migrations;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

/// <summary>
/// Adds lifecycle and binding metadata for temporary Item Mall rewards.
/// </summary>
[DbContext(typeof(EntityDataContext))]
[Migration("20261122163000_AddTemporaryBoundItems")]
public sealed class AddTemporaryBoundItems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ExpiresAt",
            schema: "data",
            table: "Item",
            type: "timestamp with time zone",
            nullable: true);
        migrationBuilder.AddColumn<bool>(
            name: "IsBound",
            schema: "data",
            table: "Item",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ExpiresAt", schema: "data", table: "Item");
        migrationBuilder.DropColumn(name: "IsBound", schema: "data", table: "Item");
    }
}
