type ModuleState = {
    isLoaded: boolean;
    isLoading: boolean;
    promise?: Promise<void>;
    languages: Set<string>;
};