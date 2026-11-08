// <copyright file="EnablePlayerAutomationAndCashShopUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.MuHelper;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enables the player-facing MU Helper and cash shop features in existing Origins databases.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("4A9BEE5C-6C8D-44E9-83F7-9F81554E5168")]
public sealed class EnablePlayerAutomationAndCashShopUpdatePlugIn : UpdatePlugInBase
{
    private const string PlugInName = "Enable MU Helper and cash shop";
    private const string PlugInDescription = "Enables the MU Helper and in-game cash shop with their default settings.";

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        EnableFeature<MuHelperFeaturePlugIn, MuHelperConfiguration>(context, gameConfiguration, new MuHelperConfiguration());
        EnableFeature<CashShopFeaturePlugIn, CashShopSettings>(context, gameConfiguration, new CashShopSettings());
        return ValueTask.CompletedTask;
    }

    private static void EnableFeature<TPlugIn, TConfiguration>(
        IContext context,
        GameConfiguration gameConfiguration,
        TConfiguration defaultConfiguration)
        where TPlugIn : IFeaturePlugIn
    {
        var typeId = typeof(TPlugIn).GUID;
        if (gameConfiguration.PlugInConfigurations.FirstOrDefault(configuration => configuration.TypeId == typeId) is not { } plugInConfiguration)
        {
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        if (string.IsNullOrWhiteSpace(plugInConfiguration.CustomConfiguration))
        {
            plugInConfiguration.SetConfiguration(defaultConfiguration, null);
        }
    }
}
