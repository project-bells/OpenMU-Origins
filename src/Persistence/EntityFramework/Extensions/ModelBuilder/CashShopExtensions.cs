// <copyright file="CashShopExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework.Extensions.ModelBuilder;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MUnique.OpenMU.Persistence.EntityFramework.Model;

/// <summary>
/// Extensions for cash shop related <see cref="EntityTypeBuilder"/>s.
/// </summary>
internal static class CashShopExtensions
{
    /// <summary>
    /// Applies the settings for the <see cref="CashShopPackage"/> entity.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static void Apply(this EntityTypeBuilder<CashShopPackage> builder)
    {
        builder.Property(package => package.Name).HasConversion(LocalizedStringConverter.Instance);
    }

    /// <summary>
    /// Applies the settings for the <see cref="CashShopStorageItem"/> entity.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static void Apply(this EntityTypeBuilder<CashShopStorageItem> builder)
    {
        builder.HasIndex(item => item.AccountId);
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(item => item.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Applies the settings for the <see cref="CashShopItemGrant"/> entity.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static void Apply(this EntityTypeBuilder<CashShopItemGrant> builder)
    {
        builder.Property(grant => grant.Reference).HasMaxLength(128);
        builder.Property(grant => grant.Reason).HasMaxLength(256);
        builder.Property(grant => grant.GrantedBy).HasMaxLength(128);
        builder.HasIndex(grant => grant.Reference).IsUnique();
        builder.HasIndex(grant => new { grant.AccountId, grant.CreatedAt });
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(grant => grant.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Applies the settings for the <see cref="CashShopCoinGrant"/> entity.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static void Apply(this EntityTypeBuilder<CashShopCoinGrant> builder)
    {
        builder.HasIndex(grant => new { grant.AccountId, grant.AppliedAt });
        builder.HasIndex(grant => grant.Reference).IsUnique();
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(grant => grant.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Applies the settings for the <see cref="WebShopAccountOperation"/> entity.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static void Apply(this EntityTypeBuilder<WebShopAccountOperation> builder)
    {
        builder.Property(operation => operation.Reference).HasMaxLength(128);
        builder.Property(operation => operation.CharacterName).HasMaxLength(10);
        builder.Property(operation => operation.NewCharacterName).HasMaxLength(10);
        builder.Property(operation => operation.RequestedBy).HasMaxLength(128);
        builder.Property(operation => operation.FailureCode).HasMaxLength(64);
        builder.HasIndex(operation => operation.Reference).IsUnique();
        builder.HasIndex(operation => new { operation.AccountId, operation.AppliedAt });
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(operation => operation.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
