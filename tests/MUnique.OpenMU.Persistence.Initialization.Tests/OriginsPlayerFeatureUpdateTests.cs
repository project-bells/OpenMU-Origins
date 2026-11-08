// <copyright file="OriginsPlayerFeatureUpdateTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.MuHelper;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests mandatory Origins updates for player-facing features.
/// </summary>
[TestFixture]
internal class OriginsPlayerFeatureUpdateTests
{
    /// <summary>
    /// Verifies that existing databases get the helper, cash shop, and unrestricted master experience.
    /// </summary>
    [Test]
    public async Task MandatoryUpdatesRepairExistingConfigurationAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var initialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await initialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        using var context = contextProvider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var muHelper = configuration.PlugInConfigurations.Single(c => c.TypeId == typeof(MuHelperFeaturePlugIn).GUID);
        var cashShop = configuration.PlugInConfigurations.Single(c => c.TypeId == typeof(CashShopFeaturePlugIn).GUID);
        muHelper.IsActive = false;
        cashShop.IsActive = false;
        configuration.MinimumMonsterLevelForMasterExperience = 95;

        await new EnablePlayerAutomationAndCashShopUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await new EnableMasterExperienceOnAllMonstersUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(muHelper.IsActive, Is.True);
            Assert.That(cashShop.IsActive, Is.True);
            Assert.That(configuration.MinimumMonsterLevelForMasterExperience, Is.Zero);
        });
    }
}
