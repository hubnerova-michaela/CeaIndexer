namespace CeaIndexer
{
    public static class LocalizationManager
    {
        private static string _currentLanguage = "en";

        private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
        {
            ["en"] = new()
            {
                ["PathToErxExe"] = "Path to erx.exe:",
                ["Browse"] = "Browse...",
                ["CeaFolder"] = "CEA Folder:",
                ["LoadFiles"] = "Load Files",
                ["Refresh"] = "Refresh",
                ["Quantity"] = "Quantity:",
                ["Operator"] = "Operator:",
                ["Filter"] = "Filter",
                ["WatchlistLabel"] = "From Watchlist:",
                ["SelectFromWatchlist"] = "-- Select from Watchlist --",
                ["IndexedFiles"] = "Indexed Files",
                ["FileDetails"] = "File Details (Quantities)",
                ["Archive"] = "Archive",
                ["QuantityName"] = "Quantity Name",
                ["InvalidFolderPath"] = "Invalid folder path.",
                ["ValidationError"] = "Validation Error",
                ["InvalidErxPath"] = "Please provide a valid path to erx.exe",
                ["IndexingFiles"] = "Indexing files, please wait...",
                ["Processing"] = "Processing: {0} / {1}",
                ["Complete"] = "Complete",
                ["NewlyIndexed"] = "Complete. Newly indexed: {0} files. Total in folder: {1}",
                ["DisplayingRecords"] = "Displaying {0} records from database.",
                ["Found"] = "Found {0} records.",
                ["DisplayingAll"] = "Displaying all records.",
                ["File"] = "File: {0} | Quantities loaded: {1}",
                ["Running"] = "Running...",
                ["Error"] = "Error",
                ["InitializationError"] = "Initialization Error",
                ["Language"] = "Language:",
                ["Ready"] = "Ready",
                ["CzechLanguageName"] = "Czech",
                ["ErrorLoadingFiles"] = "Error loading files:",
                ["ErrorLoadingQuantities"] = "Error loading quantities:",
                ["SearchError"] = "Search error:",
                ["ErrorRunningErx"] = "Error running erx.exe:",
                ["ErrorRefreshingDb"] = "Error refreshing database:",

                ["WatchlistTitle"] = "Manage Watched Quantities",
                ["ManageWatchlist"] = "Manage Watchlist",
                ["SearchQuantity"] = "Search quantity:",
                ["Cancel"] = "Cancel",
                ["SaveSelection"] = "Save Selection",
                ["SelectAll"] = "Select All",
                ["DeselectAll"] = "Deselect All",
                ["WatchlistLoadingQuantities"] = "Loading quantities...",
                ["WatchlistCount"] = "Showing {0} of {1} | Selected: {2}",
                ["WatchlistShowSelectedOnly"] = "Show selected only",
                ["WatchlistPendingLegend"] = "Red = selected now, not yet saved in database",

                ["PleaseEnterSearchCriteria"] = "Please enter search criteria",
                ["Phase1ScanningMetadata"] = "PHASE 1: Scanning metadata...",
                ["Phase2WaitingSelection"] = "PHASE 2: Waiting for user selection...",
                ["Phase3DeepAnalysis"] = "PHASE 3: Deep analysis...",
                ["CancelledByUser"] = "Cancelled by user",
                ["Phase1Processing"] = "PHASE 1: Processing {0}/{1}",
                ["AllFilesAlreadyIndexed"] = "All {0} files are already indexed.",
                ["InfoTitle"] = "Info",
                ["NoQuantitiesFoundInScannedFiles"] = "No quantities found in scanned files!",
                ["UnknownArchive"] = "unknown",
                ["NoQuantitiesSelectedDeepAnalysis"] = "No quantities selected. Deep analysis will not run.",
                ["WarningTitle"] = "Warning",
                ["Phase1ScannedAndPhase3Later"] = "Phase 1 scanned {0} total quantities from files.\nYou can run Phase 3 later by loading files again.",
                ["SelectionCancelled"] = "Selection Cancelled",
                ["NoQuantitiesSelectedForDeepAnalysis"] = "No quantities selected for deep analysis!",
                ["NoActiveQuantitiesInWatchlist"] = "No active quantities in watchlist (all deleted)!",
                ["Phase3Processing"] = "PHASE 3: Deep analysis {0}/{1}",
                ["NoQuantitiesFoundInDatabaseRunPhase1"] = "No quantities found in database.\nRun Phase 1 first (Load Files).",
                ["NoQuantitiesSelected"] = "No quantities selected!",
                ["WatchlistUpdatedSuccessfully"] = "Watchlist updated successfully!\n{0} quantities selected.",
                ["SuccessTitle"] = "Success",
                ["ErrorEditingWatchlist"] = "Error editing watchlist: {0}"
            },
            ["cs"] = new()
            {
                ["PathToErxExe"] = "Cesta k erx.exe:",
                ["Browse"] = "Procházet...",
                ["CeaFolder"] = "CEA složka:",
                ["LoadFiles"] = "Naèíst soubory",
                ["Refresh"] = "Obnovit",
                ["Quantity"] = "Velièina:",
                ["Operator"] = "Operátor:",
                ["Filter"] = "Filtrovat",
                ["WatchlistLabel"] = "Ze Watchlistu:",
                ["SelectFromWatchlist"] = "-- Vybrat ze Watchlistu --",
                ["IndexedFiles"] = "Indexované soubory",
                ["FileDetails"] = "Podrobnosti souboru (velièiny)",
                ["Archive"] = "Archiv",
                ["QuantityName"] = "Název velièiny",
                ["InvalidFolderPath"] = "Neplatná cesta ke složce.",
                ["ValidationError"] = "Chyba ovìøení",
                ["InvalidErxPath"] = "Zadejte prosím platnou cestu k erx.exe",
                ["IndexingFiles"] = "Indexuji soubory, èekejte prosím...",
                ["Processing"] = "Zpracovávám: {0} / {1}",
                ["Complete"] = "Hotovo",
                ["NewlyIndexed"] = "Hotovo. Novì indexováno: {0} souborù. Celkem ve složce: {1}",
                ["DisplayingRecords"] = "Zobrazuji {0} záznamù z databáze.",
                ["Found"] = "Nalezeno {0} záznamù.",
                ["DisplayingAll"] = "Zobrazuji všechny záznamy.",
                ["File"] = "Soubor: {0} | Naèteno velièin: {1}",
                ["Running"] = "Spouštím...",
                ["Error"] = "Chyba",
                ["InitializationError"] = "Chyba inicializace",
                ["Language"] = "Jazyk:",
                ["Ready"] = "Pøipraveno",
                ["CzechLanguageName"] = "Èeština",
                ["ErrorLoadingFiles"] = "Chyba pøi naèítání souborù:",
                ["ErrorLoadingQuantities"] = "Chyba pøi naèítání velièin:",
                ["SearchError"] = "Chyba hledání:",
                ["ErrorRunningErx"] = "Chyba pøi spuštìní erx.exe:",
                ["ErrorRefreshingDb"] = "Chyba pøi aktualizaci databáze:",

                ["WatchlistTitle"] = "Správa sledovaných velièin",
                ["ManageWatchlist"] = "Správa watchlistu",
                ["SearchQuantity"] = "Hledat velièinu:",
                ["Cancel"] = "Zrušit",
                ["SaveSelection"] = "Uložit výbìr",
                ["SelectAll"] = "Vybrat vše",
                ["DeselectAll"] = "Zrušit výbìr",
                ["WatchlistLoadingQuantities"] = "Naèítám velièiny...",
                ["WatchlistCount"] = "Zobrazeno {0} z {1} | Vybráno: {2}",
                ["WatchlistShowSelectedOnly"] = "Zobrazit jen vybrané",
                ["WatchlistPendingLegend"] = "Èervená = nyní vybráno, ještì není uloženo v databázi",

                ["PleaseEnterSearchCriteria"] = "Zadejte kritéria vyhledávání",
                ["Phase1ScanningMetadata"] = "FÁZE 1: Skenuji metadata...",
                ["Phase2WaitingSelection"] = "FÁZE 2: Èekám na výbìr uživatele...",
                ["Phase3DeepAnalysis"] = "FÁZE 3: Hloubková analýza...",
                ["CancelledByUser"] = "Zrušeno uživatelem",
                ["Phase1Processing"] = "FÁZE 1: Zpracovávám {0}/{1}",
                ["AllFilesAlreadyIndexed"] = "Všech {0} souborù je již zaindexováno.",
                ["InfoTitle"] = "Informace",
                ["NoQuantitiesFoundInScannedFiles"] = "V naskenovaných souborech nebyly nalezeny žádné velièiny!",
                ["UnknownArchive"] = "neznámý",
                ["NoQuantitiesSelectedDeepAnalysis"] = "Nebyla vybrána žádná velièina. Hloubková analýza se nespustí.",
                ["WarningTitle"] = "Upozornìní",
                ["Phase1ScannedAndPhase3Later"] = "Fáze 1 naskenovala celkem {0} velièin ze souborù.\nFázi 3 mùžete spustit pozdìji opìtovným naètením souborù.",
                ["SelectionCancelled"] = "Výbìr zrušen",
                ["NoQuantitiesSelectedForDeepAnalysis"] = "Pro hloubkovou analýzu nejsou vybrány žádné velièiny!",
                ["NoActiveQuantitiesInWatchlist"] = "V watchlistu nejsou žádné aktivní velièiny (všechny jsou odebrané)!",
                ["Phase3Processing"] = "FÁZE 3: Hloubková analýza {0}/{1}",
                ["NoQuantitiesFoundInDatabaseRunPhase1"] = "V databázi nebyly nalezeny žádné velièiny.\nNejprve spuste Fázi 1 (Naèíst soubory).",
                ["NoQuantitiesSelected"] = "Nebyla vybrána žádná velièina!",
                ["WatchlistUpdatedSuccessfully"] = "Watchlist byl úspìšnì aktualizován!\nVybráno velièin: {0}.",
                ["SuccessTitle"] = "Úspìch",
                ["ErrorEditingWatchlist"] = "Chyba pøi úpravì watchlistu: {0}"
            }
        };

        public static void SetLanguage(string language)
        {
            if (Translations.ContainsKey(language))
                _currentLanguage = language;
        }

        public static string GetString(string key, params object[] args)
        {
            if (Translations[_currentLanguage].TryGetValue(key, out var translation))
            {
                return args.Length > 0 ? string.Format(translation, args) : translation;
            }

            return key;
        }

        public static string CurrentLanguage => _currentLanguage;
    }
}
