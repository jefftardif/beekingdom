namespace BeeKingdom.HiveOperations;

// M055-CL - Progression "Palais Royal" (Coeur royal).
//
// SOURCE DE VERITE : le niveau du Palais Royal EST le niveau du batiment
// `administration_core` deja stocke dans PlayerHiveState.BuildingLevels et deja
// fait avancer par BuildingUpgradeService.CompleteAsync. Ce fichier n'introduit
// AUCUN second compteur : il ne fait qu'interpreter ce niveau existant comme le
// niveau principal de la colonie, et y attacher des prerequis / deblocages.
//
// De la meme facon, les COUTS et les DUREES ne sont PAS redefinis ici : ils
// restent exclusivement dans BuildingUpgradeOptions.Catalog (appsettings), la ou
// vivent deja ceux de tous les autres batiments. Cette configuration-ci ne porte
// que ce qui n'existait pas encore : prerequis inter-batiments et deblocages.
public static class RoyalPalaceProgressionKeys
{
    // Identifiant interne reel du Palais Royal cote serveur et cote persistance.
    // Cote Unity il est expose sous BuildingTypes.RoyalPalace ("ROYAL_PALACE") et
    // traduit vers cette cle par BuildingMappingTable (LegacyKey AdministrationCore).
    public const string RoyalPalaceBuildingKey = "administration_core";

    // Niveau implicite d'un batiment jamais ameliore - meme convention que
    // BuildingUpgradeService (GetValueOrDefault(buildingKey, 1)).
    public const int ImplicitStartingLevel = 1;
}

// Prerequis : un batiment doit avoir atteint au moins ce niveau pour que
// l'amelioration vers `Level` soit autorisee.
//
// NOTE DE LIAISON (importante) : ces trois types de CONFIGURATION sont des classes a
// proprietes settables avec constructeur sans parametre, et non des records positionnels.
// Microsoft.Extensions.Configuration ne sait pas remplir des records positionnels imbriques
// a deux niveaux (Levels[].UpgradeRequirements[]) : il construisait des instances vides que
// Validate() rejetait ensuite au demarrage. Les types de LECTURE plus bas (les *View) ne sont
// jamais lies depuis la configuration et restent donc des records.
public sealed class RoyalPalaceLevelRequirement
{
    public RoyalPalaceLevelRequirement() { }
    public RoyalPalaceLevelRequirement(string buildingKey, int minimumLevel)
    {
        BuildingKey = buildingKey;
        MinimumLevel = minimumLevel;
    }

    public string BuildingKey { get; set; } = "";
    public int MinimumLevel { get; set; }
}

// Un deblocage attache a un niveau. `Enforced` distingue honnetement :
//  - true  : la regle est REELLEMENT appliquee par du code existant aujourd'hui
//            (ex. les abeilles championnes rares/legendaires, deja bornees par
//            le niveau du Coeur royal cote Unity).
//  - false : declaratif / vitrine de progression. M055 pose la fondation mais
//            ne verrouille RIEN de nouveau (voir la regle de compatibilite des
//            comptes existants) - a promouvoir plus tard, explicitement.
public sealed class RoyalPalaceUnlock
{
    public RoyalPalaceUnlock() { }
    public RoyalPalaceUnlock(string key, string description, bool enforced)
    {
        Key = key;
        Description = description;
        Enforced = enforced;
    }

    public string Key { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Enforced { get; set; }
}

// Definition data-driven d'UN niveau de Palais Royal. `Level` est le niveau
// ATTEINT (donc l'amelioration N-1 -> N). Le niveau 1 est l'etat initial : il
// n'a ni prerequis ni cout.
public sealed class RoyalPalaceLevelDefinition
{
    public RoyalPalaceLevelDefinition() { }
    public RoyalPalaceLevelDefinition(
        int level,
        List<RoyalPalaceLevelRequirement> upgradeRequirements,
        List<RoyalPalaceUnlock> unlocks,
        string description)
    {
        Level = level;
        UpgradeRequirements = upgradeRequirements;
        Unlocks = unlocks;
        Description = description;
    }

    public int Level { get; set; }
    public List<RoyalPalaceLevelRequirement> UpgradeRequirements { get; set; } = [];
    public List<RoyalPalaceUnlock> Unlocks { get; set; } = [];
    public string Description { get; set; } = "";
}

public sealed class RoyalPalaceProgressionOptions
{
    public const string SectionName = "RoyalPalaceProgression";
    public const int MaximumSupportedLevel = 100;

    public bool Enabled { get; set; }
    public string DefinitionVersion { get; set; } = "";
    // Signale explicitement au client (et donc au joueur/QA) que les valeurs
    // affichees sont un equilibrage Alpha provisoire, pas l'equilibrage final.
    public bool IsAlphaBalance { get; set; } = true;
    public List<RoyalPalaceLevelDefinition> Levels { get; set; } = [];

    public void Validate()
    {
        if (!Enabled && Levels.Count == 0) return;
        if (Levels.Count == 0
            || DefinitionVersion.Length is < 1 or > 64
            || DefinitionVersion.Trim() != DefinitionVersion
            || !System.Text.RegularExpressions.Regex.IsMatch(DefinitionVersion, "^[a-z0-9._-]+$"))
            throw new InvalidDataException("Invalid royal palace progression options");

        var seenLevels = new HashSet<int>();
        foreach (RoyalPalaceLevelDefinition definition in Levels)
        {
            if (definition is null
                || definition.Level < 1
                || definition.Level > MaximumSupportedLevel
                || !seenLevels.Add(definition.Level)
                || definition.UpgradeRequirements is null
                || definition.Unlocks is null
                || definition.UpgradeRequirements.Count > 16
                || definition.Unlocks.Count > 16
                || string.IsNullOrWhiteSpace(definition.Description)
                || definition.Description.Length > 400)
                throw new InvalidDataException("Invalid royal palace level definition");

            var seenRequirements = new HashSet<string>(StringComparer.Ordinal);
            foreach (RoyalPalaceLevelRequirement requirement in definition.UpgradeRequirements)
            {
                if (requirement is null
                    || string.IsNullOrWhiteSpace(requirement.BuildingKey)
                    || requirement.BuildingKey.Trim() != requirement.BuildingKey
                    || requirement.BuildingKey.Length > 64
                    || requirement.MinimumLevel < 1
                    || requirement.MinimumLevel > MaximumSupportedLevel
                    || !seenRequirements.Add(requirement.BuildingKey))
                    throw new InvalidDataException("Invalid royal palace level requirement");
                // Un Palais Royal qui se requiert lui-meme serait une impasse permanente.
                if (string.Equals(requirement.BuildingKey, RoyalPalaceProgressionKeys.RoyalPalaceBuildingKey, StringComparison.Ordinal))
                    throw new InvalidDataException("The royal palace cannot be its own prerequisite");
            }

            var seenUnlocks = new HashSet<string>(StringComparer.Ordinal);
            foreach (RoyalPalaceUnlock unlock in definition.Unlocks)
            {
                if (unlock is null
                    || string.IsNullOrWhiteSpace(unlock.Key)
                    || unlock.Key.Trim() != unlock.Key
                    || unlock.Key.Length > 64
                    || string.IsNullOrWhiteSpace(unlock.Description)
                    || unlock.Description.Length > 200
                    || !seenUnlocks.Add(unlock.Key))
                    throw new InvalidDataException("Invalid royal palace unlock");
            }
        }

        // La progression doit etre une suite continue partant du niveau 1 :
        // un trou rendrait "niveau suivant" indefinissable au milieu de la table.
        for (int level = 1; level <= Levels.Count; level++)
            if (!seenLevels.Contains(level))
                throw new InvalidDataException("Royal palace levels must be contiguous from 1");
    }
}

// Vue lisible d'UN prerequis pour un etat de ruche donne.
public sealed record RoyalPalaceRequirementView(string BuildingKey, int MinimumLevel, int CurrentLevel, bool IsSatisfied);

// Reponse complete aux questions posees par M055 : quel est mon niveau, quel est
// le suivant, que me manque-t-il, pourquoi c'est bloque, et ce que ca debloque.
public sealed record RoyalPalaceProgressionView(
    string BuildingKey,
    string DefinitionVersion,
    bool IsAlphaBalance,
    int CurrentLevel,
    int? NextLevel,
    int MaxConfiguredLevel,
    bool IsMaxConfiguredLevel,
    bool RequirementsSatisfied,
    string BlockedReasonCode,
    string BlockingBuildingKey,
    int BlockingBuildingMinimumLevel,
    string NextLevelDescription,
    IReadOnlyList<RoyalPalaceRequirementView> NextLevelRequirements,
    IReadOnlyList<RoyalPalaceUnlock> NextLevelUnlocks,
    IReadOnlyList<RoyalPalaceUnlock> UnlockedSoFar);

public static class RoyalPalaceProgression
{
    // Code d'echec renvoye par le serveur quand un prerequis de batiment n'est pas
    // satisfait. Distinct de game.insufficient_resources / game.construction_busy
    // pour que le client puisse afficher LA bonne raison et pointer le bon batiment.
    public const string PrerequisitesNotMetCode = "game.royal_palace_prerequisites";
    public const string MaxLevelCode = "game.royal_palace_max_level";

    public static int CurrentLevel(IReadOnlyDictionary<string, int>? buildingLevels)
    {
        if (buildingLevels is null) return RoyalPalaceProgressionKeys.ImplicitStartingLevel;
        return buildingLevels.TryGetValue(RoyalPalaceProgressionKeys.RoyalPalaceBuildingKey, out int level) && level >= 1
            ? level
            : RoyalPalaceProgressionKeys.ImplicitStartingLevel;
    }

    // Autorise-t-on l'amelioration du Palais Royal de `fromLevel` vers `fromLevel + 1` ?
    // COMPATIBILITE COMPTES EXISTANTS : les prerequis sont des MINIMUMS. Un compte qui
    // depasse deja largement le seuil Alpha passe donc trivialement - jamais d'incoherence
    // ni de reset. Un compte dont le Palais Royal depasse deja la table configuree ne se
    // voit simplement plus proposer d'amelioration (MaxLevelCode), il n'est pas retrograde.
    public static bool TryValidateUpgrade(
        RoyalPalaceProgressionOptions? options,
        IReadOnlyDictionary<string, int>? buildingLevels,
        int fromLevel,
        out string failureCode,
        out string blockingBuildingKey,
        out int blockingMinimumLevel)
    {
        failureCode = "";
        blockingBuildingKey = "";
        blockingMinimumLevel = 0;
        // Fail-open volontaire : tant que la progression n'est pas activee en
        // configuration, le comportement d'amelioration reste EXACTEMENT celui
        // d'avant M055 (aucun prerequis) - donc aucun compte existant ne peut se
        // retrouver bloque par un deploiement partiel de configuration.
        if (options is null || !options.Enabled || options.Levels.Count == 0) return true;

        RoyalPalaceLevelDefinition? target = Definition(options, fromLevel + 1);
        // Au-dela de la table configuree, il n'y a pas de prerequis a imposer :
        // c'est le catalogue de couts (BuildingUpgradeOptions) qui borne reellement
        // la progression, et lui seul renverra game.invalid_building_level.
        if (target is null) return true;

        foreach (RoyalPalaceLevelRequirement requirement in target.UpgradeRequirements)
        {
            int current = LevelOf(buildingLevels, requirement.BuildingKey);
            if (current >= requirement.MinimumLevel) continue;
            failureCode = PrerequisitesNotMetCode;
            blockingBuildingKey = requirement.BuildingKey;
            blockingMinimumLevel = requirement.MinimumLevel;
            return false;
        }
        return true;
    }

    public static RoyalPalaceProgressionView Evaluate(
        RoyalPalaceProgressionOptions? options,
        IReadOnlyDictionary<string, int>? buildingLevels)
    {
        int currentLevel = CurrentLevel(buildingLevels);
        string version = options?.DefinitionVersion ?? "";
        bool alpha = options?.IsAlphaBalance ?? true;

        if (options is null || !options.Enabled || options.Levels.Count == 0)
            return new RoyalPalaceProgressionView(
                RoyalPalaceProgressionKeys.RoyalPalaceBuildingKey, version, alpha, currentLevel,
                null, currentLevel, false, true, "", "", 0, "",
                Array.Empty<RoyalPalaceRequirementView>(), Array.Empty<RoyalPalaceUnlock>(), Array.Empty<RoyalPalaceUnlock>());

        int maxLevel = options.Levels.Max(x => x.Level);
        // Un compte existant peut deja depasser la table Alpha : on ne le retrograde
        // pas et on ne le declare pas incoherent, on constate simplement qu'il n'y a
        // plus de palier configure devant lui.
        bool atMax = currentLevel >= maxLevel;

        RoyalPalaceUnlock[] unlockedSoFar = options.Levels
            .Where(x => x.Level <= currentLevel)
            .OrderBy(x => x.Level)
            .SelectMany(x => x.Unlocks)
            .ToArray();

        if (atMax)
            return new RoyalPalaceProgressionView(
                RoyalPalaceProgressionKeys.RoyalPalaceBuildingKey, version, alpha, currentLevel,
                null, maxLevel, true, false, MaxLevelCode, "", 0, "",
                Array.Empty<RoyalPalaceRequirementView>(), Array.Empty<RoyalPalaceUnlock>(), unlockedSoFar);

        int nextLevel = currentLevel + 1;
        RoyalPalaceLevelDefinition? next = Definition(options, nextLevel);
        if (next is null)
            return new RoyalPalaceProgressionView(
                RoyalPalaceProgressionKeys.RoyalPalaceBuildingKey, version, alpha, currentLevel,
                null, maxLevel, false, false, MaxLevelCode, "", 0, "",
                Array.Empty<RoyalPalaceRequirementView>(), Array.Empty<RoyalPalaceUnlock>(), unlockedSoFar);

        RoyalPalaceRequirementView[] requirements = next.UpgradeRequirements
            .Select(requirement =>
            {
                int current = LevelOf(buildingLevels, requirement.BuildingKey);
                return new RoyalPalaceRequirementView(requirement.BuildingKey, requirement.MinimumLevel, current, current >= requirement.MinimumLevel);
            })
            .ToArray();

        RoyalPalaceRequirementView? blocking = requirements.FirstOrDefault(x => !x.IsSatisfied);
        return new RoyalPalaceProgressionView(
            RoyalPalaceProgressionKeys.RoyalPalaceBuildingKey, version, alpha, currentLevel,
            nextLevel, maxLevel, false,
            blocking is null, blocking is null ? "" : PrerequisitesNotMetCode,
            blocking?.BuildingKey ?? "", blocking?.MinimumLevel ?? 0,
            next.Description, requirements, next.Unlocks, unlockedSoFar);
    }

    // Surface interrogeable par le FTUE et par toute regle de deblocage :
    // "RoyalPalaceLevel >= X" sans dupliquer la source de verite.
    public static bool IsAtLeast(IReadOnlyDictionary<string, int>? buildingLevels, int minimumLevel)
        => CurrentLevel(buildingLevels) >= minimumLevel;

    public static bool IsUnlocked(
        RoyalPalaceProgressionOptions? options,
        IReadOnlyDictionary<string, int>? buildingLevels,
        string unlockKey)
    {
        if (options is null || !options.Enabled || string.IsNullOrWhiteSpace(unlockKey)) return false;
        int currentLevel = CurrentLevel(buildingLevels);
        return options.Levels.Any(level => level.Level <= currentLevel
            && level.Unlocks.Any(unlock => string.Equals(unlock.Key, unlockKey, StringComparison.Ordinal)));
    }

    private static RoyalPalaceLevelDefinition? Definition(RoyalPalaceProgressionOptions options, int level)
        => options.Levels.FirstOrDefault(x => x.Level == level);

    private static int LevelOf(IReadOnlyDictionary<string, int>? buildingLevels, string buildingKey)
    {
        if (buildingLevels is null) return RoyalPalaceProgressionKeys.ImplicitStartingLevel;
        return buildingLevels.TryGetValue(buildingKey, out int level) && level >= 1
            ? level
            : RoyalPalaceProgressionKeys.ImplicitStartingLevel;
    }
}
