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

        // Courses
        COURSES: "/courses",
        COURSES_SEARCH: "/courses/search",
        COURSES_CATEGORIES: "/courses/categories",
        COURSES_BY_ID: (id: string) => `/courses/${id}`,
        COURSES_RELATED: (id: string) => `/courses/${id}/related`,
        COURSES_PROGRESS: (id: string) => `/courses/${id}/progress`,
        COURSES_ENROLLMENT_STATUS: (id: string) => `/courses/${id}/enrollment-status`,
        COURSES_PURCHASE: (id: string) => `/courses/${id}/purchase`,
        COURSES_SEASONS: "/courses/seasons",
        COURSES_LESSONS: "/courses/lessons",

        // Professor
        PROFESSOR_PROFILE: "/professor/profile",
        PROFESSOR_DASHBOARD: "/professor/dashboard",
        PROFESSOR_EARNINGS: "/professor/earnings",
        PROFESSOR_STUDENTS: "/professor/students",
        PROFESSOR_STRIPE_CONNECT: "/professor/stripe/connect",
        PROFESSOR_STRIPE_STATUS: "/professor/stripe/status",
        PROFESSOR_STRIPE_WEBHOOK: "/professor/stripe/webhook",
        PROFESSOR_EXAMS: "/professor/exams",
        PROFESSOR_EXAM_BY_ID: (id: string) => `/professor/exams/${id}`,
        PROFESSOR_EXAMS_BY_COURSE: (courseId: string) => `/professor/exams/course/${courseId}`,
        PROFESSOR_EXAM_PUBLISH: (id: string) => `/professor/exams/${id}/publish`,
        PROFESSOR_COURSE_PUBLISH: (id: string) => `/professor/courses/${id}/publish`,
        PROFESSOR_EXAM_QUESTIONS: (id: string) => `/professor/exams/${id}/questions`,
        PROFESSOR_EXAM_ATTEMPTS: (id: string) => `/professor/exams/${id}/attempts`,
        PROFESSOR_EXAMS_SUBMIT: "/professor/exams/submit",
        PROFESSOR_EXAMS_GRADE: "/professor/exams/attempts/grade",
        PROFESSOR_CERT_TEMPLATES: "/professor/certificates/templates",
        PROFESSOR_CERT_TEMPLATE_BY_ID: (id: string) => `/professor/certificates/templates/${id}`,
        PROFESSOR_CERT_ISSUE: "/professor/certificates/issue",
        PROFESSOR_CERTS_BY_COURSE: (courseId: string) => `/professor/certificates/course/${courseId}`,
        PROFESSOR_COURSES: "/professor/courses",
        PROFESSOR_COURSE_BY_ID: (id: string) => `/professor/courses/${id}`,

        // Live Streams
        PROFESSOR_LIVESTREAMS: "/live-streams",
        PROFESSOR_LIVESTREAM_BY_ID: (id: string) => `/live-streams/${id}`,
        PROFESSOR_LIVESTREAM_START: (id: string) => `/live-streams/${id}/start`,
        PROFESSOR_LIVESTREAM_END: (id: string) => `/live-streams/${id}/end`,
        PROFESSOR_LIVESTREAM_OAUTH_URL: "/live-streams/oauth/url",
        PROFESSOR_LIVESTREAM_OAUTH_EXCHANGE: "/live-streams/oauth/exchange",
        PROFESSOR_LIVESTREAM_YOUTUBE_STATUS: "/live-streams/youtube-status",
        PROFESSOR_LIVESTREAM_COURSE_ACTIVE: (courseId: string) => `/live-streams/course/${courseId}/active`,
    }
}