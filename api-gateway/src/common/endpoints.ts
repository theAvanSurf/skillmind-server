export const API_ENDPOINTS = {
    CORE: {
        AUTH_LOGIN: "/auth/login",
        AUTH_REGISTRER: "/auth/register",
        AUTH_GET_RESET_TOKEN: "/auth/account/get-reset-token",
        AUTH_CONFIRM: "/auth/account/confirm",
        AUTH_RESET_PASSWORD: "/auth/account/reset-password",
        AUTH_REFRESH: "/auth/refresh",
        AUTH_VERIFY: "/auth/verify",
        AUTH_SECURITY_SETTINGS: "/auth/account/security-settings",
        AUTH_START_PASSWORD_CHANGE: "/auth/account/credentials/password/start",
        AUTH_COMPLETE_PASSWORD_CHANGE: "/auth/account/credentials/password/complete",
        AUTH_START_EMAIL_CHANGE: "/auth/account/credentials/email/start",
        AUTH_COMPLETE_EMAIL_CHANGE: "/auth/account/credentials/email/complete",

        // Profiles
        PROFILES: "/profiles",
        PROFILES_BY_ID: (id: string) => `/profiles/${id}`,

        // Sessions
        SESSIONS: "/sessions",
        SESSIONS_DEVICES: "/sessions/devices",
        SESSIONS_DEVICE_BY_ID: (deviceId: string) => `/sessions/devices/${deviceId}`,
        SESSIONS_PROFILES: "/sessions/profiles",
        SESSIONS_PROFILE_BY_ID: (profileId: string) => `/sessions/profiles/${profileId}`,

        // Payment
        PAYMENT_CREATE_CHECKOUT_SESSION: "/payment/create-checkout-session",
        PAYMENT_CREATE_SUBSCRIPTION: "/payment/create-subscription",
        PAYMENT_SESSION_STATUS: "/payment/session-status",
        PAYMENT_CREATE_PORTAL_SESSION: "/payment/create-portal-session",
        PAYMENT_SUBSCRIPTION: "/payment/subscription",
        PAYMENT_WEBHOOK: "/payment/webhook",
    }
}