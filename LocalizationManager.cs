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
                ["TestCsv"] = "Test CSV",
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
                ["AnalysisSuccessful"] = "Analysis successful!",
                ["Minimum"] = "MINIMUM: {0} V  (at: {1})",
                ["Maximum"] = "MAXIMUM: {0} V  (at: {1})",
                ["CsvAnalysisResult"] = "CSV Analysis Result",
                ["SelectCsvFile"] = "Select CSV file for testing",
                ["CsvFiles"] = "CSV Files (*.csv)|*.csv",
                ["AnalysisErrorTitle"] = "Error analyzing CSV",
                ["InitializationError"] = "Initialization Error",
                ["Language"] = "Language:",
                ["Ready"] = "Ready",
                ["ErrorLoadingFiles"] = "Error loading files:",
                ["ErrorLoadingQuantities"] = "Error loading quantities:",
                ["SearchError"] = "Search error:",
                ["ErrorRunningErx"] = "Error running erx.exe:",
                ["ErrorRefreshingDb"] = "Error refreshing database:",
            },
            ["cs"] = new()
            {
                ["PathToErxExe"] = "Cesta k erx.exe:",
                ["Browse"] = "Procházet...",
                ["CeaFolder"] = "CEA Složka:",
                ["LoadFiles"] = "Naèíst soubory",
                ["Refresh"] = "Obnovit",
                ["Quantity"] = "Velièina:",
                ["Operator"] = "Operátor:",
                ["Filter"] = "Filtrovat",
                ["TestCsv"] = "Test CSV",
                ["IndexedFiles"] = "Indexované soubory",
                ["FileDetails"] = "Podrobnosti souboru (Velièiny)",
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
                ["AnalysisSuccessful"] = "Analýza byla úspìšná!",
                ["Minimum"] = "MINIMUM: {0} V  (v: {1})",
                ["Maximum"] = "MAXIMUM: {0} V  (v: {1})",
                ["CsvAnalysisResult"] = "Výsledek analýzy CSV",
                ["SelectCsvFile"] = "Vyberte soubor CSV k testování",
                ["CsvFiles"] = "Soubory CSV (*.csv)|*.csv",
                ["AnalysisErrorTitle"] = "Chyba pøi analýze CSV",
                ["InitializationError"] = "Chyba inicializace",
                ["Language"] = "Jazyk:",
                ["Ready"] = "Pøipraveno",
                ["ErrorLoadingFiles"] = "Chyba pøi naèítání souborù:",
                ["ErrorLoadingQuantities"] = "Chyba pøi naèítání velièin:",
                ["SearchError"] = "Chyba hledání:",
                ["ErrorRunningErx"] = "Chyba pøi spuštìní erx.exe:",
                ["ErrorRefreshingDb"] = "Chyba pøi aktualizaci databáze:",
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
