import "./reoAmbient"

interface Reo {
    /**
     * Loads translation module.
     * @param module Module name
     * @param lang Optional language parameter
     */
    require(module: string, lang?: string): Promise<void>;
    /**
     * Returns current language.
     */
    getCurrentLang(): string;
    /**
     * Gets translation for given key.
     * @param key Translation key
     * @param lang Optional language parameter
     */
    get(key: string, lang?: string): string;
    /**
     * Notifies about language change and loads missing translations.
     * @param newLang New language
     */
    setLanguage(newLang: string): Promise<void>;
    /**
     * Returns list of loaded modules and their languages.
     */
    getLoadedModules(): Array<{ module: string, languages: string[] }>;
    /**
     * Returns loading state for given module.
     * @param module Module name
     */
    getLoadingState(module: string): ModuleState | undefined;
    /**
     * Indexer allowing direct access to translations.
     */
    [key: string]: any;
    /**
     * Whether developer mode is enabled or not. In dev mode Reo uses slower keys accessor to warn about missing keys and provide hints.
     */
    devMode: boolean;
}
