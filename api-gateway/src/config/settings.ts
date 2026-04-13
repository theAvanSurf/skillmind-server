function loadEnvVariables<T extends readonly string[]>(keys: T) {
    const env: Record<string, string> = {};

    for (const key of keys) {
        const value = process.env[key];

        if (!value) {
            throw new Error(`Missing required environment variable: ${key}`);
        }

        env[key] = value;
    }

    return env as { [K in T[number]]: string };
}

const config = loadEnvVariables([
    "CORE_SERVICE_URL",
    "API_VERSION",
    "RECOMMENDATION_SERVICE_URL",
] as const);

export const appConfig = {
    API_CORE_URL: `${config.CORE_SERVICE_URL}/api/v${config.API_VERSION}`,
    API_GLOBAL_VERSION: config.API_VERSION,
    API_RECOMMENDATION_URL: config.RECOMMENDATION_SERVICE_URL,
} as const;